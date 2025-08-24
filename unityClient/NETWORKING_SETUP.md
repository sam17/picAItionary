# Unity 6 Multiplayer Networking Setup Guide

## Overview
This project has been updated to use Unity's Multiplayer Services SDK which provides a unified Sessions API combining Unity Relay (NAT punchthrough) and Unity Lobby (session management).

## Architecture Changes

### Previous Implementation (Not Working)
- Local room code dictionary (only worked on same machine)
- Direct NetworkManager.StartHost/StartClient calls
- No authentication or Unity Services
- No NAT traversal capability

### New Implementation (Unity Multiplayer Services)
- **Unity Services Authentication** - Anonymous sign-in for player identity
- **Unity Sessions API** - Unified interface for lobby and relay
- **Unity Relay** - NAT punchthrough for internet connectivity
- **Unity Lobby** - Session discovery and management
- **Netcode for GameObjects** - Networking framework

## New Scripts Created

### 1. UnityServicesInitializer.cs
- Initializes Unity Services and handles authentication
- Signs in players anonymously
- Must be added to a GameObject in the scene

### 2. SessionManager.cs
- Manages session creation, joining, and lifecycle
- Handles player properties and ready states
- Integrates with Unity Relay for connections

### 3. Updated GameNetworkManager.cs
- Now uses async/await pattern
- Integrates with SessionManager for session operations
- Handles Relay allocation and transport configuration

## Unity Editor Setup Required

### 1. Unity Services Configuration
1. Open Unity Editor
2. Go to **Edit > Project Settings > Services**
3. Link your project to Unity Dashboard (create account if needed)
4. Enable the following services:
   - Authentication
   - Relay
   - Lobby

### 2. Scene Setup
1. Create an empty GameObject called "NetworkServices"
2. Add the following components:
   - UnityServicesInitializer
   - SessionManager
   - GameNetworkManager (if not already in scene)

### 3. NetworkManager Configuration
1. Find the NetworkManager GameObject in your scene
2. Ensure it has the **Unity Transport** component
3. Set the Protocol Type to "Unity Transport"
4. The transport will be configured automatically by the code

### 4. Player Prefab Setup
1. Ensure your Player prefab has:
   - NetworkObject component
   - PlayerData script
2. Register the Player prefab in NetworkManager's NetworkPrefabs list

### 5. Build Settings
1. Add both MainMenu and Game scenes to Build Settings
2. MainMenu should be at index 0

## Testing Workflow

### Local Testing (Editor + Build)
1. Build the project
2. Run one instance in Unity Editor as Host
3. Run built executable as Client
4. Use the 6-character room code to join

### Internet Testing
1. Both players need internet connection
2. Host creates room and shares 6-character code
3. Clients join using the code
4. Unity Relay handles NAT traversal automatically

## Common Issues and Solutions

### Issue: "Failed to authenticate"
**Solution:** Check internet connection and Unity Services setup

### Issue: "Failed to create session"
**Solution:** 
- Verify Unity Dashboard configuration
- Check Authentication and Lobby services are enabled
- Ensure project is linked to Unity organization

### Issue: "Failed to join session"
**Solution:**
- Verify room code is correct (6 characters, case-insensitive)
- Check that session hasn't expired (sessions timeout after inactivity)
- Ensure both players have internet connection

### Issue: NetworkManager not connecting
**Solution:**
- Verify Unity Transport is attached to NetworkManager
- Check that Player prefab is in NetworkPrefabs list
- Ensure scenes are added to Build Settings

## API Flow

### Host Flow:
1. UnityServicesInitializer.InitializeUnityServices()
2. AuthenticationService.SignInAnonymouslyAsync()
3. SessionManager.CreateSession()
4. MultiplayerService.CreateSessionAsync() (with Relay enabled)
5. RelayService.CreateAllocationAsync()
6. Configure Unity Transport with Relay data
7. NetworkManager.StartHost()

### Client Flow:
1. UnityServicesInitializer.InitializeUnityServices()
2. AuthenticationService.SignInAnonymouslyAsync()
3. SessionManager.JoinSessionByCode()
4. MultiplayerService.JoinSessionByCodeAsync()
5. RelayService.JoinAllocationAsync()
6. Configure Unity Transport with Relay data
7. NetworkManager.StartClient()

## Next Steps

### Required Unity Editor Actions:
1. ✅ Link project to Unity Dashboard
2. ✅ Enable Authentication, Relay, and Lobby services
3. ✅ Add scripts to GameObjects in scene
4. ✅ Configure NetworkManager with Unity Transport
5. ✅ Test in Editor with local build

### Optional Enhancements:
- Add reconnection logic for dropped connections
- Implement session browser to list available games
- Add matchmaking for skill-based matches
- Implement voice chat using Unity Vivox
- Add session properties for game modes/settings

## Debugging Tips

Enable debug logging:
```csharp
Debug.unityLogger.logEnabled = true;
Debug.unityLogger.filterLogType = LogType.Log;
```

Monitor Unity Services Dashboard for:
- Active sessions
- Relay allocations
- Authentication metrics
- Error logs

## Resources
- [Unity Multiplayer Services Documentation](https://docs.unity.com/ugs/manual/mps-sdk/manual)
- [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Unity Relay](https://docs.unity.com/relay/)
- [Unity Lobby](https://docs.unity.com/lobby/)