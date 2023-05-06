using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.UI.Lobby
{
    /// <summary>
    /// Represent the player information in the team table in the JoinedLobbyView
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
