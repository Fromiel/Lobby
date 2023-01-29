using Aurore.LobbyPlugin.Scripts.UI.Views;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace Aurore.LobbyPlugin.Scripts.UI.Lobby
{
    /// <summary>
    /// Vue des erreurs
    /// </summary>
    public class ErrorView : View
    {
        [SerializeField] private TextMeshProUGUI contentErrorText;
        [SerializeField] private Button backButton;
        public override void Initialize()
        {
            backButton.onClick.AddListener(ViewManager.ShowLast);
        }

        public void SetErrorLobbyServiceExceptionMessage(LobbyServiceException e)
        {
            switch (e.Reason)
            {
                case LobbyExceptionReason.LobbyNotFound:
                    contentErrorText.text = "Lobby not found";
                    break;
                case LobbyExceptionReason.LobbyFull:
                    contentErrorText.text = "The lobby is full";
                    break;
                case LobbyExceptionReason.ValidationError:
                    contentErrorText.text = "Error when connecting to the lobby";
                    break;
                case LobbyExceptionReason.RateLimited:
                    contentErrorText.text = "ARRETE DE SPAMMER !!!!!!";
                    break;
                default:
                    contentErrorText.text = e.Reason.ToString();
                    break;
            }
        }

        public void SetMessageEmptyPseudo()
        {
            contentErrorText.text = "Can't have an empty name";
        }

        public void SetMessageEmptyRoomName()
        {
            contentErrorText.text = "Can't have an empty room name";
        }

        public void SetMessageEmptyRoomCode()
        {
            contentErrorText.text = "Can't have an empty room code";
        }
    }
}
