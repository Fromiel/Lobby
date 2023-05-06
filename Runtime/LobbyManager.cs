using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fromiel.LobbyPlugin
{
    /// <summary>
    /// Class managing the lobby
    /// </summary>
    public sealed class LobbyManager : MonoBehaviour
    {
        #region Attributes

        public static LobbyManager Instance;

        [Header("Timing of the requests to the server")]
        [Tooltip("Time between each heartBeat sent to the lobby (must be less than 30 seconds)")]
        [SerializeField]
        private float heartBeatTimerMax = 15.0f;
        
        
        [Tooltip("Time between each update of the lobby by the server (must be greater than 1 second if you are using lobby free and not too big otherwise not responsive)")]
        [SerializeField]
        private float lobbyUpdateTimerMax = 1.1f;

        [Header("Connexion to the relay server")]
        [Tooltip("Number of attempts to connect to a relay server")]
        [SerializeField]
        private int maxConnexionTry = 10;
        
        [Header("Region of the relay server")]
        [Tooltip("If you want to specify manually the region of the relay server, check this box (in general it's useless)")]
        [SerializeField]
        private bool specifyRegion;

        [Tooltip("The region of the relay server (if specifyRegion is checked) (to know the regions you can use the method ShowAllRegion of this class)")]
        [SerializeField]
        private string region;
        
        [Header("Local tests of the lobby")]
        [Tooltip("If you want to test the lobby locally, check this box")]
        [SerializeField]
        private bool localTest = true;

        private Lobby _hostLobby; //Lobby of the host if we are host
        private Lobby _joinedLobby; //Lobby joined (if you are host : _joinedLobby = _hostLobby)
        private float _heartBeatTimer;
        private float _lobbyUpdateTimer;
        private int _nbConnexionTry;
        private string _relayCode;

        private const string Waiting = "0"; //Code of the server if the game has not started yet

        #endregion

        #region Events
        
        //Event sent when the player joined a lobby
        public event Action OnRoomJoined;
        
        //Event sent when the lobby is updated
        public event Action<Lobby> OnLobbyUpdated;
        
        //Event sent when the player is connected to a relay server
        public event Action OnBeginConnect;


        #endregion

        #region Monobehaviour callbacks

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (localTest)
            {
                var playerName = "name_" + Random.Range(0, 10000); //Put random number for local test
                Authenticate(playerName); //Initialise services + authenticate (playerName for local test)
            }
            else
            {
                Authenticate(); //Initialise services + authenticate (playerName = null)
            }

            NetworkManager.Singleton.GetComponent<UnityTransport>().OnTransportEvent += OnTransportEvent;
        }

        private void Update()
        {
            HandleLobbyHeartbeat();
            HandleLobbyPollForUpdates();
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.GetComponent<UnityTransport>() != null)
                NetworkManager.Singleton.GetComponent<UnityTransport>().OnTransportEvent -= OnTransportEvent;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Create a lobby from the RoomInfos struct passed in argument
        /// </summary>
        /// <param name="room">Infos of the lobby to create</param>
        /// <param name="playerInfos">Infos of the player</param>
        public async Task CreateLobby(RoomInfos room, PlayerInfos playerInfos)
        {
            try
            {
                //Build lobby data with room data
                var lobbyData = room.data;
                lobbyData.Add(KeysTypeEnum.KeyStartGame.ToString(),
                    new DataObject(DataObject.VisibilityOptions.Member, Waiting)); //Data to if the server has to load the server

                var createLobbyOptions = new CreateLobbyOptions
                {
                    IsPrivate = room.isPrivate,
                    Player = new Player
                    {
                        Data = playerInfos.data
                    },
                    Data = lobbyData
                };
                
                //Create the lobby with the infos in createLobbyOptions
                _hostLobby =
                    await LobbyService.Instance.CreateLobbyAsync(room.lobbyName, room.maxPlayers, createLobbyOptions);
                _joinedLobby = _hostLobby;
                
                //Send event that the player joined a lobby
                SendRoomJoinedEvent();

                Debug.Log("Created lobby! " + _hostLobby.Name + " code : " + _hostLobby.LobbyCode +
                          " Data : play against ai : " +
                          _joinedLobby.Data[KeysTypeEnum.KeyPlayAgainstAI.ToString()].Value);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw; //The exception can be catch to display a feedback to the player
            }
        }

        /// <summary>
        /// Join a lobby with a code
        /// </summary>
        /// <param name="code">Code of the lobby</param>
        /// <param name="playerInfos">Data of the player</param>
        public async Task JoinLobbyByCode(string code, PlayerInfos playerInfos)
        {
            try
            {
                JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
                {
                    Player = new Player
                    {
                        Data = playerInfos.data
                    }
                };
                _joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, joinLobbyByCodeOptions);
                
                //Send event that the player joined a lobby
                SendRoomJoinedEvent();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw;
            }
        }

        /// <summary>
        /// Join a lobby by its id
        /// </summary>
        /// <param name="id">Id of the lobby to join</param>
        /// <param name="playerInfos">Data of the player</param>
        public async Task JoinLobbyById(string id, PlayerInfos playerInfos)
        {
            try
            {
                JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions()
                {
                    Player = new Player
                    {
                        Data = playerInfos.data
                    }
                };
                _joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(id, joinLobbyByIdOptions);
                SendRoomJoinedEvent();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw;
            }
        }


        /// <summary>
        /// Get the list of public lobbies
        /// </summary>
        /// <returns>Return null if there was an error</returns>
        public async Task<List<Lobby>> ListLobbies()
        {
            try
            {
                var queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
                Debug.Log("Lobbies found : " + queryResponse.Results.Count);
                foreach (var lobby in queryResponse.Results)
                {
                    Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
                }

                return queryResponse.Results;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                return null;
            }
        }

        /// <summary>
        /// Quit the lobby (for the local player)
        /// </summary>
        public async void QuitLobby()
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                _joinedLobby = null;
                _hostLobby = null;
                Debug.Log("Lobby leaved");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw;
            }
        }


        /// <summary>
        /// Kick a player from the lobby (only the host can do this)
        /// </summary>
        /// <param name="player"></param>
        public async void KickPlayer(Player player)
        {
            if (!IsHost()) return;

            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_hostLobby.Id, player.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw;
            }
        }

        /// <summary>
        /// To know if we are the host of the lobby
        /// </summary>
        /// <returns>True if we are host, false otherwise or if we aren't in a lobby</returns>
        public bool IsHost()
        {
            var currentPlayerInfo = AuthenticationService.Instance.PlayerInfo;

            return _joinedLobby != null && _joinedLobby.HostId == currentPlayerInfo.Id;
        }

        /// <summary>
        /// Know if the player passed in argument is the host
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True if he is host, false otherwise or if we called this method outside a lobby</returns>
        public bool IsHost(Player player)
        {
            return _joinedLobby != null && _joinedLobby.HostId == player.Id;
        }


        /// <summary>
        /// Launch a game
        /// </summary>
        public async void StartGame()
        {
            if (!IsHost() || _joinedLobby.Players.Count < _joinedLobby.MaxPlayers) return;

            try
            {
                string relayCode = await CreateRelay();

                await SetLobbyValue(KeysTypeEnum.KeyStartGame,
                    new DataObject(DataObject.VisibilityOptions.Member, relayCode));

                _hostLobby = null;
                _joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

        /// <summary>
        /// Return the data of the lobby associated to the key passed in argument (return null if no key exists)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetLobbyValue(KeysTypeEnum key)
        {
            if (_joinedLobby == null || !_joinedLobby.Data.ContainsKey(key.ToString())) return null;

            return _joinedLobby?.Data[key.ToString()].Value;
        }

        /// <summary>
        /// In the lobby, set the data associated to the key passed in argument by the value passed in argument
        /// Only the host can change data of the lobby
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        public async Task SetLobbyValue(KeysTypeEnum key, DataObject newValue)
        {
            if (_hostLobby == null) return;

            try
            {
                var newData = _hostLobby.Data;
                newData[key.ToString()] = newValue;

                _hostLobby = await Lobbies.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = newData
                });

                _joinedLobby = _hostLobby;

                SendLobbyUpdatedEvent(_joinedLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw;
            }
        }

        /// <summary>
        /// Get the value of a data of a player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="key">Key of the data</param>
        /// <returns>Return the value if the key exist, null otherwise</returns>
        public string GetPlayerValue(Player player, KeysTypeEnum key)
        {
            if (!player.Data.ContainsKey(key.ToString())) return null;
            return player.Data[key.ToString()].Value;
        }

        /// <summary>
        /// Get the value of a data of the local player
        /// </summary>
        /// <param name="key">Key of the data</param>
        /// <returns></returns>
        public string GetPlayerValue(KeysTypeEnum key)
        {
            if (_joinedLobby == null) return null;

            foreach (var player in _joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerInfo.Id)
                    return GetPlayerValue(player, key);
            }

            return null;
        }

        /// <summary>
        /// Set the value of a data of a player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="key">Key of the data to set</param>
        /// <param name="value">New value of the data</param>
        public async Task SetPlayerValue(Player player, KeysTypeEnum key, PlayerDataObject value)
        {
            if (_joinedLobby == null) return;

            try
            {
                var playerId = player.Id;
                var newData = player.Data;
                newData[key.ToString()] = value;

                UpdatePlayerOptions updatePlayerOptions = new UpdatePlayerOptions
                {
                    Data = newData
                };

                var lobby = await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, playerId,
                    updatePlayerOptions);

                _joinedLobby = lobby;

                SendLobbyUpdatedEvent(_joinedLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw;
            }
        }

        /// <summary>
        /// Set the value of a data of the local player
        /// </summary>
        /// <param name="key">Key of the data to set</param>
        /// <param name="value">New value of the data</param>
        public async Task SetPlayerValue(KeysTypeEnum key, PlayerDataObject value)
        {
            if (_joinedLobby == null) return;

            try
            {
                foreach (var player in _joinedLobby.Players)
                {
                    if (player.Id != AuthenticationService.Instance.PlayerInfo.Id) continue;

                    await SetPlayerValue(player, key, value);
                    return;
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
                throw;
            }
        }
        
        /// <summary>
        /// Show all available regions for relay servers
        /// </summary>
        /// <returns></returns>
        public IEnumerator ShowAllRegion()
        {
            // Request list of valid regions
            var regionsTask = Relay.Instance.ListRegionsAsync();

            while (!regionsTask.IsCompleted)
            {
                yield return null;
            }

            if (regionsTask.IsFaulted)
            {
                Debug.LogError("List regions request failed");
                yield break;
            }

            var regionList = regionsTask.Result;

            foreach (var r in regionList)
            {
                Debug.Log(r.Id);
            }
        }


        #endregion

        #region Private methods

        /// <summary>
        /// Handle sending heartbeats to the lobby
        /// </summary>
        private async void HandleLobbyHeartbeat()
        {
            if (_hostLobby == null) return;

            _heartBeatTimer -= Time.deltaTime;

            if (!(_heartBeatTimer < 0f)) return;

            _heartBeatTimer = heartBeatTimerMax;
            await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
        }

        /// <summary>
        /// Handle requests to get lobby updates
        /// </summary>
        private async void HandleLobbyPollForUpdates()
        {
            if (_joinedLobby == null) return;

            _lobbyUpdateTimer -= Time.deltaTime;

            if (!(_lobbyUpdateTimer < 0f)) return;

            _lobbyUpdateTimer = lobbyUpdateTimerMax;
            _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);

            if (_joinedLobby.Data[KeysTypeEnum.KeyStartGame.ToString()].Value != Waiting)
            {
                if (!IsHost())
                    JoinRelay(_joinedLobby.Data[KeysTypeEnum.KeyStartGame.ToString()].Value);

                _joinedLobby = null;
                return;
            }

            if (IsHost())
                _hostLobby = _joinedLobby;

            SendLobbyUpdatedEvent(_joinedLobby);
        }

        /// <summary>
        /// Initialise Unity Services and authenticate
        /// </summary>
        /// <param name="playerName"></param>
        private async void Authenticate(string playerName = null)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            if (playerName != null)
                initializationOptions.SetProfile(playerName);

            try
            {
                await UnityServices.InitializeAsync(initializationOptions);

                AuthenticationService.Instance.SignedIn += () =>
                {
                    Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
                };

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        

        /// <summary>
        /// Create a relay
        /// </summary>
        /// <returns>Code to join the relay created if the creation of the relay succeeded, null otherwise</returns>
        private async Task<string> CreateRelay()
        {
            try
            {
                SendBeginConnectEvent();
                ChangeScene.MaxPlayers = _hostLobby.MaxPlayers;

                Allocation allocation;
                if (specifyRegion)
                    allocation = await RelayService.Instance.CreateAllocationAsync(10, region);
                else
                    allocation = await RelayService.Instance.CreateAllocationAsync(10);

                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                Debug.Log("JoinCode : " + joinCode);

                RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();

                return joinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
                return null;
            }

        }

        /// <summary>
        /// Join a relay with a join code
        /// </summary>
        /// <param name="joinCode">Code of the relay to join</param>
        private async void JoinRelay(string joinCode)
        {
            try
            {
                SendBeginConnectEvent();
                Debug.Log(joinCode);
                _relayCode = joinCode;
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
            }
        }


        /// <summary>
        /// Method called when the OnTransportEvent event of the unityTransport is triggered
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="clientId"></param>
        /// <param name="payload"></param>
        /// <param name="receiveTime"></param>
        private void OnTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            Debug.Log("event type : " + eventType.ToString());
            if (eventType == NetworkEvent.Disconnect)
            {
                if (_nbConnexionTry < maxConnexionTry)
                {
                    Debug.Log("Connexion failed, retry connexion ...");
                    Debug.Log("nbConnexionTry : " + _nbConnexionTry);
                    NetworkManager.Singleton.Shutdown();
                    JoinRelay(_relayCode);
                    _nbConnexionTry++;
                }
                else
                {
                    Debug.LogError("Connexion failed !");
                }
            }
        }

        #endregion

        #region Call events

        private void SendRoomJoinedEvent()
        {
            OnRoomJoined?.Invoke();
        }

        private void SendLobbyUpdatedEvent(Lobby lobby)
        {
            OnLobbyUpdated?.Invoke(lobby);
        }

        private void SendBeginConnectEvent()
        {
            OnBeginConnect?.Invoke();
        }

        #endregion
    }
}