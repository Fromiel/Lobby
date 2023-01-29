using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aurore.LobbyPlugin.Scripts.UI.Lobby
{
    /// <summary>
    /// Represente les informations d'un joueur qui sont indiquees dans un tableau d'equipe dans la vue JoinedLobbyView
    /// </summary>
    public sealed class ShowPlayerInfo : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI pseudoText;
        [SerializeField] private Image isHostImage;

        public void Initialize(bool isHost, string pseudo)
        {
            pseudoText.text = pseudo;
            isHostImage.gameObject.SetActive(isHost);
        }
    }
}
