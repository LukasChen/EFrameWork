using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EFrame.Runtime.Procedure
{
    [DisallowMultipleComponent]
    [AddComponentMenu("EFrame/Procedure")]
    public sealed class EFrameProcedureComponent : MonoBehaviour
    {
        [SerializeField] private string[] m_availableProcedureTypeNames;
        [SerializeField] private string m_entranceProcedureTypeName;

        private EFrameProcedureManager m_procedureManager;
        private bool m_started;

        private bool HasAvailableProcedures => m_availableProcedureTypeNames != null && m_availableProcedureTypeNames.Length > 0;

        public EFrameProcedure CurrentProcedure => m_procedureManager?.CurrentProcedure;
        public float CurrentProcedureTime => m_procedureManager?.CurrentProcedureTime ?? 0f;
        public bool IsInitialized => m_procedureManager != null;
        public bool IsRunning => m_started && CurrentProcedure != null;

        public void Initialize(EFrameContext context)
        {
            if (context == null)
            {
                Debug.LogError("EFrameProcedureComponent requires a valid EFrameContext.");
                return;
            }

            Shutdown();
            EnsureDefaultAvailableProcedureTypeNames();
            EnsureDefaultEntranceProcedureTypeName();

            if (!HasAvailableProcedures)
            {
                Debug.LogError("No EFrame procedures are registered.");
                return;
            }

            var procedures = new List<EFrameProcedure>(m_availableProcedureTypeNames.Length);
            foreach (var procedureTypeName in m_availableProcedureTypeNames)
            {
                var procedureType = GetTypeByName(procedureTypeName);
                if (procedureType == null)
                {
                    Debug.LogError($"Can not find procedure type '{procedureTypeName}'.");
                    return;
                }

                if (!typeof(EFrameProcedure).IsAssignableFrom(procedureType))
                {
                    Debug.LogError($"Procedure type '{procedureTypeName}' does not inherit EFrameProcedure.");
                    return;
                }

                if (Activator.CreateInstance(procedureType) is not EFrameProcedure procedure)
                {
                    Debug.LogError($"Can not create procedure instance '{procedureTypeName}'.");
                    return;
                }

                procedures.Add(procedure);
            }

            m_procedureManager = new EFrameProcedureManager(context, procedures);
        }

        public void StartProcedure()
        {
            if (m_procedureManager == null)
            {
                Debug.LogError("EFrameProcedureComponent must be initialized before starting.");
                return;
            }

            if (m_started)
            {
                Debug.LogWarning("EFrameProcedureComponent is already running.");
                return;
            }

            var entranceProcedureType = GetTypeByName(m_entranceProcedureTypeName);
            if (entranceProcedureType == null)
            {
                Debug.LogError($"Entrance procedure is invalid: {m_entranceProcedureTypeName}");
                return;
            }

            m_procedureManager.StartProcedure(entranceProcedureType);
            m_started = true;
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (!m_started)
            {
                return;
            }

            m_procedureManager?.Update(deltaTime, unscaledDeltaTime);
        }

        public void Shutdown()
        {
            m_started = false;
            m_procedureManager?.Shutdown();
            m_procedureManager = null;
        }

        public bool HasProcedure<TProcedure>() where TProcedure : EFrameProcedure
        {
            return m_procedureManager != null && m_procedureManager.HasProcedure<TProcedure>();
        }

        public bool HasProcedure(Type procedureType)
        {
            return m_procedureManager != null && m_procedureManager.HasProcedure(procedureType);
        }

        public TProcedure GetProcedure<TProcedure>() where TProcedure : EFrameProcedure
        {
            return m_procedureManager?.GetProcedure<TProcedure>();
        }

        public EFrameProcedure GetProcedure(Type procedureType)
        {
            return m_procedureManager?.GetProcedure(procedureType);
        }

        private void Reset()
        {
            EnsureDefaultAvailableProcedureTypeNames();
            EnsureDefaultEntranceProcedureTypeName();
        }

        private void OnValidate()
        {
            EnsureDefaultAvailableProcedureTypeNames();
            EnsureDefaultEntranceProcedureTypeName();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void EnsureDefaultAvailableProcedureTypeNames()
        {
            if (HasAvailableProcedures)
            {
                return;
            }

            m_availableProcedureTypeNames = GetProcedureTypeNames();
        }

        private void EnsureDefaultEntranceProcedureTypeName()
        {
            if (!string.IsNullOrEmpty(m_entranceProcedureTypeName) || !HasAvailableProcedures)
            {
                return;
            }

            m_entranceProcedureTypeName = m_availableProcedureTypeNames[0];
        }

        public static string[] GetProcedureTypeNames()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type => type.IsClass && !type.IsAbstract && typeof(EFrameProcedure).IsAssignableFrom(type))
                .Select(type => type.FullName)
                .Where(typeName => !string.IsNullOrEmpty(typeName))
                .OrderBy(typeName => typeName)
                .ToArray();
        }

        private static Type GetTypeByName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }
    }
}
