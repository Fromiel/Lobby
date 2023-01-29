using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aurore.LobbyPlugin.Scripts.Multiplayer.Lobby_
{
    /// <summary>
    /// Classe pour charger la scene du jeu pour tous les joueurs
    /// </summary>
    public class ChangeScene : NetworkBehaviour
    {
        [SerializeField] private string changeScene;

        private int _nbPlayers;
        private static int _maxPlayers;

        public static int MaxPlayers
        {
            set => _maxPlayers = value;
        }

        private void Start()
        {
            PlayerCheck.ConnectedToServer += HandleConnexion;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            PlayerCheck.ConnectedToServer -= HandleConnexion;
        }

        private void HandleConnexion(PlayerCheck player)
        {
            _nbPlayers++;
            Debug.Log("Nb players : " + _nbPlayers);
            if (_nbPlayers == _maxPlayers)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(changeScene, LoadSceneMode.Single);
            }
        }
    }
}
