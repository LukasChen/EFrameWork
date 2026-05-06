using EFramework.Extensions.UI.VirtualList;
using EFramework.Runtime.UI;
using GameApp.Modules.EFrameExtensionShowcase.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Generated.UI
{
    public class v_EFrameExtensionShowcaseQVirtualListShowcaseWindow : BindingViewBase
    {
        private Button m_centerButton;
        private Button m_closeButton;
        private RectTransform m_contentRoot;
        private Button m_endButton;
        private QVirtualGridView m_grid;
        private Button m_gridButton;
        private QVirtualListItem m_gridItemPrefab;
        private Text m_infoText;
        private QVirtualListView m_variableList;
        private Button m_variableListButton;
        private QVirtualListItem m_listItemPrefab;
        private Button m_refreshButton;
        private Button m_reloadButton;
        private Button m_topButton;
        private VirtualListShowcaseWindow m_window;

        public Button CenterButton => m_centerButton;
        public Button CloseButton => m_closeButton;
        public RectTransform ContentRoot => m_contentRoot;
        public Button EndButton => m_endButton;
        public QVirtualGridView Grid => m_grid;
        public Button GridButton => m_gridButton;
        public QVirtualListItem GridItemPrefab => m_gridItemPrefab;
        public Text InfoText => m_infoText;
        public QVirtualListView VariableList => m_variableList;
        public Button VariableListButton => m_variableListButton;
        public QVirtualListItem ListItemPrefab => m_listItemPrefab;
        public Button RefreshButton => m_refreshButton;
        public Button ReloadButton => m_reloadButton;
        public Button TopButton => m_topButton;
        public VirtualListShowcaseWindow Window => m_window;

        public v_EFrameExtensionShowcaseQVirtualListShowcaseWindow() : base()
        {
        }

        public v_EFrameExtensionShowcaseQVirtualListShowcaseWindow(QUIBinding binding) : base(binding)
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

            m_centerButton = Binding.GetComponent<Button>("CenterButton");
            m_closeButton = Binding.GetComponent<Button>("CloseButton");
            m_contentRoot = Binding.GetComponent<RectTransform>("ContentRoot");
            m_endButton = Binding.GetComponent<Button>("EndButton");
            m_grid = Binding.GetComponent<QVirtualGridView>("Grid");
            m_gridButton = Binding.GetComponent<Button>("GridButton");
            m_gridItemPrefab = Binding.GetComponent<QVirtualListItem>("GridItemPrefab");
            m_infoText = Binding.GetComponent<Text>("InfoText");
            m_variableList = Binding.GetComponent<QVirtualListView>("VariableList");
            m_variableListButton = Binding.GetComponent<Button>("VariableListButton");
            m_listItemPrefab = Binding.GetComponent<QVirtualListItem>("ListItemPrefab");
            m_refreshButton = Binding.GetComponent<Button>("RefreshButton");
            m_reloadButton = Binding.GetComponent<Button>("ReloadButton");
            m_topButton = Binding.GetComponent<Button>("TopButton");
            m_window = Binding.GetComponent<VirtualListShowcaseWindow>("Window");
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
            if (m_centerButton == null) { Debug.LogError("Missing reference: CenterButton"); allValid = false; }
            if (m_closeButton == null) { Debug.LogError("Missing reference: CloseButton"); allValid = false; }
            if (m_contentRoot == null) { Debug.LogError("Missing reference: ContentRoot"); allValid = false; }
            if (m_endButton == null) { Debug.LogError("Missing reference: EndButton"); allValid = false; }
            if (m_grid == null) { Debug.LogError("Missing reference: Grid"); allValid = false; }
            if (m_gridButton == null) { Debug.LogError("Missing reference: GridButton"); allValid = false; }
            if (m_gridItemPrefab == null) { Debug.LogError("Missing reference: GridItemPrefab"); allValid = false; }
            if (m_infoText == null) { Debug.LogError("Missing reference: InfoText"); allValid = false; }
            if (m_variableList == null) { Debug.LogError("Missing reference: VariableList"); allValid = false; }
            if (m_variableListButton == null) { Debug.LogError("Missing reference: VariableListButton"); allValid = false; }
            if (m_listItemPrefab == null) { Debug.LogError("Missing reference: ListItemPrefab"); allValid = false; }
            if (m_refreshButton == null) { Debug.LogError("Missing reference: RefreshButton"); allValid = false; }
            if (m_reloadButton == null) { Debug.LogError("Missing reference: ReloadButton"); allValid = false; }
            if (m_topButton == null) { Debug.LogError("Missing reference: TopButton"); allValid = false; }
            if (m_window == null) { Debug.LogError("Missing reference: Window"); allValid = false; }
            return allValid;
        }
    }
}
