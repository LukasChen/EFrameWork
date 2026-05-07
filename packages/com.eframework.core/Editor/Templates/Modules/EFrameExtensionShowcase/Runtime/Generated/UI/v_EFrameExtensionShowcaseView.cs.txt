using EFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Generated.UI
{
    public class v_EFrameExtensionShowcaseView : BindingViewBase
    {
        private Button m_backButton;
        private Button m_debugConsoleButton;
        private Text m_debugConsoleLabelText;
        private Text m_detailText;
        private Button m_feedbackButton;
        private Text m_feedbackLabelText;
        private Button m_virtualListButton;
        private Text m_virtualListLabelText;

        public Button BackButton => m_backButton;
        public Button DebugConsoleButton => m_debugConsoleButton;
        public Text DebugConsoleLabelText => m_debugConsoleLabelText;
        public Text DetailText => m_detailText;
        public Button FeedbackButton => m_feedbackButton;
        public Text FeedbackLabelText => m_feedbackLabelText;
        public Button VirtualListButton => m_virtualListButton;
        public Text VirtualListLabelText => m_virtualListLabelText;

        public v_EFrameExtensionShowcaseView() : base()
        {
        }

        public v_EFrameExtensionShowcaseView(QUIBinding binding) : base(binding)
        {
            InitializeFromBinding();
        }

        protected override void OnBindingSet()
        {
            base.OnBindingSet();
            InitializeFromBinding();
        }

        private void InitializeFromBinding()
        {
            if (Binding == null)
            {
                Debug.LogWarning($"{GetType().Name}: Binding is null, component initialization skipped.");
                return;
            }

            m_backButton = Binding.GetComponent<Button>("BackButton");
            m_debugConsoleButton = Binding.GetComponent<Button>("DebugConsoleButton");
            m_debugConsoleLabelText = Binding.GetComponent<Text>("DebugConsoleLabelText");
            m_detailText = Binding.GetComponent<Text>("DetailText");
            m_feedbackButton = Binding.GetComponent<Button>("FeedbackButton");
            m_feedbackLabelText = Binding.GetComponent<Text>("FeedbackLabelText");
            m_virtualListButton = Binding.GetComponent<Button>("VirtualListButton");
            m_virtualListLabelText = Binding.GetComponent<Text>("VirtualListLabelText");
        }

        public override void RefreshComponents()
        {
            base.RefreshComponents();
            InitializeFromBinding();
        }

        public override bool ValidateReferences()
        {
            if (!base.ValidateReferences())
            {
                return false;
            }

            var allValid = true;
            if (m_backButton == null) { Debug.LogError("Missing reference: BackButton"); allValid = false; }
            if (m_debugConsoleButton == null) { Debug.LogError("Missing reference: DebugConsoleButton"); allValid = false; }
            if (m_debugConsoleLabelText == null) { Debug.LogError("Missing reference: DebugConsoleLabelText"); allValid = false; }
            if (m_detailText == null) { Debug.LogError("Missing reference: DetailText"); allValid = false; }
            if (m_feedbackButton == null) { Debug.LogError("Missing reference: FeedbackButton"); allValid = false; }
            if (m_feedbackLabelText == null) { Debug.LogError("Missing reference: FeedbackLabelText"); allValid = false; }
            if (m_virtualListButton == null) { Debug.LogError("Missing reference: VirtualListButton"); allValid = false; }
            if (m_virtualListLabelText == null) { Debug.LogError("Missing reference: VirtualListLabelText"); allValid = false; }
            return allValid;
        }
    }
}
