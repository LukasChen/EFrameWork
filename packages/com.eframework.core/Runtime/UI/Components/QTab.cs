using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EFrame.Runtime.UI.Components
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class QTab : MonoBehaviour
    {
        private Button[] m_buttons;
        private List<Image> m_selectedImages;
        [SerializeField] private int m_defaultIndex = 0;
        [SerializeField] public UnityEvent<int> OnTabSelected;
        [SerializeField] private Color m_normalColor = Color.white;
        [SerializeField] private Color m_selectedColor = Color.gray;
        private void Awake()
        {
            m_buttons = GetComponentsInChildren<Button>();
            m_selectedImages = new();

            foreach (var button in m_buttons)
            {
                if (button == null) continue;
                m_selectedImages.Add(button.GetComponent<Image>());
                button.onClick.AddListener(() => OnTabButtonClick(System.Array.IndexOf(m_buttons, button)));
            }

            SelectTab(m_defaultIndex);
        }

        private void OnTabButtonClick(int index)
        {
            if (index < 0 || index >= m_buttons.Length)
            {
                return;
            }

            SelectTab(index);
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= m_buttons.Length)
            {
                Debug.LogError("Index out of range");
                return;
            }

            for (int i = 0; i < m_selectedImages.Count; i++)
            {
                var image = m_selectedImages[i];
                image.color = index == i ? m_selectedColor : m_normalColor;
            }
            OnTabSelected?.Invoke(index);
        }
    }
}