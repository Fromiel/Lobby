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
    /// View to create a room
    /// </summary>
    public sealed class CreateRoomView : View
    {
        [SerializeField] private TMP_InputField roomNameInputField;
        [SerializeField] private TextMeshProUGUI numberOfPlayerEachTeamText;
        [SerializeField] private Toggle isPrivateRoom;
        [SerializeField] private Toggle isAgainstAI;
        [SerializeField] private Scrollbar numberOfPlayerScrollBar;
        [SerializeField] private Button createButton;
        [SerializeField] private Button backButton;

        private int _nbPlayerPerTeam = 2;
        
        public override void Initialize()
        {
            createButton.onClick.AddListener(CreateRoom);
            numberOfPlayerScrollBar.onValueChanged.AddListener(OnScrollBarChanged);
            backButton.onClick.AddListener(ViewManager.ShowLast);
            numberOfPlayerEachTeamText.text = "Number of players for each team : 2";
        }

        private void OnScrollBarChanged(float newVal)
        {
            //There are only 2 choices : 2 or 3 players per team
            if (newVal < 0.5)
                _nbPlayerPerTeam = 2;
            else
                _nbPlayerPerTeam = 3;

            numberOfPlayerEachTeamText.text = $"Number of player for each team : {_nbPlayerPerTeam}";
        }

        private async void CreateRoom()
        {
            int maximumPlayers;
            if (isAgainstAI.isOn)
                maximumPlayers = _nbPlayerPerTeam;
            else
                maximumPlayers = _nbPlayerPerTeam * 2;

            if (roomNameInputField.text.Length < 1)
            {
                ViewManager.Show<ErrorView>();
                var errorView = ViewManager.GetView<ErrorView>();
                errorView.SetMessageEmptyRoomName();
                return;
            }
            
            var room = new RoomInfos()
            {
                isPrivate = isPrivateRoom.isOn,
                lobbyName = roomNameInputField.text,
                maxPlayers = maximumPlayers,
                data = new Dictionary<string, DataObject>
                {
                    { KeysTypeEnum.KeyPlayAgainstAI.ToString(), new DataObject(DataObject.VisibilityOptions.Member, isAgainstAI.isOn.ToString())}
                }
            };

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
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerTeamEnum.Spectate.ToString())
                    }
                }
            };

            try
            {
                await LobbyManager.Instance.CreateLobby(room, player);
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
