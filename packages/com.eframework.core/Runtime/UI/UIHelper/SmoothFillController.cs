using UnityEngine;
using UnityEngine.UI;

namespace EFrame.Runtime.UI.UIHelper
{
    public class SmoothFillController : MonoBehaviour
    {
        [SerializeField] private Image m_targetImage;
        [SerializeField] public float FillSpeed = 1f;
    
        private float m_currentFill;
        private float m_targetFill;
        private bool m_isUpdating;

        public void SetStartValue(float value)
        {
            m_currentFill = value;
            m_targetImage.fillAmount = m_currentFill;
        }
        void Update()
        {
            if (!m_isUpdating) return;
        
            // 使用Mathf.MoveTowards实现匀速变化
            m_currentFill = Mathf.MoveTowards(
                m_currentFill, 
                m_targetFill, 
                FillSpeed * Time.deltaTime);
        
            m_targetImage.fillAmount = m_currentFill;
        
            // 到达目标值但保留0.01缓冲区间防止抖动
            if (Mathf.Abs(m_currentFill - m_targetFill) < 0.01f)
            {
                m_currentFill = m_targetFill;
                m_isUpdating = false;
            }
        }

        // 外部调用接口
        public void SetTargetFill(float newFill)
        {
            newFill = Mathf.Clamp01(newFill);
            if (Mathf.Approximately(m_targetFill, newFill)) return;
        
            m_targetFill = newFill;
            m_isUpdating = true;

        }
    }
}
