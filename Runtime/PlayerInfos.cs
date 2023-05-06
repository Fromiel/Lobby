using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Fromiel.LobbyPlugin
{
    /// <summary>
    /// Datas of a player
    /// </summary>
    public struct PlayerInfos
    {
        public Dictionary<string, PlayerDataObject> data;
    }
}