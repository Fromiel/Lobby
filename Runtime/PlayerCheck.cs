using Unity.Netcode;
using UnityEngine;

namespace Fromiel.LobbyPlugin
{
    /// <summary>
    /// Class sending an event when a player is connected to the server
    /// </summary>
    public class PlayerCheck : NetworkBehaviour
    {
        public delegate void OnConnectedToServer(PlayerCheck player);

        public static event OnConnectedToServer ConnectedToServer;

        private void Start()
        {
            if (!IsHost) return;

            Debug.Log("Player connected");
            ConnectedToServer?.Invoke(this);
        }
    }
}