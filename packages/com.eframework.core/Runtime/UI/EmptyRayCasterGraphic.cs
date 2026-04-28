using UnityEngine;
using UnityEngine.UI;

namespace EFrameWork.Runtime.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EmptyRayCasterGraphic : Graphic
    {
        protected EmptyRayCasterGraphic()
        {
            useLegacyMeshGeneration = false;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
