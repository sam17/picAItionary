# Unity Editor Setup Guide

## Quick Fix for Compilation Errors

Based on your screenshot, I've fixed the compilation errors and created a simplified networking setup that works with Unity Relay.

## What Was Fixed

1. **Removed problematic files** that had missing type definitions
2. **Created simplified scripts**:
   - `RelayManager.cs` - Handles Unity Relay allocations
   - `SimpleNetworkManager.cs` - Alternative simplified network manager
   - `SimpleRelayTest.cs` - Testing script for quick validation
3. **Updated GameNetworkManager.cs** to work with or without Relay

## Unity Editor Setup Steps

### 1. Link Unity Services
1. Open **Edit > Project Settings > Services**
2. Sign in to Unity (create account if needed)
3. Click **Link** to connect your project
4. Enable these services in Unity Dashboard:
   - **Authentication** ✓
   - **Relay** ✓

### 2. Scene Setup

#### Option A: Use Existing GameNetworkManager
1. Find your **NetworkManager** GameObject in the scene
2. Add the **RelayManager** script to the same GameObject (or create a new one)
3. Ensure NetworkManager has:
   - NetworkManager component
   - UnityTransport component
   - Your GameNetworkManager script

#### Option B: Quick Test Setup
1. Create empty GameObject called "TestNetworking"
2. Add components:
   - NetworkManager
   - UnityTransport
   - SimpleRelayTest
3. Set NetworkManager's NetworkTransport to the UnityTransport component
4. Use the test buttons in Inspector to create/join rooms

### 3. NetworkManager Configuration
1. Select your NetworkManager GameObject
2. In NetworkManager component:
   - Set **Network Transport** to UnityTransport
   - Add your **Player Prefab** to NetworkPrefabs list
3. In UnityTransport component:
   - Protocol Type: **Unity Transport** (should be default)
   - Connection Type will be set automatically by code

### 4. Test the Setup

#### Using SimpleRelayTest (Quickest):
1. Add SimpleRelayTest to NetworkManager GameObject
2. In Play Mode:
   - Check "Test Create Host" to create a room
   - Copy the join code from Inspector
   - In another instance: paste code in "Join Code Input" and check "Test Join Client"

#### Using Your Main Menu:
1. Ensure GameNetworkManager and RelayManager are in scene
2. Play and click "Host"
3. Room code will be displayed
4. In another instance, use that code to join

## Troubleshooting

### "Failed to create Relay allocation"
- Check Unity Services are linked in Project Settings
- Verify Authentication and Relay are enabled in Dashboard
- Check internet connection

### "RelayManager not found"
- Add RelayManager script to a GameObject in your scene
- Make sure it's on a GameObject that doesn't get destroyed

### "UnityTransport component not found"
- Add UnityTransport component to NetworkManager GameObject
- Set it as the Network Transport in NetworkManager

## Testing Checklist

- [ ] Unity Services linked in Project Settings
- [ ] Authentication service enabled
- [ ] Relay service enabled
- [ ] RelayManager in scene
- [ ] NetworkManager has UnityTransport
- [ ] Player prefab in NetworkPrefabs list
- [ ] Can create room and get 6-character code
- [ ] Can join with code from another instance

## How It Works

1. **Host creates room**:
   - Authenticates anonymously
   - Creates Relay allocation (gets server assignment)
   - Gets 6-character join code
   - Configures transport with Relay data
   - Starts NetworkManager as host

2. **Client joins room**:
   - Authenticates anonymously
   - Joins Relay allocation using code
   - Configures transport with Relay data
   - Starts NetworkManager as client

3. **Connection established** through Unity Relay servers (NAT punchthrough handled automatically)

## Next Steps

Once working:
1. Remove test scripts (SimpleRelayTest, SimpleNetworkManager)
2. Use GameNetworkManager with RelayManager for production
3. Test with builds on different networks
4. Add error handling UI elements