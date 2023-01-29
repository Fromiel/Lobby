using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe envoyant un event lorsqu'un joueur s'est connecte au serveur
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