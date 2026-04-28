using EFrameWork.Runtime.Utils;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;
using System.IO;
using UnityEngine;

namespace EFrameWork.Editor.TextureTools
{
    public static class CommonTools
    {

        [MenuItem("EFrame Tools/设置游戏图层")]
        public static void SetupLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            
            // 设置每个图层名称
            foreach (GameLayer layer in System.Enum.GetValues(typeof(GameLayer)))
            {
                SerializedProperty layerProperty = layers.GetArrayElementAtIndex((int)layer);
                if (layerProperty != null)
                {
                    layerProperty.stringValue = layer.ToString();
                }
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("游戏图层配置完成！");
        }
    }
}
