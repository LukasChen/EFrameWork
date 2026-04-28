using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFrameWork.Runtime.Utils
{
   public enum GameLayer
    {
        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,
        Water = 4,
        UI = 5,
        Preview = 6,
        Block = 7,
        BehindMask = 8,
        Mask = 9

    }
}
