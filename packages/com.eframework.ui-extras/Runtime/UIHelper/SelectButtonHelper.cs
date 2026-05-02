using System;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.UIHelper
{
    [RequireComponent(typeof(Button))]
    public class SelectButtonHelper : MonoBehaviour
    {
        [SerializeField] private Image m_selectImage;

        [SerializeField] private Button m_button;

        [SerializeField] private bool m_isSelected = false;
        public bool IsSelected => m_isSelected;

        public Action<bool> OnSelectChanged;
        void Awake()
        {
            if (m_button != null)
            {
                m_button.onClick.AddListener(() =>
                {
                    m_isSelected = !m_isSelected;
                    if (m_selectImage != null)
                    {
                        m_selectImage.enabled = m_isSelected;
                    }
                    OnSelectChanged?.Invoke(m_isSelected);
                });
            }
        }

        void Reset()
        {
            m_button = GetComponent<Button>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.ToLower().Contains("select") && child.GetComponent<Image>() != null)
                {
                    m_selectImage = child.GetComponent<Image>();
                    break;
                }
            }
        }
    }
}