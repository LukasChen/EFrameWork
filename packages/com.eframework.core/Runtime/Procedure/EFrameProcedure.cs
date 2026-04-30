using System.Threading;
using Cysharp.Threading.Tasks;
using EFrameWork.Runtime.Asset;

namespace EFrameWork.Runtime.Procedure
{
    public abstract class EFrameProcedure
    {
        private CancellationTokenSource m_leaveCancellationTokenSource;

        public EFrameContext Context { get; private set; }
        public EFrameProcedureManager Manager { get; private set; }
        public float StateTime { get; internal set; }
        protected CancellationToken LeaveCancellationToken => m_leaveCancellationTokenSource?.Token ?? CancellationToken.None;

        internal void Bind(EFrameContext context, EFrameProcedureManager manager)
        {
            Context = context;
            Manager = manager;
        }

        internal void BeginEnter()
        {
            StateTime = 0f;
            m_leaveCancellationTokenSource?.Dispose();
            m_leaveCancellationTokenSource = new CancellationTokenSource();
        }

        internal void BeginLeave()
        {
            if (m_leaveCancellationTokenSource == null)
            {
                return;
            }

            if (!m_leaveCancellationTokenSource.IsCancellationRequested)
            {
                m_leaveCancellationTokenSource.Cancel();
            }
        }

        internal void EndLeave()
        {
            m_leaveCancellationTokenSource?.Dispose();
            m_leaveCancellationTokenSource = null;
        }

        protected internal virtual void OnInit()
        {
        }

        protected internal virtual UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
        {
            return UniTask.CompletedTask;
        }

        protected internal virtual void OnEnter(ProcedureEnterContext context)
        {
        }

        protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
        }

        protected internal virtual void OnLeave(bool isShutdown)
        {
        }

        protected internal virtual void OnDestroy()
        {
        }

        protected void ChangeState<TProcedure>() where TProcedure : EFrameProcedure
        {
            Manager.ChangeState<TProcedure>();
        }

        protected void ChangeState<TProcedure>(object payload) where TProcedure : EFrameProcedure
        {
            Manager.ChangeState<TProcedure>(payload);
        }
    }
}
