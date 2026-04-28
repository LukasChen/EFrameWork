//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    ///     流程组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Procedure")]
    public sealed class ProcedureComponent : GameFrameworkComponent
    {

        [SerializeField]
        private string[] m_AvailableProcedureTypeNames;

        [SerializeField]
        private string m_EntranceProcedureTypeName;

        private ProcedureBase m_EntranceProcedure;
        private IProcedureManager m_ProcedureManager;

        private bool HasAvailableProcedures => m_AvailableProcedureTypeNames != null && m_AvailableProcedureTypeNames.Length > 0;

        /// <summary>
        ///     获取当前流程。
        /// </summary>
        public ProcedureBase CurrentProcedure
        {
            get
            {
                return m_ProcedureManager.CurrentProcedure;
            }
        }

        /// <summary>
        ///     获取当前流程持续时间。
        /// </summary>
        public float CurrentProcedureTime
        {
            get
            {
                return m_ProcedureManager.CurrentProcedureTime;
            }
        }

        /// <summary>
        ///     游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_ProcedureManager = GameFrameworkEntry.GetModule<IProcedureManager>();
            if (m_ProcedureManager == null)
            {
                Debug.LogError("Procedure manager is invalid.");
            }

        }

        private IEnumerator Start()
        {
            EnsureDefaultAvailableProcedureTypeNames();
            EnsureDefaultEntranceProcedureTypeName();

            ProcedureBase[] procedures = new ProcedureBase[m_AvailableProcedureTypeNames.Length];
            for (int i = 0; i < m_AvailableProcedureTypeNames.Length; i++)
            {
                Type procedureType = Utility.Assembly.GetType(m_AvailableProcedureTypeNames[i]);
                if (procedureType == null)
                {
                    Debug.LogFormat("Can not find procedure type '{0}'.", m_AvailableProcedureTypeNames[i]);
                    yield break;
                }

                procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);
                if (procedures[i] == null)
                {
                    Debug.LogFormat("Can not create procedure instance '{0}'.", m_AvailableProcedureTypeNames[i]);
                    yield break;
                }

                if (m_EntranceProcedureTypeName == m_AvailableProcedureTypeNames[i])
                {
                    m_EntranceProcedure = procedures[i];
                }
            }

            if (m_EntranceProcedure == null)
            {
                Debug.LogFormat("Entrance procedure is invalid.");
                yield break;
            }

            m_ProcedureManager.Initialize(GameFrameworkEntry.GetModule<IFsmManager>(), procedures);

            yield return new WaitForEndOfFrame();

            m_ProcedureManager.StartProcedure(m_EntranceProcedure.GetType());
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

        private void EnsureDefaultAvailableProcedureTypeNames()
        {
            if (HasAvailableProcedures)
            {
                return;
            }

            m_AvailableProcedureTypeNames = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ProcedureBase).IsAssignableFrom(type))
                .Select(type => type.FullName)
                .OrderBy(name => name)
                .ToArray();
        }

        private void EnsureDefaultEntranceProcedureTypeName()
        {
            if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName) || !HasAvailableProcedures)
            {
                return;
            }

            m_EntranceProcedureTypeName = m_AvailableProcedureTypeNames[0];
        }

        private static Type[] GetLoadableTypes(System.Reflection.Assembly assembly)
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



        /// <summary>
        ///     是否存在流程。
        /// </summary>
        /// <typeparam name="T">要检查的流程类型。</typeparam>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure<T>() where T : ProcedureBase
        {
            return m_ProcedureManager.HasProcedure<T>();
        }

        /// <summary>
        ///     是否存在流程。
        /// </summary>
        /// <param name="procedureType">要检查的流程类型。</param>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure(Type procedureType)
        {
            return m_ProcedureManager.HasProcedure(procedureType);
        }

        /// <summary>
        ///     获取流程。
        /// </summary>
        /// <typeparam name="T">要获取的流程类型。</typeparam>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure<T>() where T : ProcedureBase
        {
            return m_ProcedureManager.GetProcedure<T>();
        }

        /// <summary>
        ///     获取流程。
        /// </summary>
        /// <param name="procedureType">要获取的流程类型。</param>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure(Type procedureType)
        {
            return m_ProcedureManager.GetProcedure(procedureType);
        }
    }
}
