using System;
using Lobby.Multiplayer;
using Lobby.UI.Views;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using Fromiel.LobbyPlugin;
using Fromiel.Keys;

namespace Lobby.UI.Lobby
{
    /// <summary>
    /// View of the player when is joined in a lobby
    /// </summary>
    public sealed class JoinedLobbyView : View
    {
        #region Attributes
        
        [SerializeField] private Button playButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TextMeshProUGUI nbPlayersText;
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private VerticalLayoutGroup aTeamLayout;
        [SerializeField] private Button joinATeamButton;
        [SerializeField] private VerticalLayoutGroup bTeamLayout;
        [SerializeField] private Button joinBTeamButton;

        [SerializeField] private Transform uiPlayerPrefab;

        private Unity.Services.Lobbies.Models.Lobby _joinedLobby;
        private int _nbPlayersATeam;
        private int _nbPlayersBTeam;
        private int _maxPlayerEachTeam;
        private int _maxPlayers;
        private bool _connecting;

        public int MaxPlayers => _maxPlayers;

        #endregion

        #region Views override methods

        public override void Initialize()
        {
            LobbyManager.Instance.OnLobbyUpdated += UpdateUiWithLobbyInfos;
            LobbyManager.Instance.OnBeginConnect += OnBeginConnect;
            
            playButton.onClick.AddListener(Play);
            
            leaveButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.QuitLobby();
                ViewManager.Show<MainView>(false);
            });
            
            joinATeamButton.onClick.AddListener(() => JoinTeam(true));
            joinBTeamButton.onClick.AddListener(() => JoinTeam(false));
            
        }

        #endregion

        #region Monobehaviour callbacks

        private void OnDestroy()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnLobbyUpdated -= UpdateUiWithLobbyInfos;
                LobbyManager.Instance.OnBeginConnect -= OnBeginConnect;
            }
        }

        #endregion
        

        #region Private methods

        /// <summary>
        /// Method called when the lobby is updated, update the ui
        /// </summary>
        /// <param name="lobby"></param>
        private void UpdateUiWithLobbyInfos(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            _joinedLobby = lobby;
            _maxPlayers = lobby.MaxPlayers;

            if (!LobbyManager.Instance.IsHost())
            {
                playButton.interactable = false;
            }
            else
            {
                playButton.interactable = !_connecting && _nbPlayersATeam + _nbPlayersBTeam == _maxPlayers;
            }

            if (bool.Parse(LobbyManager.Instance.GetLobbyValue(KeysTypeEnum.KeyPlayAgainstAI)))
            {
                _maxPlayerEachTeam = _joinedLobby.MaxPlayers;
                bTeamLayout.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                _maxPlayerEachTeam = _joinedLobby.MaxPlayers / 2;
                bTeamLayout.transform.parent.gameObject.SetActive(true);
            }
            
            SetRoomName(_joinedLobby.Name);
            SetNbPlayers(_joinedLobby.Players.Count, _joinedLobby.MaxPlayers);
            SetId(_joinedLobby.LobbyCode);
            SetTeamsLayout();
        }


        /// <summary>
        /// Method to update the ui of the number of players
        /// </summary>
        /// <param name="nbPlayers"></param>
        /// <param name="maxPlayers"></param>
        private void SetNbPlayers(int nbPlayers, int maxPlayers)
        {
            nbPlayersText.text = $"Number of players : {nbPlayers} / {maxPlayers}";
        }

        /// <summary>
        /// Method to update the ui of the room id
        /// </summary>
        /// <param name="id"></param>
        private void SetId(string id)
        {
            idText.text = $"Room id : {id}";
        }

        private void Play()
        {
            if (_nbPlayersATeam + _nbPlayersBTeam < _maxPlayers) return;
            
            LobbyManager.Instance.StartGame();
        }

        private void SetRoomName(string roomName)
        {
            roomNameText.text = roomName;
        }

        private void SetTeamsLayout()
        {
            ClearChildren(aTeamLayout.transform);
            ClearChildren(bTeamLayout.transform);

            _nbPlayersATeam = 0;
            _nbPlayersBTeam = 0;

            foreach (var player in _joinedLobby.Players)
            {
                var team = Enum.Parse<PlayerTeamEnum>(LobbyManager.Instance.GetPlayerValue(player, KeysTypeEnum.KeyPlayerTeam));
                if (team == PlayerTeamEnum.A)
                {
                    _nbPlayersATeam++;
                    var go = Instantiate(uiPlayerPrefab, aTeamLayout.transform);
                    go.GetComponent<ShowPlayerInfo>().Initialize(LobbyManager.Instance.IsHost(player), LobbyManager.Instance.GetPlayerValue(player, KeysTypeEnum.KeyPlayerName));
                }
                else if (team == PlayerTeamEnum.B)
                {
                    _nbPlayersBTeam++;
                    var go = Instantiate(uiPlayerPrefab, bTeamLayout.transform);
                    go.GetComponent<ShowPlayerInfo>().Initialize(LobbyManager.Instance.IsHost(player), LobbyManager.Instance.GetPlayerValue(player, KeysTypeEnum.KeyPlayerName));
                }
            }
        }


        private void ClearChildren(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                Destroy(t.GetChild(i).gameObject);
            }
        }

        private async void JoinTeam(bool isATeam)
        {
            if (isATeam && _nbPlayersATeam >= _maxPlayerEachTeam) return;
            if (!isATeam && _nbPlayersBTeam >= _maxPlayerEachTeam) return;
            
            var team = isATeam ? PlayerTeamEnum.A : PlayerTeamEnum.B;

            await LobbyManager.Instance.SetPlayerValue(KeysTypeEnum.KeyPlayerTeam, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, team.ToString()));
        }


        private void OnBeginConnect()
        {
            playButton.interactable = false;
            _connecting = true;
        }
       
       #endregion
    }
}
