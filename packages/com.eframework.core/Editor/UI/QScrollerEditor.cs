#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EFrame.Editor.UI
{
    [CustomEditor(typeof(EFrame.Runtime.UI.Components.QScroller))]
    public class QScrollerEditor : UnityEditor.UI.ScrollRectEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // 先绘制基类Inspector

            // 绘制自定义字段
            SerializedProperty hUnits = serializedObject.FindProperty("m_snapHorizontalUnits");
            SerializedProperty vUnits = serializedObject.FindProperty("m_snapVerticalUnits");
            EditorGUILayout.PropertyField(hUnits);
            EditorGUILayout.PropertyField(vUnits);

            // 绘制 UnityEvent
            SerializedProperty onSnapToHorizontal = serializedObject.FindProperty("OnSnapToHorizontalSegment");
            SerializedProperty onSnapToVertical = serializedObject.FindProperty("OnSnapToVerticalSegment");
            EditorGUILayout.PropertyField(onSnapToHorizontal);
            EditorGUILayout.PropertyField(onSnapToVertical);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif