using UnityEngine;

namespace EFramework.Runtime
{
    public abstract class EFrameBehaviour : MonoBehaviour, IEFrameContextAware
    {
        protected EFrameContext Context { get; private set; }

        public void BindContext(EFrameContext context)
        {
            Context = context;
            OnContextBound(context);
        }

        protected virtual void OnContextBound(EFrameContext context)
        {
        }
    }
}
