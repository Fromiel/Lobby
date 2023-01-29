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

/// <summary>
/// Classe gérant le lobby
/// </summary>
public sealed class LobbyManager : MonoBehaviour
{
    #region Attributes
        
    public static LobbyManager Instance; //Lobby est un singleton

    [Header("Timing des requetes aux serveurs")]
    [Tooltip("Temps entre chaque envoie de heartBeat au lobby (doit etre inferieur à 30 secondes)")]
    [SerializeField] private float heartBeatTimerMax = 15.0f; //Temps entre chaque envoie de "heartBeat" au lobby
    [Tooltip("Temps entre chaque mise a jour du lobby par rapport au serveur (doit etre superieur à 1 seconde car sinon c'est payant et pas trop grand sinon pas assez responsive)")]
    [SerializeField] private float lobbyUpdateTimerMax = 1.1f; //Temps entre chaque update local du lobby

    [Header("Connexion au serveur relay")]
    [Tooltip("Nombre de tentatives de connexion à un serveur relay")]
    [SerializeField] private int maxConnexionTry = 10; //Nombre de tentatives de connexion à un serveur relay
        
    //Si on veut specifier la region dans laquelle on veut lancer le serveur relay
    [Header("Region du serveur relay")]
    [Tooltip("Faut cocher cette case pour que la region spécifiée en dessous soit prise en compte (en theorie ca sert à rien)")]
    [SerializeField] private bool specifyRegion;
    [Tooltip("Pour savoir les regions disponibles, faut utiliser la methode ShowAllRegion de ce script")]
    [SerializeField] private string region;

    private Lobby _hostLobby; //lobby de l'host si on est host
    private Lobby _joinedLobby; //lobby rejoint (si host n'est pas null, _joinedLobby = _hostLobby)
    private float _heartBeatTimer; 
    private float _lobbyUpdateTimer;
    private int _nbConnexionTry;
    private string _relayCode;
        
    private const string Waiting = "0"; //Code du serveur si la partie n'a pas encore ete lancee

    #endregion

    #region Events
        
    //Event lorsque l'on a rejoint un lobby
    public delegate void OnRoomJoined();
    public event OnRoomJoined RoomJoined;  
        
    //Event envoye lors de la mis a jour du lobby
    public delegate void OnLobbyUpdated(Lobby lobby);
    public event OnLobbyUpdated LobbyUpdated; 
        
    //Event envoye lorsque le joueur se connecte à un serveur relay
    public delegate void OnBeginConnect();
    public event OnBeginConnect BeginConnect;


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
        string playerName = "nom_" + Random.Range(0, 10000); //On met un nombre aleatoire pour les test en local
        Authenticate(playerName); //On initialise les services + on s'authentifie (playername pour les tests en local)
        NetworkManager.Singleton.GetComponent<UnityTransport>().OnTransportEvent += OnTransportEvent;
    }
        
    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private void OnDestroy()
    {
        if(NetworkManager.Singleton.GetComponent<UnityTransport>() != null)
            NetworkManager.Singleton.GetComponent<UnityTransport>().OnTransportEvent -= OnTransportEvent;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Methode pour creer un lobby à partir du struct RoomInfos passé en argument
    /// </summary>
    /// <param name="room">Les infos du lobby à créer</param>
    /// <param name="playerInfos">Les infos du player</param>
    public async Task CreateLobby(RoomInfos room, PlayerInfos playerInfos)
    {
        try
        {
            //On construit les infos du lobby à l'aide des données de room
            var lobbyData = room.data;
            lobbyData.Add(KeysTypeEnum.KeyStartGame.ToString(), new DataObject(DataObject.VisibilityOptions.Member, Waiting)); //Info pour savoir si le client doit charger le serveur
                
            var createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = room.isPrivate,
                Player = new Player
                {
                    Data = playerInfos.data
                },
                Data = lobbyData
            };
                
            //On cree le lobby à l'aide des infos que l'on a mis dans createLobbyOptions
            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(room.lobbyName, room.maxPlayers, createLobbyOptions);
            _joinedLobby = _hostLobby;

            //On envoie l'event que l'on a rejoint un lobby
            SendRoomJoinedEvent();

            Debug.Log("Created lobby! " + _hostLobby.Name + " code : " + _hostLobby.LobbyCode + " Data : play against ai : " + _joinedLobby.Data[KeysTypeEnum.KeyPlayAgainstAI.ToString()].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            throw; //Pour etre recuperer par l'ui et lui permettre d'afficher un feedback au joueur
        }
    }

    /// <summary>
    /// Methode pour rejoindre un lobby avec un code
    /// </summary>
    /// <param name="code">Code du lobby</param>
    /// <param name="playerInfos">Les infos du player</param>
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
                
            //On envoie l'event que l'on a rejoint un lobby
            SendRoomJoinedEvent();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            throw;
        }
    }

    /// <summary>
    /// Methode pour rejoindre un lobby par son id
    /// </summary>
    /// <param name="id">L'id du lobby à rejoindre</param>
    /// <param name="playerInfos">Les infos du player</param>
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
    /// Methode retournant la liste des lobbies publics
    /// </summary>
    /// <returns>La liste des lobbies publics, null si il y a une erreur</returns>
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
    /// Methode pour quitter le lobby
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
    /// Methode pour kick un joueur
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
    /// Methode pour savoir si on est l'host du lobby
    /// </summary>
    /// <returns>True si on est l'host du lobby, false sinon ou si l'on n'est pas dans un lobby</returns>
    public bool IsHost()
    {
        var currentPlayerInfo = AuthenticationService.Instance.PlayerInfo;
            
        return _joinedLobby != null && _joinedLobby.HostId == currentPlayerInfo.Id;
    }
        
    /// <summary>
    /// Methode pour savoir si le joueur passé en argument est l'host
    /// </summary>
    /// <param name="player"></param>
    /// <returns>True si il est l'host du lobby, false sinon ou si l'on n'est pas dans un lobby</returns>
    public bool IsHost(Player player)
    {
        return _joinedLobby != null && _joinedLobby.HostId == player.Id;
    }
        

    /// <summary>
    /// Methode pour lancer une partie
    /// </summary>
    public async void StartGame()
    {
        if (!IsHost() || _joinedLobby.Players.Count < _joinedLobby.MaxPlayers) return;

        try
        {
            string relayCode = await CreateRelay();

            await SetLobbyValue(KeysTypeEnum.KeyStartGame, new DataObject(DataObject.VisibilityOptions.Member, relayCode));

            _hostLobby = null;
            _joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Retourne la donnee du lobby associe à la cle passee en argument (retourne null si aucune cle existe)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string GetLobbyValue(KeysTypeEnum key)
    {
        if (_joinedLobby == null || !_joinedLobby.Data.ContainsKey(key.ToString())) return null;

        return _joinedLobby?.Data[key.ToString()].Value;
    }

    /// <summary>
    /// Methode pour set la donnee du lobby associe a la cle passe en argument par la valeur passe en argument
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
    /// Methode pour obtenir la valeur d'une donnee d'un player
    /// </summary>
    /// <param name="player">Player dont on veut connaitre une donnee</param>
    /// <param name="key">Cle de la donnee a obtenir</param>
    /// <returns></returns>
    public string GetPlayerValue(Player player, KeysTypeEnum key)
    {
        if (!player.Data.ContainsKey(key.ToString())) return null;
        return player.Data[key.ToString()].Value;
    }

    /// <summary>
    /// Methode pour obtenir la valeur d'une donnee du joueur local
    /// </summary>
    /// <param name="key">Cle de la donnee a obtenir</param>
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
    /// Methode pour set la valeur d'une donnee d'un player
    /// </summary>
    /// <param name="player">Player dont on veut set une donnee</param>
    /// <param name="key">Cle de la donnee a set</param>
    /// <param name="value">Nouvelle valeur de la donnee</param>
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

            var lobby = await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, playerId, updatePlayerOptions);

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
    /// Methode pour set la valeur d'une donnee du joueur local
    /// </summary>
    /// <param name="key">Cle de la donnee a set</param>
    /// <param name="value">Nouvelle valeur de la donnee</param>
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
        

    #endregion

    #region Private methods

    /// <summary>
    /// Methode gerant l'envoie de battements de coeur au lobby
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
    /// Methode chargee de gerer les requetes pour obtenir des mises a jour du lobby
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
    /// Methode pour initialiser les services unity et pour s'authentifier
    /// </summary>
    /// <param name="playerName"></param>
    private async void Authenticate(string playerName = null)
    {
        InitializationOptions initializationOptions = new InitializationOptions();
        if(playerName != null)
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
    /// Methode montrant toutes les regions disponibles pour les servers relays
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShowAllRegion()
    { 
        // Request list of valid regions
        var regionsTask = Relay.Instance.ListRegionsAsync();

        while(!regionsTask.IsCompleted)
        {
            yield return null;
        }

        if (regionsTask.IsFaulted)
        {
            Debug.LogError("List regions request failed");
            yield break;
        }

        var regionList = regionsTask.Result;

        foreach(var r in regionList)
        {
            Debug.Log(r.Id);
        }
    }

    /// <summary>
    /// Methode pour creer un relay
    /// </summary>
    /// <returns>Le code pour rejoindre ce relay</returns>
    private async Task<string> CreateRelay()
    {
        try
        {
            SendBeginConnectEvent();
            ChangeScene.MaxPlayers = _hostLobby.MaxPlayers;
                
            Allocation allocation;
            if(specifyRegion)
                allocation = await RelayService.Instance.CreateAllocationAsync(10, region);
            else
                allocation = await RelayService.Instance.CreateAllocationAsync(10);

            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                
            Debug.Log("Joincode : " + joinCode);

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
    /// Methode pour rejoindre un relay
    /// </summary>
    /// <param name="joinCode">Code du relay a rejoindre</param>
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
    /// Methode appelee lorsqu'un l'event OnTransportEvent du unityTransport est declenche
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="clientId"></param>
    /// <param name="payload"></param>
    /// <param name="receiveTime"></param>
    private void OnTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
    {
        Debug.Log("event type : "+eventType.ToString());
        if (eventType == NetworkEvent.Disconnect)
        {
            if (_nbConnexionTry < maxConnexionTry)
            {
                Debug.Log("Connexion failed, retry connexion ...");
                Debug.Log("nbConnexionTry : "+_nbConnexionTry);
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
        RoomJoined?.Invoke();
    }

    private void SendLobbyUpdatedEvent(Lobby lobby)
    {
        LobbyUpdated?.Invoke(lobby);
    }

    private void SendBeginConnectEvent()
    {
        BeginConnect?.Invoke();
    }

    #endregion
}