using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private Dictionary<string, string> roomCodeToLobbyId = new Dictionary<string, string>();
    
    public void RegisterRoomCode(string roomCode, string lobbyId)
    {
        if (!roomCodeToLobbyId.ContainsKey(roomCode))
        {
            roomCodeToLobbyId[roomCode] = lobbyId;
        }
    }
    
    public bool ValidateRoomCode(string roomCode, string lobbyId)
    {
        if (roomCodeToLobbyId.ContainsKey(roomCode))
        {
            return roomCodeToLobbyId[roomCode] == lobbyId;
        }
        
        return false;
    }
    
    public void UnregisterRoomCode(string roomCode)
    {
        if (roomCodeToLobbyId.ContainsKey(roomCode))
        {
            roomCodeToLobbyId.Remove(roomCode);
        }
    }
    
    public string GetLobbyIdFromRoomCode(string roomCode)
    {
        return roomCodeToLobbyId.ContainsKey(roomCode) ? roomCodeToLobbyId[roomCode] : null;
    }
}