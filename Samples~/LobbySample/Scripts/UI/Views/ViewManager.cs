using System.Collections.Generic;
using UnityEngine;

namespace Lobby.UI.Views
{
    /// <summary>
    /// Singleton class to manage the different views of the ui
    /// </summary>
    public sealed class ViewManager : MonoBehaviour
    {
        #region Attributes

        private static ViewManager _instance;

        [SerializeField] private View startingView; //Starting view

        [SerializeField] private View[] views; //List of all the views

        private View _currentView;

        private readonly Stack<View> _history = new(); //Historic of the views (to be able to go back (on the main menu for example))
        
        #endregion

        #region Public static methods

        /// <summary>
        /// Return the view of type T
        /// </summary>
        /// <typeparam name="T">Type of the view</typeparam>
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
        /// Show the view of type T
        /// </summary>
        /// <param name="remember">Boolean to know if we keep in memory the old view (true to keep in memory and false otherwise)</param>
        /// <typeparam name="T">Type of the view</typeparam>
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
        /// Show the view passed in parameter
        /// </summary>
        /// <param name="view">View to show</param>
        /// <param name="remember">Boolean to know if we keep in memory the old view (true to keep in memory and false otherwise)</param>
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
        /// Method to show the last view in memory
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
            //Initialize all the views
            for (int i = 0; i < views.Length; i++)
            {
                views[i].Initialize();

                views[i].Hide();
            }

            //Show the starting view
            if (startingView != null)
            {
                Show(startingView, true);
            }
        }
        
        #endregion
    }
}
