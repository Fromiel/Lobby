using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fromiel.LobbyPlugin.Editor
{
    public class ModifyKeysEnumWindow : EditorWindow
    {
        private readonly List<string> _keysEnums = new ();
    
        // Add menu item named "LobbyPlugin/ModifyKeysEnum"
        [MenuItem("LobbyPlugin/ModifyKeysEnum")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            var w = (ModifyKeysEnumWindow) GetWindow(typeof(ModifyKeysEnumWindow));
            foreach (var key in (KeysTypeEnum[])Enum.GetValues(typeof(KeysTypeEnum)))
            {
                w._keysEnums.Add(key.ToString());
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Add key"))
                AddKey();
            if(GUILayout.Button("Remove last key"))
                RemoveLastKey();
            EditorGUILayout.EndHorizontal();
            for(var i = 0; i < _keysEnums.Count; i++)
            {
                _keysEnums[i] = EditorGUILayout.TextField("Key : ", _keysEnums[i]);
            }

            if (GUILayout.Button("Set keys enum"))
            {
                ChangeEnum();
                Close();
            }

        }

        private void AddKey()
        {
            _keysEnums.Add("");
        }

        private void RemoveLastKey()
        {
            _keysEnums.RemoveAt(_keysEnums.Count - 1);
        }

        private void ChangeEnum()
        {
            string enumName = "KeysTypeEnum";
            string filePathAndName = "Packages/LobbyPlugin/Runtime/" + enumName + ".cs";
        
            using ( StreamWriter streamWriter = new StreamWriter( filePathAndName ) )
            {
                streamWriter.WriteLine( "public enum " + enumName );
                streamWriter.WriteLine( "{" );
                foreach (var key in _keysEnums)
                {
                    streamWriter.WriteLine( "\t" + key + "," );
                }
                streamWriter.WriteLine( "}" );
            }
            AssetDatabase.Refresh();
        
        }
    }
}
