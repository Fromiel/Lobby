using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Aurore.LobbyPlugin.Scripts.Multiplayer.Lobby_
{
    /// <summary>
    /// Les infos d'un player
    /// </summary>
    public struct PlayerInfos
    {
        public Dictionary<string, PlayerDataObject> data;
    }
}
