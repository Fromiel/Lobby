using Aurore.LobbyPlugin.Scripts.Multiplayer.Lobby_;
using Aurore.LobbyPlugin.Scripts.UI.Views;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Aurore.LobbyPlugin.Scripts.UI.Lobby
{
    /// <summary>
    /// Vue du choix de pseudo
    /// </summary>
    public sealed class ChoosePseudoView : View
    {
        [SerializeField] private TMP_InputField choosePseudoInputField;
        [SerializeField] private Button chooseButton;
        [SerializeField] private Button backButton;

        public override void Initialize()
        {
            chooseButton.onClick.AddListener(ChoosePseudo);
            backButton.onClick.AddListener(() =>
            {
                ViewManager.ShowLast();
                LobbyManager.Instance.QuitLobby();
            });
        }


        private async void ChoosePseudo()
        {
            try
            {
                var pseudo = choosePseudoInputField.text;
                if (pseudo.Length < 1)
                {
                    ViewManager.Show<ErrorView>();
                    var errorView = ViewManager.GetView<ErrorView>();
                    errorView.SetMessageEmptyPseudo();
                }
                else
                {
                    await LobbyManager.Instance.SetPlayerValue(KeysTypeEnum.KeyPlayerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, pseudo));
                    ViewManager.Show<JoinedLobbyView>();
                }
            }
            catch (LobbyServiceException e)
            {
                ViewManager.Show<MainView>(false);
                ViewManager.Show<ErrorView>();
                var errorView = ViewManager.GetView<ErrorView>();
                errorView.SetErrorLobbyServiceExceptionMessage(e);
            }
        }
        
    }
}
