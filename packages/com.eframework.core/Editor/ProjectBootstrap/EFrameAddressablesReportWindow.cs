#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EFramework.Editor.ProjectBootstrap
{
    internal sealed class EFrameAddressablesReportWindow : EditorWindow
    {
        private const string WindowTitle = "EFrame Addressables";

        private Vector2 m_scrollPosition;
        private string m_filter = string.Empty;
        private int m_selectedMainTab;
        private bool m_analysisReportExpanded;
        private readonly HashSet<string> m_collapsedGroups = new(StringComparer.Ordinal);
        private EFrameAddressablesBootstrapUtility.ManagedResourceReport m_resourceReport;
        private EFrameAddressablesBootstrapUtility.DirectoryStructureReport m_directoryReport;

        public static void Open()
        {
            var window = GetWindow<EFrameAddressablesReportWindow>(WindowTitle);
            window.minSize = new Vector2(920f, 520f);
            window.RefreshReports();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshReports();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (m_resourceReport == null || m_directoryReport == null)
            {
                RefreshReports();
            }

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            DrawMainTabs();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    RefreshReports();
                }

                if (GUILayout.Button("Sync Groups And Generate ResPath", EditorStyles.toolbarButton, GUILayout.Width(220f)))
                {
                    SyncNow();
                }

                GUILayout.Space(12f);
                GUILayout.Label("Filter", GUILayout.Width(36f));
                m_filter = GUILayout.TextField(m_filter ?? string.Empty, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180f));

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select ResPath", EditorStyles.toolbarButton, GUILayout.Width(92f)))
                {
                    SelectAsset(EFrameAddressablesBootstrapUtility.GeneratedResPathFilePath);
                }
            }
        }

        private void DrawAnalysisReportMenu()
        {
            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(m_analysisReportExpanded ? "v" : ">", EditorStyles.miniButton, GUILayout.Width(22f), GUILayout.Height(22f)))
                    {
                        m_analysisReportExpanded = !m_analysisReportExpanded;
                    }

                    if (GUILayout.Button("Analysis Report", EditorStyles.boldLabel, GUILayout.Height(22f)))
                    {
                        m_analysisReportExpanded = !m_analysisReportExpanded;
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(GetAnalysisReportSummary(), EditorStyles.miniLabel, GUILayout.Height(22f));
                }

                if (m_analysisReportExpanded)
                {
                    DrawSummary();
                    EditorGUILayout.Space(4f);
                    DrawDirectoryChecks();
                }
            }
        }

        private void DrawSummary()
        {
            var addressablesMessage = m_resourceReport.AddressablesInitialized
                ? "Addressables settings found."
                : "Addressables settings are missing. Sync will initialize them.";
            EditorGUILayout.HelpBox(addressablesMessage, m_resourceReport.AddressablesInitialized ? MessageType.Info : MessageType.Warning);

            var syncMessage = $"Managed resources: {m_resourceReport.Items.Count}. Synced: {m_resourceReport.SyncedCount}. Needs sync: {m_resourceReport.NeedsSyncCount}. Stale: {m_resourceReport.StaleCount}.";
            EditorGUILayout.HelpBox(syncMessage, m_resourceReport.NeedsSyncCount == 0 ? MessageType.Info : MessageType.Warning);

            var resPathType = m_resourceReport.ResPathCurrent ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(m_resourceReport.ResPathMessage ?? "ResPath status is unknown.", resPathType);

            var directoryMessage = m_directoryReport.IsClean
                ? "Directory structure matches the EFrame convention checks."
                : $"Directory checks found {m_directoryReport.MissingRequiredCount} missing required folders, {m_directoryReport.ErrorCount} errors, and {m_directoryReport.WarningCount} warnings.";
            EditorGUILayout.HelpBox(directoryMessage, m_directoryReport.IsClean ? MessageType.Info : MessageType.Warning);

            foreach (var issue in m_resourceReport.Issues)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }

        private void DrawMainTabs()
        {
            EditorGUILayout.Space(8f);
            if (m_selectedMainTab > (int)MainTab.Report)
            {
                m_selectedMainTab = (int)MainTab.ResPathDirSelection;
            }

            m_selectedMainTab = GUILayout.Toolbar(m_selectedMainTab, new[]
            {
                "ResPath Dir Selection",
                "Need Sync Mapping",
                "Report"
            }, GetMainTabStyle(), GUILayout.Height(30f));

            EditorGUILayout.Space(8f);
            switch ((MainTab)m_selectedMainTab)
            {
                case MainTab.ResPathDirSelection:
                    DrawResPathDirectorySelection();
                    break;
                case MainTab.Mapping:
                    DrawMapping();
                    break;
                case MainTab.Report:
                    DrawReport();
                    break;
            }
        }

        private void DrawResPathDirectorySelection()
        {
            EditorGUILayout.LabelField("ResPath Directory Selection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Addressables owns every managed resource directory. Check directory trees whose managed resources should generate ResPath entries for code-driven loading.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Directory Path", EditorStyles.boldLabel);
                GUILayout.Label("State", EditorStyles.boldLabel, GUILayout.Width(110f));
                GUILayout.Label("Include", EditorStyles.boldLabel, GUILayout.Width(64f));
            }

            var visibleCount = 0;
            foreach (var directory in EFrameAddressablesBootstrapUtility.CollectResPathSelectableDirectories())
            {
                if (!MatchesFilter(directory))
                {
                    continue;
                }

                visibleCount++;
                DrawResPathDirectoryRow(directory);
            }

            if (visibleCount == 0)
            {
                EditorGUILayout.HelpBox("No selectable directories match the current filter.", MessageType.Info);
            }
        }

        private void DrawResPathDirectoryRow(string directory)
        {
            var explicitIncluded = EFrameAddressablesBootstrapUtility.IsResPathDirectoryExplicitlyIncluded(directory);
            var state = explicitIncluded ? "Selected" : "Excluded";
            var previousBackgroundColor = GUI.backgroundColor;
            if (explicitIncluded)
            {
                GUI.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.28f, 0.58f, 0.38f)
                    : new Color(0.72f, 0.96f, 0.78f);
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = previousBackgroundColor;
                var pathStyle = explicitIncluded ? GetSelectedDirectoryPathStyle() : EditorStyles.label;
                var stateStyle = explicitIncluded ? GetSelectedDirectoryStateStyle() : EditorStyles.label;

                if (GUILayout.Button(directory, pathStyle))
                {
                    SelectAsset(directory);
                }

                GUILayout.Label(state, stateStyle, GUILayout.Width(110f));

                EditorGUI.BeginChangeCheck();
                var nextExplicitIncluded = EditorGUILayout.Toggle(explicitIncluded, GUILayout.Width(64f));
                if (EditorGUI.EndChangeCheck())
                {
                    EFrameAddressablesBootstrapUtility.SetResPathDirectoryIncluded(directory, nextExplicitIncluded);
                    RefreshReports();
                }
            }
        }

        private static GUIStyle GetSelectedDirectoryPathStyle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.72f, 1f, 0.78f)
                        : new Color(0.06f, 0.42f, 0.16f)
                }
            };
            return style;
        }

        private static GUIStyle GetSelectedDirectoryStateStyle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.72f, 1f, 0.78f)
                        : new Color(0.06f, 0.42f, 0.16f)
                }
            };
            return style;
        }

        private void DrawMapping()
        {
            EditorGUILayout.LabelField("Need Sync Mapping", EditorStyles.boldLabel);
            DrawMappingGroup(ResourceStatusTab.NeedsSync, $"Needs Sync ({CountItems(ResourceStatusTab.NeedsSync)})");
        }

        private void DrawReport()
        {
            EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
            DrawAnalysisReportMenu();
            EditorGUILayout.Space(8f);
            DrawMappingGroup(ResourceStatusTab.Stale, $"Stale Mapping ({CountItems(ResourceStatusTab.Stale)})");
            DrawMappingGroup(ResourceStatusTab.Synced, $"Synced Mapping ({CountItems(ResourceStatusTab.Synced)})");
        }

        private void DrawMappingGroup(ResourceStatusTab statusTab, string label)
        {
            if (!DrawFoldoutGroup($"mapping:{statusTab}", label))
            {
                return;
            }

            var visibleCount = 0;
            foreach (var item in m_resourceReport.Items)
            {
                if (!MatchesStatusTab(item, statusTab) || !MatchesFilter(item))
                {
                    continue;
                }

                visibleCount++;
                DrawMappingRow(item);
            }

            if (visibleCount == 0)
            {
                EditorGUILayout.HelpBox($"No {GetStatusTabName(statusTab)} resources match the current filter.", MessageType.Info);
            }
        }

        private void DrawMappingRow(EFrameAddressablesBootstrapUtility.ManagedResourceReportItem item)
        {
            var previousColor = GUI.color;
            if (item.IsStale)
            {
                GUI.color = new Color(1f, 0.72f, 0.72f);
            }
            else if (item.NeedsSync)
            {
                GUI.color = new Color(1f, 0.9f, 0.62f);
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUI.color = previousColor;
                if (GUILayout.Button(item.AssetPath ?? "<missing>", EditorStyles.label))
                {
                    SelectAsset(item.AssetPath);
                }
            }
        }

        private void DrawDirectoryChecks()
        {
            EditorGUILayout.LabelField("Directory Convention Checks", EditorStyles.boldLabel);

            if (m_directoryReport.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No directory convention issues found.", MessageType.Info);
                return;
            }

            DrawDirectoryIssueGroup(DirectoryIssueTab.Missing, $"Missing ({CountDirectoryIssues(DirectoryIssueTab.Missing)})");
            DrawDirectoryIssueGroup(DirectoryIssueTab.Warning, $"Warning ({CountDirectoryIssues(DirectoryIssueTab.Warning)})");
            DrawDirectoryIssueGroup(DirectoryIssueTab.Error, $"Error ({CountDirectoryIssues(DirectoryIssueTab.Error)})");
        }

        private void DrawDirectoryIssueGroup(DirectoryIssueTab issueTab, string label)
        {
            if (!DrawFoldoutGroup($"check:{issueTab}", label))
            {
                return;
            }

            var visibleCount = 0;
            foreach (var issue in m_directoryReport.Issues)
            {
                if (!MatchesDirectoryIssueTab(issue, issueTab)
                    || (!MatchesFilter(issue.Path) && !MatchesFilter(issue.Message)))
                {
                    continue;
                }

                visibleCount++;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(issue.Path, EditorStyles.label))
                    {
                        SelectAsset(issue.Path);
                    }

                    EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedMiniLabel);
                }
            }

            if (visibleCount == 0)
            {
                EditorGUILayout.HelpBox($"No {GetDirectoryIssueTabName(issueTab)} directory issues match the current filter.", MessageType.Info);
            }
        }

        private bool DrawFoldoutGroup(string key, string label)
        {
            var expanded = !m_collapsedGroups.Contains(key);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button(expanded ? "v" : ">", EditorStyles.miniButton, GUILayout.Width(22f)))
                {
                    expanded = !expanded;
                    if (expanded)
                    {
                        m_collapsedGroups.Remove(key);
                    }
                    else
                    {
                        m_collapsedGroups.Add(key);
                    }
                }

                GUILayout.Label(label, EditorStyles.boldLabel);
            }

            return expanded;
        }

        private void RefreshReports()
        {
            m_resourceReport = EFrameAddressablesBootstrapUtility.BuildManagedResourceReport();
            m_directoryReport = EFrameAddressablesBootstrapUtility.BuildDirectoryStructureReport();
            Repaint();
        }

        private void SyncNow()
        {
            EditorUtility.DisplayProgressBar("EFrame Addressables", "Syncing managed groups and generating ResPath...", 0.5f);
            try
            {
                if (!EFrameAddressablesBootstrapUtility.SyncProjectAddressablesAndGenerateResPath(out var message))
                {
                    Debug.LogError($"[EFrame Addressables] {message}");
                    EditorUtility.DisplayDialog("EFrame Addressables Sync Failed", message, "OK");
                    return;
                }

                Debug.Log($"[EFrame Addressables] {message}");
                RefreshReports();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private bool MatchesFilter(EFrameAddressablesBootstrapUtility.ManagedResourceReportItem item)
        {
            return MatchesFilter(item.AssetPath)
                || MatchesFilter(item.ExpectedGroup)
                || MatchesFilter(item.ExpectedAddress)
                || MatchesFilter(item.ResPathMember)
                || MatchesFilter(item.ActualGroup)
                || MatchesFilter(item.ActualAddress)
                || MatchesFilter(item.Status);
        }

        private int CountItems(ResourceStatusTab statusTab)
        {
            if (m_resourceReport?.Items == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var item in m_resourceReport.Items)
            {
                if (MatchesStatusTab(item, statusTab))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool MatchesStatusTab(EFrameAddressablesBootstrapUtility.ManagedResourceReportItem item, ResourceStatusTab statusTab)
        {
            switch (statusTab)
            {
                case ResourceStatusTab.NeedsSync:
                    return item.NeedsSync && !item.IsStale;
                case ResourceStatusTab.Stale:
                    return item.IsStale;
                case ResourceStatusTab.Synced:
                    return !item.NeedsSync && !item.IsStale;
                default:
                    return false;
            }
        }

        private static string GetStatusTabName(ResourceStatusTab statusTab)
        {
            switch (statusTab)
            {
                case ResourceStatusTab.NeedsSync:
                    return "Needs Sync";
                case ResourceStatusTab.Stale:
                    return "Stale";
                case ResourceStatusTab.Synced:
                    return "Synced";
                default:
                    return "selected";
            }
        }

        private int CountDirectoryIssues(DirectoryIssueTab issueTab)
        {
            if (m_directoryReport?.Issues == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var issue in m_directoryReport.Issues)
            {
                if (MatchesDirectoryIssueTab(issue, issueTab))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool MatchesDirectoryIssueTab(EFrameAddressablesBootstrapUtility.DirectoryStructureIssue issue, DirectoryIssueTab issueTab)
        {
            switch (issueTab)
            {
                case DirectoryIssueTab.Missing:
                    return string.Equals(issue.Severity, "Missing", StringComparison.OrdinalIgnoreCase);
                case DirectoryIssueTab.Warning:
                    return string.Equals(issue.Severity, "Warning", StringComparison.OrdinalIgnoreCase);
                case DirectoryIssueTab.Error:
                    return string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private static string GetDirectoryIssueTabName(DirectoryIssueTab issueTab)
        {
            switch (issueTab)
            {
                case DirectoryIssueTab.Missing:
                    return "Missing";
                case DirectoryIssueTab.Warning:
                    return "Warning";
                case DirectoryIssueTab.Error:
                    return "Error";
                default:
                    return "selected";
            }
        }

        private bool MatchesFilter(string value)
        {
            return string.IsNullOrWhiteSpace(m_filter)
                || (!string.IsNullOrEmpty(value) && value.IndexOf(m_filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private string GetAnalysisReportSummary()
        {
            var issueCount = m_directoryReport.MissingRequiredCount + m_directoryReport.WarningCount + m_directoryReport.ErrorCount;
            var syncCount = m_resourceReport.NeedsSyncCount + m_resourceReport.StaleCount;
            return $"Managed: {m_resourceReport.Items.Count} | Sync Issues: {syncCount} | Directory Issues: {issueCount}";
        }

        private static GUIStyle GetMainTabStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 30f,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            return style;
        }

        private static void SelectAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private enum MainTab
        {
            ResPathDirSelection = 0,
            Mapping = 1,
            Report = 2
        }

        private enum ResourceStatusTab
        {
            NeedsSync = 0,
            Stale = 1,
            Synced = 2
        }

        private enum DirectoryIssueTab
        {
            Missing = 0,
            Warning = 1,
            Error = 2
        }
    }
}
#endif
