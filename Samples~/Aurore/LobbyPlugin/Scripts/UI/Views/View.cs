using UnityEngine;

namespace Aurore.LobbyPlugin.Scripts.UI.Views
{
    /// <summary>
    /// Classe représentant une vue dans le jeu
    /// </summary>
    public abstract class View : MonoBehaviour
    {
        /// <summary>
        /// Methode pour initialiser la vue
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Methode pour cacher la vue
        /// </summary>
        public virtual void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Methode pour afficher la vue
        /// </summary>
        public virtual void Show() => gameObject.SetActive(true);
        
    }
}
