using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Graphics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class UISmoothFill : MonoBehaviour
    {
        [SerializeField] private Image m_targetImage;
        [SerializeField, Min(0f)] private float m_fillSpeed = 1f;

        private float m_currentFill;
        private float m_targetFill;
        private bool m_isUpdating;

        public Image TargetImage => m_targetImage;
        public float FillSpeed
        {
            get => m_fillSpeed;
            set => m_fillSpeed = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            EnsureTargetImage();
            SyncFromTarget();
        }

        private void OnEnable()
        {
            EnsureTargetImage();
            SyncFromTarget();
        }

        private void Reset()
        {
            m_targetImage = GetComponent<Image>();
        }

        private void OnValidate()
        {
            if (m_targetImage == null)
            {
                m_targetImage = GetComponent<Image>();
            }

            m_fillSpeed = Mathf.Max(0f, m_fillSpeed);
        }

        private void Update()
        {
            if (!m_isUpdating || m_targetImage == null)
            {
                return;
            }

            m_currentFill = Mathf.MoveTowards(
                m_currentFill,
                m_targetFill,
                m_fillSpeed * Time.deltaTime);

            m_targetImage.fillAmount = m_currentFill;

            if (Mathf.Abs(m_currentFill - m_targetFill) < 0.01f)
            {
                m_currentFill = m_targetFill;
                m_targetImage.fillAmount = m_currentFill;
                m_isUpdating = false;
            }
        }

        public void SetValue(float value)
        {
            SetValueWithoutAnimation(value);
        }

        public void SetValueWithoutAnimation(float value)
        {
            if (m_targetImage == null)
            {
                return;
            }

            m_currentFill = Mathf.Clamp01(value);
            m_targetFill = m_currentFill;
            m_targetImage.fillAmount = m_currentFill;
            m_isUpdating = false;
        }

        public void SetTargetFill(float newFill)
        {
            if (m_targetImage == null)
            {
                return;
            }

            newFill = Mathf.Clamp01(newFill);
            if (!m_isUpdating)
            {
                m_currentFill = m_targetImage.fillAmount;
            }

            if (Mathf.Approximately(m_targetFill, newFill))
            {
                return;
            }

            m_targetFill = newFill;
            m_isUpdating = m_fillSpeed > 0f;
            if (!m_isUpdating)
            {
                SetValueWithoutAnimation(newFill);
            }
        }

        private void EnsureTargetImage()
        {
            if (m_targetImage == null)
            {
                m_targetImage = GetComponent<Image>();
            }
        }

        private void SyncFromTarget()
        {
            if (m_targetImage == null)
            {
                return;
            }

            m_currentFill = m_targetImage.fillAmount;
            m_targetFill = m_currentFill;
            m_isUpdating = false;
        }
    }
}
