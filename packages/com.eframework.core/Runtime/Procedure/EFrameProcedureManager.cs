using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EFrameWork.Runtime.Asset;
using UnityEngine;

namespace EFrameWork.Runtime.Procedure
{
    public sealed class EFrameProcedureManager
    {
        private readonly Dictionary<Type, EFrameProcedure> m_procedures = new();
        private readonly Queue<ProcedureTransition> m_pendingTransitions = new();
        private readonly CancellationTokenSource m_shutdownCancellationTokenSource = new();
        private bool m_isTransitioning;
        private EFrameProcedure m_currentProcedure;
        private EFrameProcedure m_enteringProcedure;
        private IAssetPreloadScope m_currentPreloadScope;

        public EFrameProcedureManager(EFrameContext context, IEnumerable<EFrameProcedure> procedures)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));

            foreach (var procedure in procedures)
            {
                if (procedure == null)
                {
                    continue;
                }

                var procedureType = procedure.GetType();
                if (m_procedures.ContainsKey(procedureType))
                {
                    throw new InvalidOperationException($"Duplicate procedure type: {procedureType.FullName}");
                }

                procedure.Bind(context, this);
                m_procedures.Add(procedureType, procedure);
            }

            foreach (var procedure in m_procedures.Values)
            {
                procedure.OnInit();
            }
        }

        public EFrameContext Context { get; }
        public EFrameProcedure CurrentProcedure => m_currentProcedure;
        public float CurrentProcedureTime => m_currentProcedure?.StateTime ?? 0f;

        public void StartProcedure(Type procedureType)
        {
            if (m_currentProcedure != null)
            {
                throw new InvalidOperationException("Procedure manager is already running.");
            }

            RequestChangeState(procedureType, null);
        }

        public void ChangeState<TProcedure>() where TProcedure : EFrameProcedure
        {
            ChangeState<TProcedure>(null);
        }

        public void ChangeState<TProcedure>(object payload) where TProcedure : EFrameProcedure
        {
            RequestChangeState(typeof(TProcedure), payload);
        }

        public bool HasProcedure<TProcedure>() where TProcedure : EFrameProcedure
        {
            return m_procedures.ContainsKey(typeof(TProcedure));
        }

        public bool HasProcedure(Type procedureType)
        {
            return procedureType != null && m_procedures.ContainsKey(procedureType);
        }

        public TProcedure GetProcedure<TProcedure>() where TProcedure : EFrameProcedure
        {
            return m_procedures.TryGetValue(typeof(TProcedure), out var procedure) ? (TProcedure)procedure : null;
        }

        public EFrameProcedure GetProcedure(Type procedureType)
        {
            if (procedureType == null)
            {
                return null;
            }

            return m_procedures.TryGetValue(procedureType, out var procedure) ? procedure : null;
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (m_currentProcedure == null)
            {
                ProcessPendingTransitions();
                return;
            }

            m_currentProcedure.StateTime += deltaTime;
            m_currentProcedure.OnUpdate(deltaTime, unscaledDeltaTime);
            ProcessPendingTransitions();
        }

        public void Shutdown()
        {
            m_pendingTransitions.Clear();
            m_shutdownCancellationTokenSource.Cancel();

            if (m_enteringProcedure != null)
            {
                m_enteringProcedure.BeginLeave();
                m_enteringProcedure.EndLeave();
                m_enteringProcedure = null;
            }

            if (m_currentProcedure != null)
            {
                m_currentProcedure.BeginLeave();
                m_currentProcedure.OnLeave(true);
                m_currentProcedure.EndLeave();
                m_currentProcedure = null;
            }

            ReleaseCurrentPreloadScope();

            foreach (var procedure in m_procedures.Values)
            {
                procedure.OnDestroy();
            }

            m_procedures.Clear();
        }

        private void RequestChangeState(Type procedureType, object payload)
        {
            if (procedureType == null)
            {
                throw new ArgumentNullException(nameof(procedureType));
            }

            if (!typeof(EFrameProcedure).IsAssignableFrom(procedureType))
            {
                throw new ArgumentException($"Procedure type is invalid: {procedureType.FullName}", nameof(procedureType));
            }

            if (!m_procedures.ContainsKey(procedureType))
            {
                throw new InvalidOperationException($"Procedure is not registered: {procedureType.FullName}");
            }

            m_pendingTransitions.Enqueue(new ProcedureTransition(procedureType, payload));
            ProcessPendingTransitions();
        }

        private void ProcessPendingTransitions()
        {
            if (m_pendingTransitions.Count == 0)
            {
                return;
            }

            if (m_isTransitioning)
            {
                return;
            }

            m_isTransitioning = true;
            ProcessPendingTransitionsAsync(m_shutdownCancellationTokenSource.Token).Forget();
        }

        private async UniTaskVoid ProcessPendingTransitionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (m_pendingTransitions.Count > 0)
                {
                    var transition = m_pendingTransitions.Dequeue();
                    await ExecuteTransitionAsync(transition, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                m_isTransitioning = false;

                if (!cancellationToken.IsCancellationRequested && m_pendingTransitions.Count > 0)
                {
                    ProcessPendingTransitions();
                }
            }
        }

        private async UniTask ExecuteTransitionAsync(ProcedureTransition transition, CancellationToken cancellationToken)
        {
            var previousProcedure = m_currentProcedure;
            var previousProcedureType = previousProcedure?.GetType();

            if (previousProcedure != null)
            {
                previousProcedure.BeginLeave();
                previousProcedure.OnLeave(false);
                previousProcedure.EndLeave();
                ReleaseCurrentPreloadScope();
                m_currentProcedure = null;
            }

            if (!m_procedures.TryGetValue(transition.ProcedureType, out var nextProcedure))
            {
                Debug.LogError($"Procedure is not registered: {transition.ProcedureType.FullName}");
                m_currentProcedure = null;
                return;
            }

            var enterContext = new ProcedureEnterContext(previousProcedureType, transition.Payload);
            var preloadScope = Context.Assets.CreatePreloadScope();
            m_enteringProcedure = nextProcedure;
            nextProcedure.BeginEnter();

            try
            {
                await nextProcedure.OnPreloadAsync(preloadScope, enterContext);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                preloadScope.Dispose();
                nextProcedure.BeginLeave();
                nextProcedure.EndLeave();
                return;
            }
            finally
            {
                m_enteringProcedure = null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                preloadScope.Dispose();
                nextProcedure.BeginLeave();
                nextProcedure.EndLeave();
                return;
            }

            m_currentPreloadScope = preloadScope;
            m_currentProcedure = nextProcedure;
            nextProcedure.OnEnter(enterContext);
        }

        private void ReleaseCurrentPreloadScope()
        {
            m_currentPreloadScope?.Dispose();
            m_currentPreloadScope = null;
        }

        private readonly struct ProcedureTransition
        {
            public ProcedureTransition(Type procedureType, object payload)
            {
                ProcedureType = procedureType;
                Payload = payload;
            }

            public Type ProcedureType { get; }
            public object Payload { get; }
        }
    }
}
