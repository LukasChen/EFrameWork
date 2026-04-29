using DG.Tweening;
using UnityEngine;

namespace EFrameWork.Runtime.UI.UIAnimation
{
    public class UIEnableAnimation : MonoBehaviour
    {
        [SerializeField] private float m_openDuration = 0.15f;
        [SerializeField] private float m_easeOvershoot = 1.4f;
        [SerializeField] private float m_startScale = 0.85f;
        void OnEnable()
        {
            if (!this.gameObject.activeSelf) this.gameObject.SetActive(true);
            this.transform.DOKill(true);
            this.transform.localScale = Vector3.one * m_startScale; // 起始稍小
            this.transform.DOScale(1f, m_openDuration)         // 放大到 1
                .SetEase(Ease.OutBack, m_easeOvershoot)                 // 稍微弹一下
                .SetUpdate(true);                            // 可选：不受 Time.timeScale 影响
        }
        private void OnDestroy()
        {
            this.transform.DOKill();
        }
    }
}
