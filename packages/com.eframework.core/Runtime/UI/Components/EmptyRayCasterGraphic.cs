using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 空的 Graphic 组件，用于占位和接收射线事件，避免添加不必要的 Image 组件带来的性能开销。
/// </summary>
namespace EFrameWork.Runtime.UI.Components
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
