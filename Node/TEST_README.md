# Unity Client Test Broadcasting

This setup allows you to test broadcasting messages from your Node.js server to Unity clients.

## 🚀 How to Run

### 1. Start the Test Server

**Option A: Using VS Code Debug (F5)**

**Current Issue Fix**: If F5 is starting app.ts instead of test_broadcast.ts:

1. **Quick Fix Method**:
   - Press **Ctrl+Shift+P** (Command Palette)
   - Type: `Debug: Select and Start Debugging`
   - Choose "🎯 Debug Test Broadcast Server"
   - VS Code will remember this choice for future F5 presses

2. **Alternative Method**:
   - Open Debug panel: **Ctrl+Shift+D**
   - Click the dropdown next to the play button
   - Select "🎯 Debug Test Broadcast Server"
   - Click the green play button or press F5

3. **Reset VS Code Method**:
   - Close VS Code completely
   - Delete: `.vscode/settings.json` (if needed)
   - Reopen VS Code and try F5 again

**Configured Shortcuts:**
- **F5**: Should start test broadcast server (after selection)
- **F6**: Run test server without debugging (works immediately)
- **Shift+F5**: Start main app.ts instead
- **Ctrl+F5**: Run without debugging

**Option B: Using Terminal**
```bash
npx ts-node test_broadcast.ts
```

You should see:
```
🚀 Unity Broadcast Test Server Started
📡 TCP Server listening on port: 8009
🔐 WebSocket Server listening on port: 8010
📊 Status API on port: 8011

⌨️  Press SPACEBAR to broadcast test message to Unity clients
🔄 Press R to show room statistics  
👥 Press P to show peer details
❌ Press CTRL+C to exit
```

### 2. Setup Unity Client

1. **Add the Script**: Copy `Unity_TestMessageReceiver.cs` to your Unity project
2. **Add to Scene**: Create an empty GameObject and attach the `TestMessageReceiver` script
3. **Configure**: In the inspector, you can toggle:
   - Log to Console: Shows messages in Unity console
   - Show On Screen Notifications: Displays messages as overlay

### 3. Connect Unity Clients

1. Run your Unity application
2. Connect both Unity clients to the server
3. Have them join the same room

You should see in the Node.js console:
```
🏠 New room created: "YourRoomName" (room-uuid) joincode: abc
👤 Peer joined: peer-uuid-1 → Room "YourRoomName" (1 total peers)
👤 Peer joined: peer-uuid-2 → Room "YourRoomName" (2 total peers)
```

### 4. Test Broadcasting

Press **SPACEBAR** in the Node.js console to broadcast messages.

You'll see:
- **Node.js Console**: Confirmation of message sent
- **Unity Console**: Received message details
- **Unity Scene**: On-screen notification (if enabled)

## 🎛️ Available Commands

| Key | Action |
|-----|--------|
| **SPACEBAR** | Broadcast test message to all Unity clients |
| **R** | Show detailed room statistics |
| **P** | Show peer details for all connected clients |
| **Ctrl+C** | Gracefully shutdown server |

## 📱 Unity Message Types

### Test Broadcast Message
```json
{
  "messageId": 1,
  "content": "🎯 Test broadcast from Node.js server #1",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "sender": "NodeJS-TestServer",
  "type": "TestBroadcast",
  "data": {
    "counter": 1,
    "rooms": 1,
    "totalPeers": 2
  }
}
```

### Welcome Message
Automatically sent when a Unity client joins:
```json
{
  "message": "Welcome to RoomName!",
  "timestamp": "2024-01-15T10:30:45.123Z", 
  "serverVersion": "1.0.0",
  "peersInRoom": 2
}
```

## 🔧 Unity Script Features

The `TestMessageReceiver` script provides:

- **Automatic Message Handling**: Processes server messages automatically
- **On-Screen Notifications**: Visual feedback in Unity scene
- **Console Logging**: Detailed logs for debugging
- **Event System**: Other scripts can subscribe to message events
- **Error Handling**: Graceful handling of malformed messages

## 📊 Expected Output

### Node.js Console:
```
📢 Broadcasting test message #1...
   📡 Room "Test Room" (joincode: abc): 2 Unity peers
     👤 unity-client-1 (clientId: 12345)
     👤 unity-client-2 (clientId: 67890)
✅ Message sent to 2 Unity peers across 1 rooms
```

### Unity Console:
```
[TestMessageReceiver] 🎯 Test Message #1: 🎯 Test broadcast from Node.js server #1
[TestMessageReceiver] Timestamp: 2024-01-15T10:30:45.123Z
[TestMessageReceiver] Sender: NodeJS-TestServer
[TestMessageReceiver] Rooms: 1, Total Peers: 2
```

### Unity Scene:
On-screen notification showing the message content, timestamp, and peer count.

## 🐛 Debugging Features

### VS Code Debug Configuration
- **F5 Key**: Quick start debugging the test broadcast server
- **Breakpoints**: Set breakpoints in TypeScript code for step-through debugging
- **Variable Inspection**: Inspect `roomServer`, peer objects, and message data
- **Call Stack**: See the full execution path when messages are processed

### Useful Breakpoints to Set:
- `broadcastTestMessage()` function - See when messages are sent
- `getAllRooms()` function - Inspect room data structure
- `showRoomStatistics()` function - Debug room state
- Event handlers (`roomServer.on(...)`) - Monitor room/peer events

### Debug Console Commands:
While debugging, you can evaluate expressions in the Debug Console:
```javascript
// Check rooms
getAllRooms()

// Inspect room server
roomServer

// Check configuration
nconf.get('roomserver')

// See connected peers
getAllRooms().flatMap(room => room.peers)
```

## 🐛 Troubleshooting

### Unity Clients Not Appearing
- Check Unity is connecting to correct server ports (8009 for TCP, 8010 for WebSocket)
- Verify room creation and joining is working
- Use **R** key to check room statistics

### Messages Not Received
- Ensure `TestMessageReceiver` script is attached to a GameObject
- Check Unity console for any error messages
- Verify the message structure matches expected format

### Connection Issues
- Make sure server is running before starting Unity clients
- Check firewall settings for ports 8009, 8010, 8011
- Try connecting locally first (localhost/127.0.0.1)

## 🎯 Next Steps

This test setup demonstrates the foundation for:
- **Gaussian Splatting Notifications**: Notify when STL files are ready
- **Room-wide Announcements**: Server status updates
- **Real-time Collaboration**: Synchronized events across clients

You can extend the message types and Unity handling for your specific use cases!
