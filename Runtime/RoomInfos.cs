using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Les infos d'un lobby à créer
/// </summary>
public struct RoomInfos
{
    public string lobbyName;
    public int maxPlayers;
    public bool isPrivate;
    public Dictionary<string, DataObject> data;
}