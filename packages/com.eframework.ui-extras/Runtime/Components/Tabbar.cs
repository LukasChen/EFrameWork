using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EFramework.Extensions.UI.Extras.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class Tabbar : MonoBehaviour
    {
        [Serializable]
        public class TabChangedEvent : UnityEvent<int, int>
        {
        }

        [Tooltip("Explicit StateButton items. Leave empty to auto collect child StateButton components.")]
        [SerializeField] private List<StateButton> m_tabs = new();
        [SerializeField] private bool m_autoCollectChildStateButtons = true;
        [SerializeField] private bool m_includeInactiveStateButtons = true;
        [SerializeField] private int m_defaultIndex = 0;
        [SerializeField] private bool m_selectOnEnable = true;
        [SerializeField] private bool m_notifyOnInitialSelection = true;
        [SerializeField] private bool m_allowReselect = false;
        [SerializeField] public UnityEvent<int> OnTabSelected = new();
        [SerializeField] public UnityEvent<int> OnTabReselected = new();
        [SerializeField] public TabChangedEvent OnTabChanged = new();

        private readonly List<StateButton> m_activeTabs = new();
        private readonly List<UnityAction> m_clickHandlers = new();

        public int CurrentIndex { get; private set; } = -1;
        public int TabCount => m_activeTabs.Count;

        private void OnEnable()
        {
            RebuildTabs();
            AddButtonListeners();

            if (m_selectOnEnable)
            {
                int initialIndex = GetInitialIndex();
                if (IsValidIndex(initialIndex))
                {
                    SelectTab(initialIndex, m_notifyOnInitialSelection);
                }
                else
                {
                    CurrentIndex = -1;
                }

                return;
            }

            ApplySelectionState(CurrentIndex);
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
        }

        private void OnTabButtonClick(int index)
        {
            if (index == CurrentIndex)
            {
                if (m_allowReselect)
                {
                    OnTabReselected?.Invoke(index);
                }

                return;
            }

            SelectTab(index);
        }

        public void SelectTab(int index)
        {
            SelectTab(index, true);
        }

        public void SelectTab(int index, bool notify)
        {
            if (!TrySelectTab(index, notify))
            {
                Debug.LogError($"{nameof(Tabbar)} index out of range: {index}", this);
            }
        }

        public void SelectTabWithoutNotify(int index)
        {
            SelectTab(index, false);
        }

        public bool TrySelectTab(int index, bool notify = true)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            if (index == CurrentIndex)
            {
                if (m_allowReselect && notify)
                {
                    OnTabReselected?.Invoke(index);
                }

                return true;
            }

            int previousIndex = CurrentIndex;
            CurrentIndex = index;
            ApplySelectionState(index);

            if (notify)
            {
                OnTabSelected?.Invoke(index);
                OnTabChanged?.Invoke(previousIndex, index);
            }

            return true;
        }

        public Button GetButton(int index)
        {
            return IsValidIndex(index) ? m_activeTabs[index].Button : null;
        }

        public StateButton GetStateButton(int index)
        {
            return IsValidIndex(index) ? m_activeTabs[index] : null;
        }

        public void Refresh()
        {
            RemoveButtonListeners();
            RebuildTabs();
            AddButtonListeners();

            if (!IsValidIndex(CurrentIndex))
            {
                CurrentIndex = GetInitialIndex();
            }

            ApplySelectionState(CurrentIndex);
        }

        private void RebuildTabs()
        {
            m_activeTabs.Clear();

            if (m_tabs != null && m_tabs.Count > 0)
            {
                for (int i = 0; i < m_tabs.Count; i++)
                {
                    if (m_tabs[i] != null && m_tabs[i].HasButton)
                    {
                        m_activeTabs.Add(m_tabs[i]);
                    }
                }

                return;
            }

            if (!m_autoCollectChildStateButtons)
            {
                return;
            }

            var stateButtons = GetComponentsInChildren<StateButton>(m_includeInactiveStateButtons);
            for (int i = 0; i < stateButtons.Length; i++)
            {
                if (stateButtons[i] != null && stateButtons[i].HasButton)
                {
                    m_activeTabs.Add(stateButtons[i]);
                }
            }
        }

        private void AddButtonListeners()
        {
            RemoveButtonListeners();

            for (int i = 0; i < m_activeTabs.Count; i++)
            {
                int index = i;
                var stateButton = m_activeTabs[i];
                if (stateButton == null || !stateButton.HasButton)
                {
                    continue;
                }

                UnityAction handler = () => OnTabButtonClick(index);
                m_clickHandlers.Add(handler);
                stateButton.OnClick.AddListener(handler);
            }
        }

        private void RemoveButtonListeners()
        {
            int count = Mathf.Min(m_activeTabs.Count, m_clickHandlers.Count);
            for (int i = 0; i < count; i++)
            {
                var stateButton = m_activeTabs[i];
                if (stateButton != null)
                {
                    stateButton.OnClick.RemoveListener(m_clickHandlers[i]);
                }
            }

            m_clickHandlers.Clear();
        }

        private void ApplySelectionState(int selectedIndex)
        {
            for (int i = 0; i < m_activeTabs.Count; i++)
            {
                if (m_activeTabs[i] != null)
                {
                    m_activeTabs[i].SetSelectedWithoutNotify(i == selectedIndex);
                }
            }
        }

        private int GetInitialIndex()
        {
            if (IsValidIndex(CurrentIndex))
            {
                return CurrentIndex;
            }

            if (m_activeTabs.Count == 0)
            {
                return -1;
            }

            return Mathf.Clamp(m_defaultIndex, 0, m_activeTabs.Count - 1);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < m_activeTabs.Count;
        }
    }
}
