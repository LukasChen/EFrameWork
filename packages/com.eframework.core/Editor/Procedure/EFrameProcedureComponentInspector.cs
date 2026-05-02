using System.Collections.Generic;
using System.Linq;
using EFrame.Runtime.Procedure;
using UnityEditor;
using UnityEngine;

namespace EFrame.Editor.Procedure
{
    [CustomEditor(typeof(EFrameProcedureComponent))]
    internal sealed class EFrameProcedureComponentInspector : UnityEditor.Editor
    {
        private SerializedProperty m_availableProcedureTypeNames;
        private SerializedProperty m_entranceProcedureTypeName;
        private List<string> m_currentAvailableProcedureTypeNames;
        private string[] m_procedureTypeNames;
        private int m_entranceProcedureIndex = -1;

        private void OnEnable()
        {
            m_availableProcedureTypeNames = serializedObject.FindProperty("m_availableProcedureTypeNames");
            m_entranceProcedureTypeName = serializedObject.FindProperty("m_entranceProcedureTypeName");
            RefreshTypeNames();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var component = (EFrameProcedureComponent)target;
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Current Procedure", component.CurrentProcedure == null ? "None" : component.CurrentProcedure.GetType().FullName);
                EditorGUILayout.LabelField("Current Procedure Time", component.CurrentProcedureTime.ToString("F2"));
                EditorGUILayout.Space();
            }

            if (string.IsNullOrEmpty(m_entranceProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("Entrance procedure is invalid.", MessageType.Error);
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                GUILayout.Label("Available Procedures", EditorStyles.boldLabel);
                if (m_procedureTypeNames.Length > 0)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        foreach (var procedureTypeName in m_procedureTypeNames)
                        {
                            var selected = m_currentAvailableProcedureTypeNames.Contains(procedureTypeName);
                            var nextSelected = EditorGUILayout.ToggleLeft(procedureTypeName, selected);
                            if (selected == nextSelected)
                            {
                                continue;
                            }

                            if (nextSelected)
                            {
                                m_currentAvailableProcedureTypeNames.Add(procedureTypeName);
                            }
                            else if (procedureTypeName != m_entranceProcedureTypeName.stringValue)
                            {
                                m_currentAvailableProcedureTypeNames.Remove(procedureTypeName);
                            }

                            WriteAvailableProcedureTypeNames();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("There is no available EFrame procedure.", MessageType.Warning);
                }

                if (m_currentAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Space();
                    var selectedIndex = EditorGUILayout.Popup("Entrance Procedure", m_entranceProcedureIndex, m_currentAvailableProcedureTypeNames.ToArray());
                    if (selectedIndex != m_entranceProcedureIndex)
                    {
                        m_entranceProcedureIndex = selectedIndex;
                        m_entranceProcedureTypeName.stringValue = m_currentAvailableProcedureTypeNames[selectedIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select available procedures first.", MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshTypeNames()
        {
            m_procedureTypeNames = EFrameProcedureComponent.GetProcedureTypeNames();
            ReadAvailableProcedureTypeNames();

            if (m_currentAvailableProcedureTypeNames.Count == 0 && m_procedureTypeNames.Length > 0)
            {
                m_currentAvailableProcedureTypeNames = m_procedureTypeNames.ToList();
            }

            var oldCount = m_currentAvailableProcedureTypeNames.Count;
            m_currentAvailableProcedureTypeNames = m_currentAvailableProcedureTypeNames.Where(typeName => m_procedureTypeNames.Contains(typeName)).ToList();
            if (m_currentAvailableProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(m_entranceProcedureTypeName.stringValue))
            {
                m_entranceProcedureIndex = m_currentAvailableProcedureTypeNames.IndexOf(m_entranceProcedureTypeName.stringValue);
                if (m_entranceProcedureIndex < 0)
                {
                    m_entranceProcedureTypeName.stringValue = string.Empty;
                }
            }

            EnsureDefaultEntranceProcedureTypeName();
            serializedObject.ApplyModifiedProperties();
        }

        private void ReadAvailableProcedureTypeNames()
        {
            m_currentAvailableProcedureTypeNames = new List<string>();
            for (var index = 0; index < m_availableProcedureTypeNames.arraySize; index++)
            {
                m_currentAvailableProcedureTypeNames.Add(m_availableProcedureTypeNames.GetArrayElementAtIndex(index).stringValue);
            }
        }

        private void WriteAvailableProcedureTypeNames()
        {
            m_availableProcedureTypeNames.ClearArray();
            m_currentAvailableProcedureTypeNames.Sort();

            for (var index = 0; index < m_currentAvailableProcedureTypeNames.Count; index++)
            {
                m_availableProcedureTypeNames.InsertArrayElementAtIndex(index);
                m_availableProcedureTypeNames.GetArrayElementAtIndex(index).stringValue = m_currentAvailableProcedureTypeNames[index];
            }

            EnsureDefaultEntranceProcedureTypeName();
        }

        private void EnsureDefaultEntranceProcedureTypeName()
        {
            if (m_currentAvailableProcedureTypeNames == null || m_currentAvailableProcedureTypeNames.Count == 0)
            {
                m_entranceProcedureIndex = -1;
                m_entranceProcedureTypeName.stringValue = string.Empty;
                return;
            }

            if (!string.IsNullOrEmpty(m_entranceProcedureTypeName.stringValue))
            {
                m_entranceProcedureIndex = m_currentAvailableProcedureTypeNames.IndexOf(m_entranceProcedureTypeName.stringValue);
                if (m_entranceProcedureIndex >= 0)
                {
                    return;
                }
            }

            m_entranceProcedureIndex = 0;
            m_entranceProcedureTypeName.stringValue = m_currentAvailableProcedureTypeNames[0];
        }
    }
}
