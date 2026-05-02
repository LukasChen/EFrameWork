using System;
using Random = UnityEngine.Random;

namespace EFramework.Runtime.Base.Attributes
{
    [Serializable]
    public struct RangeFloat
    {

        public float Min;
        public float Max;

        public RangeFloat(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float RandomValue
        {
            get => Random.Range(Min, Max);
        }
    }
}
