using System.Collections.Generic;
using Aurore.LobbyPlugin.Scripts.Multiplayer;
using Aurore.LobbyPlugin.Scripts.Multiplayer.Lobby_;
using Aurore.LobbyPlugin.Scripts.UI.Views;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Aurore.LobbyPlugin.Scripts.UI.Lobby
{
    /// <summary>
    /// Represente une salle que l'on peut rejoindre dans la vue JoinPublicRoom
    /// </summary>
    public sealed class JoinableRoom : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI numberOfPlayersText;
        [SerializeField] private Button joinButton;

        private string _lobbyId;


        public void Initialize(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            roomNameText.text = lobby.Name;
            var maxPlayers = lobby.MaxPlayers;
            var nbPlayers = lobby.Players.Count;
            numberOfPlayersText.text = $"{nbPlayers} / {maxPlayers}";
            
            _lobbyId = lobby.Id;
            
            joinButton.onClick.AddListener(Join);
        }

        private async void Join()
        {
            var player = new PlayerInfos()
            {
                data = new Dictionary<string, PlayerDataObject>()
                {
                    {
                        KeysTypeEnum.KeyPlayerName.ToString(),
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "")
                    },
                    {
                        KeysTypeEnum.KeyPlayerTeam.ToString(),
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                            PlayerTeamEnum.Spectate.ToString())
                    }
                }
            };
            
            try
            { 
                await LobbyManager.Instance.JoinLobbyById(_lobbyId, player); 
                ViewManager.Show<ChoosePseudoView>();
            }
            catch (LobbyServiceException e)
            { 
                ViewManager.Show<ErrorView>();
                var errorView = ViewManager.GetView<ErrorView>(); 
                errorView.SetErrorLobbyServiceExceptionMessage(e);
            }
        }

    }
}
