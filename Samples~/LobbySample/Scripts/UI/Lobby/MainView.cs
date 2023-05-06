using System.Collections.Generic;
using Lobby.Multiplayer;
using Lobby.UI.Views;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Fromiel.LobbyPlugin;

namespace Lobby.UI.Lobby
{
    /// <summary>
    /// Class representing the main view in the lobby
    /// </summary>
    public sealed class MainView : View
    {
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinPrivateRoomButton;
        [SerializeField] private Button joinPublicRoomButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_InputField roomIdInputField;
        [SerializeField] private string mainMenuScene;
        
        public override void Initialize()
        {
            backButton.onClick.AddListener(Back);
            createRoomButton.onClick.AddListener(CreateRoom);
            joinPublicRoomButton.onClick.AddListener(JoinPublicRoom);
            joinPrivateRoomButton.onClick.AddListener(JoinPrivateRoom);
        }


        private void Back()
        {
            SceneManager.LoadScene(mainMenuScene);
        }


        private void CreateRoom()
        {
            ViewManager.Show<CreateRoomView>(true);
        }


        private void JoinPublicRoom()
        {
            ViewManager.Show<JoinPublicRoomView>(true);
        }


        private async void JoinPrivateRoom()
        {
            if (roomIdInputField.text.Length < 1)
            {
                ViewManager.Show<ErrorView>();
                var errorView = ViewManager.GetView<ErrorView>();
                errorView.SetMessageEmptyRoomCode();
                return;
            }
            
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
                await LobbyManager.Instance.JoinLobbyByCode(roomIdInputField.text, player);
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
