using UnityEngine;

namespace Lobby.UI.Views
{
    /// <summary>
    /// Abstract class to represent a view in the game
    /// </summary>
    public abstract class View : MonoBehaviour
    {
        /// <summary>
        /// Initialize the view
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Hide the view
        /// </summary>
        public virtual void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Show the view
        /// </summary>
        public virtual void Show() => gameObject.SetActive(true);
        
    }
}
