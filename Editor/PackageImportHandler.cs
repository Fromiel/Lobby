using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fromiel.LobbyPlugin.Editor
{
    public sealed class PackageImportHandler : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            ChangeEnum();
        }

        private static void ChangeEnum()
        {
            string enumFolder = "Assets/LobbyPlugin";
            string enumName = "KeysTypeEnum";
            string filePathAndName = "Packages/LobbyPlugin/Runtime/" + enumName + ".cs";

            if (!Directory.Exists("Packages/LobbyPlugin/Runtime/"))
            {
                if (!Directory.Exists(enumFolder))
                {
                    Directory.CreateDirectory(enumFolder);
                    if (File.Exists(filePathAndName))
                    {
                        File.Delete(filePathAndName);
                    }
                }
                filePathAndName = enumFolder + enumName + ".cs";
            }

            using ( StreamWriter streamWriter = new StreamWriter( filePathAndName ) )
            {
                streamWriter.WriteLine( "namespace Fromiel.LobbyPlugin");
                streamWriter.WriteLine( "{" );
                streamWriter.WriteLine( "public enum " + enumName );
                streamWriter.WriteLine( "{" );
                streamWriter.WriteLine( "\t" + "KeyStartGame" + "," );
                foreach (var key in (KeysTypeEnum[])Enum.GetValues(typeof(KeysTypeEnum)))
                {
                    if(key == KeysTypeEnum.KeyStartGame)
                        continue;
                    streamWriter.WriteLine( "\t" + key + "," );
                }
                streamWriter.WriteLine( "}" );
                streamWriter.WriteLine( "}" );
            }
            AssetDatabase.Refresh();
        
        }
    }
}
