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

        [Tooltip("Explicit CheckableButton items. Leave empty to auto collect child CheckableButton components.")]
        [SerializeField] private List<CheckableButton> m_tabs = new();
        [SerializeField] private bool m_autoCollectChildCheckableButtons = true;
        [SerializeField] private bool m_includeInactiveCheckableButtons = true;
        [SerializeField] private int m_defaultIndex = 0;
        [SerializeField] private bool m_checkOnEnable = true;
        [SerializeField] private bool m_notifyOnInitialCheck = true;
        [SerializeField] private bool m_focusCheckedOnEnable = false;
        [SerializeField] private bool m_allowReselect = false;
        [SerializeField] public UnityEvent<int> OnTabChecked = new();
        [SerializeField] public UnityEvent<int> OnTabReselected = new();
        [SerializeField] public TabChangedEvent OnTabChanged = new();

        private readonly List<CheckableButton> m_activeTabs = new();
        private readonly List<UnityAction> m_clickHandlers = new();
        private readonly List<bool> m_previousToggleOnClick = new();

        public int CurrentIndex { get; private set; } = -1;
        public int TabCount => m_activeTabs.Count;

        private void OnEnable()
        {
            RebuildTabs();
            AddButtonListeners();

            if (m_checkOnEnable)
            {
                int initialIndex = GetInitialIndex();
                if (IsValidIndex(initialIndex))
                {
                    CheckTab(initialIndex, m_notifyOnInitialCheck);

                    if (m_focusCheckedOnEnable)
                    {
                        FocusTab(initialIndex);
                    }
                }
                else
                {
                    CurrentIndex = -1;
                }

                return;
            }

            ApplyCheckedState(CurrentIndex);
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
        }

        private void OnTabButtonClick(int index)
        {
            if (index == CurrentIndex)
            {
                ApplyCheckedState(CurrentIndex);

                if (m_allowReselect)
                {
                    OnTabReselected?.Invoke(index);
                }

                return;
            }

            CheckTab(index);
        }

        public void CheckTab(int index)
        {
            CheckTab(index, true);
        }

        public void CheckTab(int index, bool notify)
        {
            if (!TryCheckTab(index, notify))
            {
                Debug.LogError($"{nameof(Tabbar)} index out of range: {index}", this);
            }
        }

        public void CheckTabWithoutNotify(int index)
        {
            CheckTab(index, false);
        }

        public bool TryCheckTab(int index, bool notify = true)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            if (index == CurrentIndex)
            {
                ApplyCheckedState(CurrentIndex);

                if (m_allowReselect && notify)
                {
                    OnTabReselected?.Invoke(index);
                }

                return true;
            }

            int previousIndex = CurrentIndex;
            CurrentIndex = index;
            ApplyCheckedState(index);

            if (notify)
            {
                OnTabChecked?.Invoke(index);
                OnTabChanged?.Invoke(previousIndex, index);
            }

            return true;
        }

        public void FocusCurrentTab()
        {
            FocusTab(CurrentIndex);
        }

        public void FocusTab(int index)
        {
            if (IsValidIndex(index))
            {
                m_activeTabs[index].Focus();
            }
        }

        public Button GetButton(int index)
        {
            return IsValidIndex(index) ? m_activeTabs[index].Button : null;
        }

        public CheckableButton GetCheckableButton(int index)
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

            ApplyCheckedState(CurrentIndex);
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

            if (!m_autoCollectChildCheckableButtons)
            {
                return;
            }

            var checkableButtons = GetComponentsInChildren<CheckableButton>(m_includeInactiveCheckableButtons);
            for (int i = 0; i < checkableButtons.Length; i++)
            {
                if (checkableButtons[i] != null && checkableButtons[i].HasButton)
                {
                    m_activeTabs.Add(checkableButtons[i]);
                }
            }
        }

        private void AddButtonListeners()
        {
            RemoveButtonListeners();

            for (int i = 0; i < m_activeTabs.Count; i++)
            {
                int index = i;
                var checkableButton = m_activeTabs[i];
                if (checkableButton == null || !checkableButton.HasButton)
                {
                    continue;
                }

                UnityAction handler = () => OnTabButtonClick(index);
                m_clickHandlers.Add(handler);
                m_previousToggleOnClick.Add(checkableButton.ToggleOnClick);
                checkableButton.ToggleOnClick = false;
                checkableButton.OnClick.AddListener(handler);
            }
        }

        private void RemoveButtonListeners()
        {
            int count = Mathf.Min(m_activeTabs.Count, m_clickHandlers.Count);
            for (int i = 0; i < count; i++)
            {
                var checkableButton = m_activeTabs[i];
                if (checkableButton != null)
                {
                    checkableButton.OnClick.RemoveListener(m_clickHandlers[i]);
                    checkableButton.ToggleOnClick = m_previousToggleOnClick[i];
                }
            }

            m_clickHandlers.Clear();
            m_previousToggleOnClick.Clear();
        }

        private void ApplyCheckedState(int checkedIndex)
        {
            for (int i = 0; i < m_activeTabs.Count; i++)
            {
                if (m_activeTabs[i] != null)
                {
                    m_activeTabs[i].SetCheckedWithoutNotify(i == checkedIndex);
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
