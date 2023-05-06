using Lobby.UI.Views;
using UnityEngine;
using UnityEngine.UI;
using Fromiel.LobbyPlugin;

namespace Lobby.UI.Lobby
{
    /// <summary>
    /// View to choose public rooms
    /// </summary>
    public sealed class JoinPublicRoomView : View
    {
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button backButton;
        [SerializeField] private VerticalLayoutGroup roomsLayout;
        [SerializeField] private Transform uiRoomPrefab;

        public override void Initialize()
        {
            refreshButton.onClick.AddListener(Refresh);
            backButton.onClick.AddListener(ViewManager.ShowLast);
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        private async void Refresh()
        {
            ClearChildren(roomsLayout.transform);

            var lobbies = await LobbyManager.Instance.ListLobbies();

            foreach (var lobby in lobbies)
            {
                var go = Instantiate(uiRoomPrefab, roomsLayout.transform);
                go.GetComponent<JoinableRoom>().Initialize(lobby);
            }
        }


        private void ClearChildren(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                Destroy(t.GetChild(i).gameObject);
            }
        }
    }
}
