# Python Room Monitor for Ubiq Server

This directory contains Python clients that can monitor and interact with the Ubiq Node.js server to discover available rooms and join them. The monitors support both **TCP** and **WebSocket** connections.

## Files

- `enhanced_room_monitor.py` - Full-featured room monitor with TCP and WebSocket support
- `simple_room_monitor.py` - Lightweight room monitor with basic TCP/WebSocket functionality  
- `python_room_monitor.py` - Original WebSocket-only monitor (for compatibility)
- `requirements_python.txt` - Python dependencies

## Connection Types

The Ubiq server supports multiple connection types:

- **TCP**: Direct TCP socket connection (usually port 8011)
- **WebSocket**: Unencrypted WebSocket connection (usually port 8010)  
- **Secure WebSocket**: SSL/TLS encrypted WebSocket connection (usually port 8009)

The enhanced monitors will automatically try all connection methods in order of preference.

## Installation

1. Make sure you have Python 3.7+ installed
2. Install the required dependencies:
   ```powershell
   pip install websockets
   ```

## Usage

### Starting the Ubiq Server

First, make sure your Ubiq Node.js server is running:

```powershell
# Start the regular server
F5  # Or run the debug configuration in VS Code

# OR start the test broadcast server
F6  # Or use the test broadcast task
```

### Running the Python Room Monitor

### Enhanced Monitor (Recommended)

```powershell
python enhanced_room_monitor.py
```

This will prompt you for:
- Server host (default: localhost)
- TCP port (default: 8011)
- WebSocket port (default: 8010)  
- Secure WebSocket port (default: 8009)
- Connection method preference

#### Simple Monitor

```powershell
python simple_room_monitor.py
```

Supports both TCP and WebSocket with automatic connection fallback.

#### Original WebSocket-Only Monitor

```powershell
python python_room_monitor.py
```

This version only supports WebSocket connections for compatibility.

## Features

### Multi-Protocol Support
- **TCP Connection**: Direct binary socket communication
- **WebSocket**: Standard WebSocket protocol
- **Secure WebSocket**: SSL/TLS encrypted WebSocket
- **Auto-Connect**: Automatically tries all available connection methods

### Room Discovery
- Automatically discovers all published rooms on the server
- Shows room names, join codes, and publish status
- Refreshes room list on demand

### Room Interaction
- Join any available room by selecting its number
- Send ping messages to test connectivity
- Receive test broadcasts from the Node.js test server
- Leave rooms and return to the main menu

### Real-time Updates
- Receives notifications when peers join/leave rooms
- Shows test broadcasts sent by the Node.js server (spacebar broadcasts)
- Displays welcome messages and server responses

## Commands

### Main Menu
- `1-N`: Join room by number
- `R`: Refresh room list
- `P`: Send ping to server
- `Q`: Quit

### In Room
- `P`: Send ping
- `I`: Show room information
- `L`: Leave room

## Integration with Test Broadcast Server

When you run the test broadcast server (`test_broadcast.ts`) and press the spacebar, the Python monitor will receive and display the broadcast messages. This is useful for testing server-to-client communication.

## Troubleshooting

### Connection Issues

1. **SSL Certificate Errors**: The monitors use self-signed certificate acceptance for development. Make sure your server is using the correct SSL configuration.

2. **Port Issues**: Ensure the server is listening on the expected port (default: 8009 for WSS, 8010 for WS).

3. **Firewall**: Make sure your firewall allows connections on the server ports.

### Server Configuration

Make sure your Ubiq server configuration includes both TCP and WebSocket servers:

```json
{
  "roomserver": {
    "tcp": {
      "port": 8011
    },
    "wss": {
      "port": 8009,
      "cert": "path/to/cert.pem", 
      "key": "path/to/key.pem"
    }
  }
}
```

Note: The server can run TCP and WebSocket simultaneously. The Python monitors will detect which protocols are available.

### Message Format

The Python monitors expect messages in the format used by the Ubiq server:

```json
{
  "type": "MessageType",
  "args": "{\"key\":\"value\"}"
}
```

## Development

To extend the monitors:

1. Add new message handlers in the `handle_incoming_message()` method
2. Add new commands in the interactive loops
3. Implement additional Ubiq protocol features as needed

## Example Session

```
🚀 Enhanced Ubiq Room Monitor (TCP + WebSocket)
==================================================
Server host (default: localhost): 
TCP port (default: 8011): 
WebSocket port (default: 8010): 
Secure WebSocket port (default: 8009): 

Connection methods:
1. Auto-connect (try all methods)
2. TCP only
3. WebSocket only  
4. Secure WebSocket only
Choose connection method (default: 1): 

🔄 Trying Secure WebSocket...
✅ Connected via Secure WebSocket!

============================================================
🔍 ENHANCED UBIQ ROOM MONITOR
============================================================
🔗 Connected via Secure WebSocket to localhost:8009
📡 Found 2 rooms via WSS

🏠 Available Rooms (2) via WSS:
------------------------------------------------------------
 1. Test Room Alpha
     Code: abc | 📢 Public
     UUID: 11111111-2222-3333-4444-555555555555

 2. Development Room
     Code: xyz | 🔒 Private  
     UUID: 66666666-7777-8888-9999-000000000000

Commands:
1-2: Join room by number
R: Refresh rooms
P: Send ping
S: Connection status
Q: Quit

>> 1
🏠 Joining room: Test Room Alpha (code: abc)
✅ Joined room: Test Room Alpha
👤 Peer joined: unity-client-123 (unity_client via tcp)

🏠 In room: Test Room Alpha (via WSS)
Commands: P=ping, I=room info, L=leave
[Test Room Alpha] >> 

📢 TEST BROADCAST #1
   From: NodeJS-TestServer
   Message: 🎯 Test broadcast from Node.js server #1
   Time: 2025-07-01T10:30:00.000Z
   Via: WSS
```
