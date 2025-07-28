using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

public class NetworkSTLHandler : MonoBehaviour
{
    NetworkContext context;

    [Serializable]
    private struct Message
    {
        public string type;
        public STL_Data args; // Change this to string to handle nested JSON

        public Message(string type, STL_Data args)
        {
            this.type = type;
            this.args = args;
        }
    }

    [Serializable]
    private struct STL_Data
    {
        public string filename;
        public float[] vertices;  // Flattened array: [v1x, v1y, v1z, v2x, v2y, v2z, v3x, v3y, v3z, ...] (9 floats per triangle)
        public float[] normals;   // Flattened array: [n1x, n1y, n1z, n2x, n2y, n2z, ...] (3 floats per triangle)
        public int vertex_count;
        public int triangle_count;
        public BoundsData bounds;
    }

    [Serializable]
    private struct BoundsData
    {
        public float min_x;
        public float max_x;
        public float min_y;
        public float max_y;
        public float min_z;
        public float max_z;
    }

    // Wrapper for the actual STL data message
    [Serializable]
    private struct STLMessage
    {
        public STL_Data stl_data;
    }

    // Queue for async processing
    private Queue<STL_Data> stlProcessingQueue = new Queue<STL_Data>();
    private bool isProcessing = false;

    void Start()
    {
        context = NetworkScene.Register(this);
        context.Scene.gameObject.TryGetComponent(out RoomClient roomClient);
        if (roomClient != null)
        {
            roomClient.OnRoomMessage.AddListener(ProcessMessage);
        }
        else
        {
            Debug.LogError("RoomClient is not assigned in NetworkSTLHandler.");
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        try
        {
            // First parse the outer message structure
            var outerMessage = JsonUtility.FromJson<Message>(message.ToString());

            if (outerMessage.type != "STL_DATA") return;

            // Then parse the args field which contains the actual STL data
            var stlData = outerMessage.args;

            // Handle the STL data here
            Debug.Log($"Received STL Data: {stlData.filename}");
            Debug.Log($"Vertex Count: {stlData.vertex_count}");
            Debug.Log($"Triangle Count: {stlData.triangle_count}");
            Debug.Log($"Vertices array length: {stlData.vertices?.Length ?? 0}");
            Debug.Log($"Normals array length: {stlData.normals?.Length ?? 0}");
            Debug.Log($"Bounds: X({stlData.bounds.min_x} to {stlData.bounds.max_x}), Y({stlData.bounds.min_y} to {stlData.bounds.max_y}), Z({stlData.bounds.min_z} to {stlData.bounds.max_z})");

            // Process the vertices and normals asynchronously
            if (stlData.vertices != null && stlData.vertices.Length > 0)
            {
                int expectedTriangles = stlData.vertices.Length / 9;
                Debug.Log($"Queueing {expectedTriangles} triangles for async processing...");
                QueueSTLDataForProcessing(stlData);
            }
            else
            {
                Debug.LogWarning("Vertices array is null or empty!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing STL message: {e.Message}");
            Debug.LogError($"Raw message: {message.ToString()}");
        }
    }

    private IEnumerator ProcessSTLDataAsync(STL_Data stlData)
    {
        Debug.Log($"Starting async processing of STL: {stlData.filename}");

        // Create a mesh from the STL data
        Mesh mesh = new Mesh();

        // Convert the triangle data to Unity's mesh format
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexIndex = 0;
        int processedTriangles = 0;

        // Validate data
        if (stlData.vertices == null || stlData.vertices.Length == 0)
        {
            Debug.LogError("Vertices data is null or empty!");
            yield break;
        }

        if (stlData.normals == null || stlData.normals.Length == 0)
        {
            Debug.LogError("Normals data is null or empty!");
            yield break;
        }

        // Validate array lengths
        if (stlData.vertices.Length % 9 != 0)
        {
            Debug.LogError($"Vertices array length ({stlData.vertices.Length}) is not divisible by 9!");
            yield break;
        }

        if (stlData.normals.Length % 3 != 0)
        {
            Debug.LogError($"Normals array length ({stlData.normals.Length}) is not divisible by 3!");
            yield break;
        }

        int totalTriangles = stlData.vertices.Length / 9;
        
        // Check if normals are per-triangle (3 per triangle) or per-vertex (9 per triangle)
        bool normalsPerVertex = stlData.normals.Length == stlData.vertices.Length; // 9 normals per triangle
        bool normalsPerTriangle = stlData.normals.Length == (totalTriangles * 3); // 3 normals per triangle
        
        if (!normalsPerVertex && !normalsPerTriangle)
        {
            Debug.LogError($"Normals array length ({stlData.normals.Length}) doesn't match expected format. Expected {stlData.vertices.Length} (per-vertex) or {totalTriangles * 3} (per-triangle)");
            yield break;
        }

        Debug.Log($"Processing {totalTriangles} triangles from flattened arrays. Normals format: {(normalsPerVertex ? "per-vertex" : "per-triangle")}");

        // Process each triangle from the flattened arrays
        for (int i = 0; i < totalTriangles; i++)
        {
            // Calculate base indices for this triangle
            int vertexBaseIndex = i * 9;  // 9 floats per triangle (3 vertices * 3 coords)

            // Get the three vertices of the triangle
            Vector3 v0 = new Vector3(
                stlData.vertices[vertexBaseIndex],
                stlData.vertices[vertexBaseIndex + 1],
                stlData.vertices[vertexBaseIndex + 2]
            );
            Vector3 v1 = new Vector3(
                stlData.vertices[vertexBaseIndex + 3],
                stlData.vertices[vertexBaseIndex + 4],
                stlData.vertices[vertexBaseIndex + 5]
            );
            Vector3 v2 = new Vector3(
                stlData.vertices[vertexBaseIndex + 6],
                stlData.vertices[vertexBaseIndex + 7],
                stlData.vertices[vertexBaseIndex + 8]
            );

            // Add vertices in Unity's preferred counter-clockwise order
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);

            // Handle normals based on format
            if (normalsPerVertex)
            {
                // 9 normal components per triangle (3 per vertex)
                int normalBaseIndex = i * 9;
                Vector3 n0 = new Vector3(
                    stlData.normals[normalBaseIndex],
                    stlData.normals[normalBaseIndex + 1],
                    stlData.normals[normalBaseIndex + 2]
                );
                Vector3 n1 = new Vector3(
                    stlData.normals[normalBaseIndex + 3],
                    stlData.normals[normalBaseIndex + 4],
                    stlData.normals[normalBaseIndex + 5]
                );
                Vector3 n2 = new Vector3(
                    stlData.normals[normalBaseIndex + 6],
                    stlData.normals[normalBaseIndex + 7],
                    stlData.normals[normalBaseIndex + 8]
                );
                
                normals.Add(n0);
                normals.Add(n1);
                normals.Add(n2);
            }
            else
            {
                // 3 normal components per triangle (shared by all vertices)
                int normalBaseIndex = i * 3;
                Vector3 triangleNormal = new Vector3(
                    stlData.normals[normalBaseIndex],
                    stlData.normals[normalBaseIndex + 1],
                    stlData.normals[normalBaseIndex + 2]
                );
                
                normals.Add(triangleNormal);
                normals.Add(triangleNormal);
                normals.Add(triangleNormal);
            }

            // Add triangle indices
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            vertexIndex += 3;

            processedTriangles++;

            // Yield control every 100 triangles to prevent frame drops
            if (processedTriangles % 100 == 0)
            {
                Debug.Log($"Processed {processedTriangles}/{totalTriangles} triangles...");
                yield return null; // Wait for next frame
            }
        }

        Debug.Log($"Final mesh stats: {vertices.Count} vertices, {normals.Count} normals, {triangles.Count} triangle indices");

        // Validate mesh data
        if (vertices.Count == 0)
        {
            Debug.LogError("No vertices were processed!");
            yield break;
        }

        // Apply to mesh (this must be done on the main thread)
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();

        // Recalculate bounds and tangents
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        // Create a GameObject to display the mesh
        GameObject stlObject = new GameObject($"STL_{stlData.filename}");
        stlObject.transform.parent = this.transform;
        stlObject.transform.localPosition = Vector3.zero;
        stlObject.transform.localRotation = Quaternion.identity;
        stlObject.transform.localScale = Vector3.one;
        
        MeshFilter meshFilter = stlObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = stlObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;

        // Create and configure the material properly
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Set the base color to gray (URP uses "_BaseColor" instead of "_Color")
        material.SetColor("_BaseColor", Color.gray);
        
        // Optional: Set other properties for better appearance
        material.SetFloat("_Metallic", 0.0f);      // Non-metallic
        material.SetFloat("_Smoothness", 0.3f);    // Slightly rough surface
        
        // Enable double-sided rendering as a fallback
        material.SetFloat("_Cull", 0); // 0 = Off (double-sided), 1 = Front, 2 = Back
        
        meshRenderer.material = material;

        Debug.Log($"Async processing completed: Created STL GameObject '{stlObject.name}' with {vertices.Count} vertices");

        // Optional: Add some visual feedback
        StartCoroutine(FlashNewObject(stlObject));
    }

    private void QueueSTLDataForProcessing(STL_Data stlData)
    {
        stlProcessingQueue.Enqueue(stlData);

        // Start processing if not already processing
        if (!isProcessing)
        {
            StartCoroutine(ProcessSTLQueue());
        }
    }

    private IEnumerator ProcessSTLQueue()
    {
        isProcessing = true;

        while (stlProcessingQueue.Count > 0)
        {
            var stlData = stlProcessingQueue.Dequeue();

            // Process the STL data asynchronously using a coroutine
            yield return StartCoroutine(ProcessSTLDataAsync(stlData));
        }

        isProcessing = false;
    }

    // Optional: Visual feedback for newly created objects
    private IEnumerator FlashNewObject(GameObject obj)
    {
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            var originalMaterial = renderer.material;
            
            // Create flash material using the same shader as the original
            var flashMaterial = new Material(originalMaterial.shader);
            
            // Set color properties for URP
            if (flashMaterial.HasProperty("_BaseColor"))
            {
                flashMaterial.SetColor("_BaseColor", Color.green);
            }
            else if (flashMaterial.HasProperty("_Color"))
            {
                flashMaterial.SetColor("_Color", Color.green);
            }
            
            // Set metallic and smoothness if the shader supports it
            if (flashMaterial.HasProperty("_Metallic"))
                flashMaterial.SetFloat("_Metallic", 0.8f);
            if (flashMaterial.HasProperty("_Smoothness"))
                flashMaterial.SetFloat("_Smoothness", 0.8f);

            // Flash green for 1 second
            renderer.material = flashMaterial;
            yield return new WaitForSeconds(1f);
            renderer.material = originalMaterial;

            // Clean up the temporary material
            Destroy(flashMaterial);
        }
    }

    // Optional: Method to clear all processed STL objects
    public void ClearAllSTLObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).name.StartsWith("STL_"))
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        Debug.Log("Cleared all STL objects");
    }

    // Optional: Get processing status
    public bool IsProcessing => isProcessing;
    public int QueuedItems => stlProcessingQueue.Count;
}