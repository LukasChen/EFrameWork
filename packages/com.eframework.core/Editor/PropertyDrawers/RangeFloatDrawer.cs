#if UNITY_EDITOR


using EFramework.Runtime.Base.Attributes;
using UnityEditor;
using UnityEngine;

namespace EFramework.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(RangeFloat))]
    public class RangeFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Start property drawing
            EditorGUI.BeginProperty(position, label, property);

            // Get the min and max properties
            SerializedProperty minProperty = property.FindPropertyRelative("Min");
            SerializedProperty maxProperty = property.FindPropertyRelative("Max");

            // Get the MinMaxRange attribute
            MinMaxRangeAttribute range = (MinMaxRangeAttribute)fieldInfo.GetCustomAttributes(typeof(MinMaxRangeAttribute), false)[0];

            // Calculate rects
            float labelWidth = EditorGUIUtility.labelWidth;
            float sliderWidth = position.width - labelWidth - 60; // 60 for min and max labels
            Rect labelRect = new(position.x, position.y, labelWidth, position.height);
            Rect minLabelRect = new(position.x + labelWidth, position.y, 30, position.height);
            Rect sliderRect = new(position.x + labelWidth + 30, position.y, sliderWidth, position.height);
            Rect maxLabelRect = new(position.x + labelWidth + 30 + sliderWidth, position.y, 30, position.height);

            // Draw label
            EditorGUI.LabelField(labelRect, label);

            // Draw min and max value labels
            float minValue = minProperty.floatValue;
            float maxValue = maxProperty.floatValue;
            EditorGUI.LabelField(minLabelRect, minValue.ToString("F2"));
            EditorGUI.LabelField(maxLabelRect, maxValue.ToString("F2"));

            // Draw slider
            EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, range.Min, range.Max);

            // Update the properties with the new values
            minProperty.floatValue = minValue;
            maxProperty.floatValue = maxValue;

            // End property drawing
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
