using System.Collections.Generic;
using Lobby.Multiplayer;
using Lobby.UI.Views;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using Fromiel.LobbyPlugin;
using Fromiel.Keys;

namespace Lobby.UI.Lobby
{
    /// <summary>
    /// Represent a room that can be joined in the JoinPublicRoom view
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
