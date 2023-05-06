using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Fromiel.LobbyPlugin
{
    /// <summary>
    /// Datas of a lobby to create
    /// </summary>
    public struct RoomInfos
    {
        public string lobbyName;
        public int maxPlayers;
        public bool isPrivate;
        public Dictionary<string, DataObject> data;
    }
}