using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime.UI.Interactions
{
    public interface IUIDismissDispatcher
    {
        UIDismissScope Register(IEnumerable<RectTransform> insideAreas, Action<UIDismissTrigger> onDismiss, bool ignoreCurrentFrame = true);
        UIDismissScope Register(Action<UIDismissTrigger> onDismiss, bool ignoreCurrentFrame = true, params RectTransform[] insideAreas);
        void Unregister(UIDismissScope scope);
        void Clear();
    }
}
