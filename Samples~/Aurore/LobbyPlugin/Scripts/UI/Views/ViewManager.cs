using System.Collections.Generic;
using UnityEngine;

namespace Aurore.LobbyPlugin.Scripts.UI.Views
{
    /// <summary>
    /// Classe (singleton) permettant de gerer les differentes vues de l'ui
    /// </summary>
    public sealed class ViewManager : MonoBehaviour
    {
        #region Attributes

        private static ViewManager _instance; //ViewManager est un singleton

        [SerializeField] private View startingView; //Vue du debut de la scene

        [SerializeField] private View[] views; //Differentes vues

        private View _currentView; //Vue actuelle

        private readonly Stack<View> _history = new(); //Historique des vues (pour pouvoir faire des retour en arriere (sur le menu principal par exemple))
        
        #endregion

        #region Public static methods

        /// <summary>
        /// Retourne la vue du type T
        /// </summary>
        /// <typeparam name="T">Type de la vue</typeparam>
        /// <returns></returns>
        public static T GetView<T>() where T : View
        {
            for (int i = 0; i < _instance.views.Length; i++)
            {
                if (_instance.views[i] is T tView)
                {
                    return tView;
                }
            }

            return null;
        }

        /// <summary>
        /// Affiche la vue de type T 
        /// </summary>
        /// <param name="remember">Booleen pour savoir si on garde en memoire l'ancienne vue (true pour garder en memoire et false sinon)</param>
        /// <typeparam name="T">Type de la vue</typeparam>
        public static void Show<T>(bool remember = true) where T : View
        {
            for (int i = 0; i < _instance.views.Length; i++)
            {
                if (_instance.views[i] is not T) continue;
                
                if (_instance._currentView != null)
                {
                    if (remember)
                    {
                        _instance._history.Push(_instance._currentView);
                    }

                    _instance._currentView.Hide();
                }

                _instance.views[i].Show();

                _instance._currentView = _instance.views[i];
            }
        }

        /// <summary>
        /// Affiche la vue passée en argument
        /// </summary>
        /// <param name="view">Vue à afficher</param>
        /// <param name="remember">Booleen pour savoir si on garde en memoire l'ancienne vue (true pour garder en memoire et false sinon)</param>
        public static void Show(View view, bool remember = true)
        {
            if (_instance._currentView != null)
            {
                if (remember)
                {
                    _instance._history.Push(_instance._currentView);
                }

                _instance._currentView.Hide();
            }

            view.Show();

            _instance._currentView = view;
        }

        /// <summary>
        /// Methode pour afficher la derniere vue en memoire
        /// </summary>
        public static void ShowLast()
        {
            if (_instance._history.Count != 0)
            {
                Show(_instance._history.Pop(), false);
            }
        }
        
        #endregion


        #region Monobehaviour callbacks
        
        private void Awake() => _instance = this;

        private void Start()
        {
            //On initialise toutes les vues
            for (int i = 0; i < views.Length; i++)
            {
                views[i].Initialize();

                views[i].Hide();
            }

            //On affiche la vue de départ
            if (startingView != null)
            {
                Show(startingView, true);
            }
        }
        
        #endregion
    }
}
