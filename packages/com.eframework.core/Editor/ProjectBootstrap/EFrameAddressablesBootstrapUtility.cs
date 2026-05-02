#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace EFramework.Editor.ProjectBootstrap
{
    internal static class EFrameAddressablesBootstrapUtility
    {
        internal const string AppResRootPath = "Assets/App/Res";
        internal const string AppBootstrapRootPath = "Assets/App/Res/Bootstrap";
        internal const string AppSceneAssetsRootPath = "Assets/App/Res/SceneAssets";
        internal const string ProjectScenesRootPath = "Assets/Scenes";
        internal const string ModulesRootPath = "Assets/Modules";
        internal const string GeneratedResPathFilePath = "Assets/App/Runtime/Generated/Res/ResPath.Generated.cs";
        internal const string ResPathSelectionSettingsFilePath = "Assets/Settings/EFrameAddressablesResPathSettings.json";
        internal const string GeneratedResPathNamespace = "EFramework.Generated";

        internal const string AppSharedGroupName = "App Shared Group";
        internal const string AppBootstrapGroupName = "App Bootstrap Group";
        internal const string AppSceneAssetsGroupName = "App SceneAssets Group";
        internal const string AppScenesGroupName = "App Scenes Group";

        private static readonly string[] ManagedRootPaths =
        {
            AppResRootPath,
            ProjectScenesRootPath,
            ModulesRootPath
        };

        internal const string ManagedLabelName = "eframe-managed";
        private const string ManagedLabel = ManagedLabelName;
        private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
            "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
            "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        private readonly struct GroupBinding
        {
            public GroupBinding(string groupName, string address)
            {
                GroupName = groupName;
                Address = address;
            }

            public string GroupName { get; }
            public string Address { get; }
        }

        private readonly struct GeneratedAddressEntry
        {
            public GeneratedAddressEntry(string groupName, string address, string assetPath)
            {
                GroupName = groupName;
                Address = address;
                AssetPath = assetPath;
            }

            public string GroupName { get; }
            public string Address { get; }
            public string AssetPath { get; }
        }

        [Serializable]
        private sealed class ResPathSelectionSettings
        {
            public List<string> IncludedDirectories = new();
        }

        internal sealed class ManagedResourceReport
        {
            public List<ManagedResourceReportItem> Items { get; } = new();
            public List<string> Issues { get; } = new();
            public bool AddressablesInitialized { get; set; }
            public bool ResPathCurrent { get; set; }
            public string ResPathMessage { get; set; }
            public int SyncedCount { get; set; }
            public int NeedsSyncCount { get; set; }
            public int StaleCount { get; set; }
        }

        internal sealed class ManagedResourceReportItem
        {
            public string AssetPath { get; set; }
            public string ExpectedGroup { get; set; }
            public string ExpectedAddress { get; set; }
            public string ResPathMember { get; set; }
            public bool IncludedInResPath { get; set; }
            public string ActualGroup { get; set; }
            public string ActualAddress { get; set; }
            public bool HasManagedLabel { get; set; }
            public bool IsStale { get; set; }
            public bool NeedsSync { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
        }

        internal sealed class DirectoryStructureReport
        {
            public List<DirectoryStructureIssue> Issues { get; } = new();
            public int MissingRequiredCount { get; set; }
            public int WarningCount { get; set; }
            public int ErrorCount { get; set; }
            public bool IsClean => MissingRequiredCount == 0 && WarningCount == 0 && ErrorCount == 0;
        }

        internal sealed class DirectoryStructureIssue
        {
            public DirectoryStructureIssue(string severity, string path, string message)
            {
                Severity = severity;
                Path = path;
                Message = message;
            }

            public string Severity { get; }
            public string Path { get; }
            public string Message { get; }
        }

        private sealed class ResPathNode
        {
            public ResPathNode(string originalName, string identifier)
            {
                OriginalName = originalName;
                Identifier = identifier;
            }

            public string OriginalName { get; }
            public string Identifier { get; }
            public SortedDictionary<string, ResPathNode> Children { get; } = new(StringComparer.Ordinal);
            public string Address { get; set; }
        }

        internal static bool AreAddressablesInitialized()
        {
            return AddressableAssetSettingsDefaultObject.GetSettings(false) != null;
        }

        internal static bool AreProjectAddressablesGroupsReady()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            return settings != null
                && settings.FindGroup(AppSharedGroupName) != null
                && settings.FindGroup(AppBootstrapGroupName) != null
                && settings.FindGroup(AppSceneAssetsGroupName) != null
                && settings.FindGroup(AppScenesGroupName) != null;
        }

        internal static bool IsGeneratedResPathReady()
        {
            return File.Exists(GetProjectAbsolutePath(GeneratedResPathFilePath));
        }

        internal static bool IsGeneratedResPathCurrent(out string message)
        {
            if (!TryCollectResPathAddressEntries(out var entries, out message))
            {
                return false;
            }

            var generatedResPath = GetProjectAbsolutePath(GeneratedResPathFilePath);
            if (!File.Exists(generatedResPath))
            {
                message = $"{GeneratedResPathFilePath} is missing.";
                return false;
            }

            var expectedResPathContent = BuildGeneratedResPathContent(GeneratedResPathNamespace, entries);
            if (!string.Equals(File.ReadAllText(generatedResPath), expectedResPathContent, StringComparison.Ordinal))
            {
                message = $"{GeneratedResPathFilePath} is stale.";
                return false;
            }

            message = $"{GeneratedResPathFilePath} is current.";
            return true;
        }

        internal static bool EnsureAddressablesInitialized(out string message)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings != null)
            {
                message = "Addressables is already initialized.";
                return true;
            }

            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                message = "Failed to create Addressables settings.";
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            message = "Initialized Addressables settings under Assets/AddressableAssetsData.";
            return true;
        }

        internal static bool EnsureProjectAddressablesGroups(out string message)
        {
            EnsureFolderExists(AppResRootPath);
            EnsureFolderExists(ProjectScenesRootPath);
            EnsureFolderExists(ModulesRootPath);
            return SyncManagedAssets(out message);
        }

        internal static bool SyncProjectAddressablesAndGenerateResPath(out string message)
        {
            var messages = new List<string>();

            if (!EnsureAddressablesInitialized(out var initializationMessage))
            {
                message = initializationMessage;
                return false;
            }

            messages.Add(initializationMessage);

            if (!EnsureProjectAddressablesGroups(out var syncMessage))
            {
                messages.Add(syncMessage);
                message = string.Join(Environment.NewLine, messages);
                return false;
            }

            messages.Add(syncMessage);
            message = string.Join(Environment.NewLine, messages);
            return true;
        }

        internal static bool SyncImportedAssets(IEnumerable<string> assetPaths, out string message)
        {
            if (AreImportedManagedAssetsCurrent(assetPaths, out message))
            {
                return true;
            }

            return SyncProjectAddressablesAndGenerateResPath(out message);
        }

        internal static IReadOnlyList<string> GetResPathIncludedDirectories()
        {
            return LoadResPathSelectionSettings().IncludedDirectories;
        }

        internal static bool IsResPathDirectoryExplicitlyIncluded(string directoryPath)
        {
            var normalizedDirectory = NormalizeAssetPath(directoryPath);
            foreach (var includedDirectory in LoadResPathSelectionSettings().IncludedDirectories)
            {
                if (string.Equals(includedDirectory, normalizedDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void SetResPathDirectoryIncluded(string directoryPath, bool included)
        {
            var normalizedDirectory = NormalizeAssetPath(directoryPath);
            if (string.IsNullOrEmpty(normalizedDirectory))
            {
                return;
            }

            var settings = LoadResPathSelectionSettings();
            settings.IncludedDirectories.RemoveAll(path => string.Equals(path, normalizedDirectory, StringComparison.OrdinalIgnoreCase));
            if (included)
            {
                settings.IncludedDirectories.Add(normalizedDirectory);
            }

            settings.IncludedDirectories.Sort(StringComparer.OrdinalIgnoreCase);
            SaveResPathSelectionSettings(settings);
        }

        internal static List<string> CollectResPathSelectableDirectories()
        {
            var directories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                AppResRootPath,
                ProjectScenesRootPath,
                ModulesRootPath
            };

            AddSelectableDirectoriesUnderRoot(directories, AppResRootPath);
            AddSelectableDirectoriesUnderRoot(directories, ProjectScenesRootPath);

            if (AssetDatabase.IsValidFolder(ModulesRootPath))
            {
                foreach (var moduleFolder in AssetDatabase.GetSubFolders(ModulesRootPath))
                {
                    directories.Add(moduleFolder);
                    AddSelectableModuleScopeDirectories(directories, $"{moduleFolder}/Res");
                    AddSelectableModuleScopeDirectories(directories, $"{moduleFolder}/Scenes");
                }
            }

            return new List<string>(directories);
        }

        internal static ManagedResourceReport BuildManagedResourceReport()
        {
            var report = new ManagedResourceReport();
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            report.AddressablesInitialized = settings != null;

            CollectAddressableManagedEntries(false, out var generatedEntries, out var duplicateIssues);
            foreach (var duplicateIssue in duplicateIssues)
            {
                report.Issues.Add(duplicateIssue);
            }

            var resPathEntries = FilterResPathEntries(generatedEntries);
            var resPathLookup = BuildResPathMemberLookup(resPathEntries);
            var liveAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var generatedEntry in generatedEntries)
            {
                var includedInResPath = IsAssetIncludedInGeneratedResPath(generatedEntry.AssetPath);
                liveAssetPaths.Add(generatedEntry.AssetPath);

                var item = new ManagedResourceReportItem
                {
                    AssetPath = generatedEntry.AssetPath,
                    ExpectedGroup = generatedEntry.GroupName,
                    ExpectedAddress = generatedEntry.Address,
                    IncludedInResPath = includedInResPath,
                    ResPathMember = includedInResPath && resPathLookup.TryGetValue(generatedEntry.Address, out var member)
                        ? member
                        : string.Empty,
                    Status = "Needs Sync",
                    Message = "Addressables is not initialized."
                };

                if (settings != null)
                {
                    var guid = AssetDatabase.AssetPathToGUID(generatedEntry.AssetPath);
                    var entry = string.IsNullOrEmpty(guid) ? null : settings.FindAssetEntry(guid);
                    if (entry == null)
                    {
                        item.Message = "Missing Addressables entry.";
                    }
                    else
                    {
                        item.ActualGroup = entry.parentGroup?.Name;
                        item.ActualAddress = entry.address;
                        item.HasManagedLabel = HasLabel(entry, ManagedLabel);
                        item.NeedsSync = !string.Equals(item.ActualGroup, item.ExpectedGroup, StringComparison.Ordinal)
                            || !string.Equals(item.ActualAddress, item.ExpectedAddress, StringComparison.Ordinal)
                            || !item.HasManagedLabel;
                        item.Status = item.NeedsSync ? "Needs Sync" : "Synced";
                        item.Message = item.NeedsSync
                            ? "Group, address, or managed label differs from the directory convention."
                            : "Addressables entry matches the EFrame convention.";
                    }
                }
                else
                {
                    item.NeedsSync = true;
                }

                if (item.Status == "Synced")
                {
                    report.SyncedCount++;
                }
                else
                {
                    item.NeedsSync = true;
                    report.NeedsSyncCount++;
                }

                report.Items.Add(item);
            }

            if (settings != null)
            {
                foreach (var group in settings.groups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    foreach (var entry in group.entries)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.AssetPath) || !IsManagedAssetPath(entry.AssetPath))
                        {
                            continue;
                        }

                        if (liveAssetPaths.Contains(entry.AssetPath))
                        {
                            continue;
                        }

                        report.StaleCount++;
                        report.NeedsSyncCount++;
                        report.Items.Add(new ManagedResourceReportItem
                        {
                            AssetPath = entry.AssetPath,
                            ActualGroup = group.Name,
                            ActualAddress = entry.address,
                            HasManagedLabel = HasLabel(entry, ManagedLabel),
                            IsStale = true,
                            NeedsSync = true,
                            Status = "Stale",
                            Message = "This managed Addressables entry no longer matches a valid EFrame managed asset."
                        });
                    }
                }
            }

            report.ResPathCurrent = IsGeneratedResPathCurrent(out var resPathMessage);
            report.ResPathMessage = resPathMessage;
            if (!report.ResPathCurrent)
            {
                report.Issues.Add(resPathMessage);
            }

            report.Items.Sort((left, right) => string.Compare(left.AssetPath, right.AssetPath, StringComparison.OrdinalIgnoreCase));
            return report;
        }

        internal static DirectoryStructureReport BuildDirectoryStructureReport()
        {
            var report = new DirectoryStructureReport();

            var requiredFolders = new[]
            {
                "Assets/App",
                "Assets/App/Editor",
                "Assets/App/Res",
                "Assets/App/Res/Bootstrap",
                "Assets/App/Res/Audios",
                "Assets/App/Res/Config",
                "Assets/App/Res/Fonts",
                "Assets/App/Res/FX",
                "Assets/App/Res/Materials",
                "Assets/App/Res/SceneAssets",
                "Assets/App/Res/Shaders",
                "Assets/App/Res/UI",
                "Assets/App/Res/UI/Common",
                "Assets/App/Res/UI/Panels",
                "Assets/App/Res/UI/Popups",
                "Assets/App/Res/UI/Widgets",
                "Assets/App/Runtime",
                "Assets/App/Runtime/Common",
                "Assets/App/Runtime/Config",
                "Assets/App/Runtime/Data",
                "Assets/App/Runtime/Events",
                "Assets/App/Runtime/Generated",
                "Assets/App/Runtime/Procedure",
                "Assets/App/Runtime/Scene",
                "Assets/App/Runtime/Services",
                "Assets/App/Runtime/UI",
                "Assets/App/Runtime/UI/Controllers",
                "Assets/App/Runtime/UI/Views",
                "Assets/App/Runtime/UI/Widgets",
                "Assets/Modules",
                "Assets/Scenes",
                "Assets/Settings"
            };

            foreach (var folder in requiredFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                report.MissingRequiredCount++;
                report.Issues.Add(new DirectoryStructureIssue("Missing", folder, "Required by package Documentation~/user/UNITY_DIRECTORY_STRUCTURE.md."));
            }

            AddDirectoryRuleIssues(report);
            return report;
        }

        internal static bool ValidateManagedAddressables(out string message)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                message = "Addressables settings are missing. EFrame managed resources cannot be validated.";
                return false;
            }

            var errors = new List<string>();
            var targetSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!TryCollectAddressableManagedEntries(out var generatedEntries, out var collectMessage))
            {
                message = collectMessage;
                return false;
            }

            foreach (var generatedEntry in generatedEntries)
            {
                targetSet.Add(generatedEntry.AssetPath);

                var guid = AssetDatabase.AssetPathToGUID(generatedEntry.AssetPath);
                var entry = string.IsNullOrEmpty(guid) ? null : settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    errors.Add($"{generatedEntry.AssetPath} is not registered as an Addressables entry.");
                    continue;
                }

                var expectedGroup = settings.FindGroup(generatedEntry.GroupName);
                if (entry.parentGroup != expectedGroup)
                {
                    errors.Add($"{generatedEntry.AssetPath} is in group '{entry.parentGroup?.Name ?? "<none>"}' but should be in '{generatedEntry.GroupName}'.");
                }

                if (!string.Equals(entry.address, generatedEntry.Address, StringComparison.Ordinal))
                {
                    errors.Add($"{generatedEntry.AssetPath} has address '{entry.address}' but should be '{generatedEntry.Address}'.");
                }

                if (!HasLabel(entry, ManagedLabel))
                {
                    errors.Add($"{generatedEntry.AssetPath} is missing the '{ManagedLabel}' Addressables label.");
                }
            }

            foreach (var group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var entry in group.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.AssetPath) || !IsManagedAssetPath(entry.AssetPath))
                    {
                        continue;
                    }

                    if (!targetSet.Contains(entry.AssetPath))
                    {
                        errors.Add($"{entry.AssetPath} is a stale managed Addressables entry in group '{group.Name}'.");
                    }
                }
            }

            if (!IsGeneratedResPathCurrent(out var resPathMessage))
            {
                errors.Add($"{resPathMessage} Run EFrame Tools/Addressables/Sync Groups And Generate ResPath.");
            }

            if (errors.Count > 0)
            {
                message = "EFrame managed Addressables validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors);
                return false;
            }

            message = $"EFrame managed Addressables validation passed. Entries checked: {generatedEntries.Count}.";
            return true;
        }

        private static bool SyncManagedAssets(out string message)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                message = "Initialize Addressables before configuring project Addressables groups.";
                return false;
            }

            if (!TryCollectAddressableManagedEntries(out var generatedEntries, out message))
            {
                return false;
            }

            var targetSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var generatedEntry in generatedEntries)
            {
                targetSet.Add(generatedEntry.AssetPath);
            }

            var groupCache = new Dictionary<string, AddressableAssetGroup>(StringComparer.OrdinalIgnoreCase);

            EnsureGroup(settings, AppSharedGroupName, groupCache);
            EnsureGroup(settings, AppBootstrapGroupName, groupCache);
            EnsureGroup(settings, AppSceneAssetsGroupName, groupCache);
            EnsureGroup(settings, AppScenesGroupName, groupCache);

            RemoveStaleManagedEntries(settings, targetSet);

            var registeredCount = 0;
            var touchedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var generatedEntry in generatedEntries)
            {
                var guid = AssetDatabase.AssetPathToGUID(generatedEntry.AssetPath);
                var group = EnsureGroup(settings, generatedEntry.GroupName, groupCache);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null)
                {
                    continue;
                }

                entry.address = generatedEntry.Address;
                entry.SetLabel(ManagedLabel, true, true, false);
                registeredCount++;
                touchedGroups.Add(generatedEntry.GroupName);
            }

            AssetDatabase.SaveAssets();
            var resPathEntries = FilterResPathEntries(generatedEntries);
            if (!GenerateResPathCode(resPathEntries, out var generatedMessage))
            {
                message = generatedMessage;
                return false;
            }

            message = $"Project Addressables groups ready. Groups touched: {touchedGroups.Count}, entries updated: {registeredCount}. {generatedMessage}";
            return true;
        }

        private static List<string> CollectAllManagedAssetPaths()
        {
            var assetPaths = new List<string>();

            foreach (var rootPath in ManagedRootPaths)
            {
                if (!AssetDatabase.IsValidFolder(rootPath))
                {
                    continue;
                }

                var guids = AssetDatabase.FindAssets(string.Empty, new[] { rootPath });
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    assetPaths.Add(assetPath);
                }
            }

            return assetPaths;
        }

        internal static bool GenerateResPathCode(out string message)
        {
            if (!TryCollectResPathAddressEntries(out var entries, out message))
            {
                return false;
            }

            return GenerateResPathCode(entries, out message);
        }

        private static bool TryCollectAddressableManagedEntries(out List<GeneratedAddressEntry> entries, out string message)
        {
            CollectAddressableManagedEntries(true, out entries, out var issues);
            if (issues.Count > 0)
            {
                message = string.Join(Environment.NewLine, issues);
                return false;
            }

            message = $"Collected {entries.Count} EFrame managed Addressables entries.";
            return true;
        }

        private static bool TryCollectResPathAddressEntries(out List<GeneratedAddressEntry> entries, out string message)
        {
            if (!TryCollectAddressableManagedEntries(out var allEntries, out message))
            {
                entries = new List<GeneratedAddressEntry>();
                return false;
            }

            entries = FilterResPathEntries(allEntries);
            if (!TryValidateUniqueAddresses(entries, out message))
            {
                return false;
            }

            message = $"Collected {entries.Count} EFrame ResPath entries.";
            return true;
        }

        private static void CollectAddressableManagedEntries(bool requireUniqueAddresses, out List<GeneratedAddressEntry> entries, out List<string> issues)
        {
            entries = new List<GeneratedAddressEntry>();
            issues = new List<string>();

            foreach (var assetPath in CollectAllManagedAssetPaths())
            {
                if (TryCollectGeneratedAddressEntry(assetPath, out var generatedEntry))
                {
                    entries.Add(generatedEntry);
                }
            }

            if (requireUniqueAddresses)
            {
                if (!TryValidateUniqueAddresses(entries, out var message))
                {
                    issues.Add(message);
                }

                return;
            }

            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (seen.TryGetValue(entry.Address, out var existingAssetPath))
                {
                    issues.Add($"Duplicate generated address '{entry.Address}' for '{existingAssetPath}' and '{entry.AssetPath}'.");
                    continue;
                }

                seen.Add(entry.Address, entry.AssetPath);
            }
        }

        private static bool TryCollectGeneratedAddressEntry(string assetPath, out GeneratedAddressEntry entry)
        {
            entry = default;

            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
            {
                return false;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (mainAssetType == typeof(DefaultAsset) || mainAssetType == typeof(MonoScript))
            {
                return false;
            }

            if (!TryResolveGroupBinding(assetPath, out var binding))
            {
                return false;
            }

            entry = new GeneratedAddressEntry(binding.GroupName, binding.Address, assetPath);
            return true;
        }

        private static bool AreImportedManagedAssetsCurrent(IEnumerable<string> assetPaths, out string message)
        {
            message = string.Empty;
            if (assetPaths == null)
            {
                return false;
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                message = "Addressables settings are missing.";
                return false;
            }

            var checkedCount = 0;
            foreach (var assetPath in assetPaths)
            {
                if (!IsManagedAssetPath(assetPath))
                {
                    continue;
                }

                if (!TryCollectGeneratedAddressEntry(assetPath, out var generatedEntry))
                {
                    continue;
                }

                checkedCount++;
                if (!IsGeneratedEntrySynced(settings, generatedEntry))
                {
                    message = $"{assetPath} is not current in EFrame managed Addressables.";
                    return false;
                }
            }

            if (!IsGeneratedResPathCurrent(out var resPathMessage))
            {
                message = resPathMessage;
                return false;
            }

            message = checkedCount > 0
                ? $"Imported managed assets already current. Entries checked: {checkedCount}. {resPathMessage}"
                : $"No imported managed Addressables entries require sync. {resPathMessage}";
            return true;
        }

        private static bool IsGeneratedEntrySynced(AddressableAssetSettings settings, GeneratedAddressEntry generatedEntry)
        {
            var guid = AssetDatabase.AssetPathToGUID(generatedEntry.AssetPath);
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            var entry = settings.FindAssetEntry(guid);
            var expectedGroup = settings.FindGroup(generatedEntry.GroupName);
            return entry != null
                && expectedGroup != null
                && entry.parentGroup == expectedGroup
                && string.Equals(entry.address, generatedEntry.Address, StringComparison.Ordinal)
                && HasLabel(entry, ManagedLabel);
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string groupName, IDictionary<string, AddressableAssetGroup> groupCache)
        {
            if (groupCache.TryGetValue(groupName, out var cachedGroup) && cachedGroup != null)
            {
                return cachedGroup;
            }

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            groupCache[groupName] = group;
            return group;
        }

        private static void RemoveStaleManagedEntries(AddressableAssetSettings settings, ISet<string> liveAssetPaths)
        {
            foreach (var group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                var staleEntryGuids = new List<string>();
                foreach (var entry in group.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.AssetPath) || !IsManagedAssetPath(entry.AssetPath))
                    {
                        continue;
                    }

                    if (!liveAssetPaths.Contains(entry.AssetPath))
                    {
                        staleEntryGuids.Add(entry.guid);
                    }
                }

                foreach (var guid in staleEntryGuids)
                {
                    settings.RemoveAssetEntry(guid, false);
                }
            }
        }

        private static bool TryResolveGroupBinding(string assetPath, out GroupBinding binding)
        {
            if (assetPath.StartsWith(AppBootstrapRootPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                binding = new GroupBinding(AppBootstrapGroupName, BuildRelativeAddress(assetPath, AppResRootPath));
                return true;
            }

            if (assetPath.StartsWith(AppSceneAssetsRootPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                binding = new GroupBinding(AppSceneAssetsGroupName, BuildRelativeAddress(assetPath, AppResRootPath));
                return true;
            }

            if (assetPath.StartsWith(AppResRootPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                binding = new GroupBinding(AppSharedGroupName, BuildRelativeAddress(assetPath, AppResRootPath));
                return true;
            }

            if (assetPath.StartsWith(ProjectScenesRootPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                binding = new GroupBinding(AppScenesGroupName, BuildRelativeAddress(assetPath, "Assets"));
                return true;
            }

            if (TryResolveModuleBinding(assetPath, out binding))
            {
                return true;
            }

            binding = default;
            return false;
        }

        private static bool TryResolveModuleBinding(string assetPath, out GroupBinding binding)
        {
            binding = default;

            if (!assetPath.StartsWith(ModulesRootPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var normalizedPath = assetPath.Replace('\\', '/');
            var segments = normalizedPath.Split('/');
            if (segments.Length < 4)
            {
                return false;
            }

            var moduleName = segments[2];
            var scope = segments[3];
            if (string.Equals(scope, "Res", StringComparison.OrdinalIgnoreCase))
            {
                binding = new GroupBinding($"Module {moduleName} Assets Group", BuildRelativeAddress(assetPath, "Assets"));
                return true;
            }

            if (string.Equals(scope, "Scenes", StringComparison.OrdinalIgnoreCase))
            {
                binding = new GroupBinding($"Module {moduleName} Scenes Group", BuildRelativeAddress(assetPath, "Assets"));
                return true;
            }

            return false;
        }

        private static bool IsManagedAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            foreach (var rootPath in ManagedRootPaths)
            {
                if (assetPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsManagedAssetChangePath(string assetPath)
        {
            return IsManagedAssetPath(assetPath);
        }

        private static bool IsAssetIncludedInGeneratedResPath(string assetPath)
        {
            var normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(normalizedAssetPath))
            {
                return false;
            }

            var directoryPath = NormalizeAssetPath(Path.GetDirectoryName(normalizedAssetPath));
            return IsIncludedResPathDirectoryOrChild(directoryPath);
        }

        private static bool IsIncludedResPathDirectoryOrChild(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            foreach (var includedDirectory in LoadResPathSelectionSettings().IncludedDirectories)
            {
                if (string.Equals(directoryPath, includedDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (directoryPath.StartsWith(includedDirectory + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ResPathSelectionSettings LoadResPathSelectionSettings()
        {
            var settingsPath = GetProjectAbsolutePath(ResPathSelectionSettingsFilePath);
            if (File.Exists(settingsPath))
            {
                try
                {
                    var settings = JsonUtility.FromJson<ResPathSelectionSettings>(File.ReadAllText(settingsPath));
                    if (settings?.IncludedDirectories != null)
                    {
                        NormalizeIncludedDirectories(settings);
                        return settings;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[EFrame Addressables] Failed to read {ResPathSelectionSettingsFilePath}: {exception.Message}");
                }
            }

            return CreateDefaultResPathSelectionSettings();
        }

        private static void SaveResPathSelectionSettings(ResPathSelectionSettings settings)
        {
            NormalizeIncludedDirectories(settings);
            EnsureFolderExists("Assets/Settings");
            File.WriteAllText(GetProjectAbsolutePath(ResPathSelectionSettingsFilePath), JsonUtility.ToJson(settings, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ResPathSelectionSettingsFilePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
        }

        private static ResPathSelectionSettings CreateDefaultResPathSelectionSettings()
        {
            return new ResPathSelectionSettings
            {
                IncludedDirectories = new List<string>
                {
                    AppBootstrapRootPath,
                    "Assets/App/Res/Config",
                    "Assets/App/Res/FX",
                    "Assets/App/Res/UI/Panels",
                    "Assets/App/Res/UI/Popups",
                    "Assets/App/Res/UI/Widgets",
                    ProjectScenesRootPath,
                    ModulesRootPath
                }
            };
        }

        private static void NormalizeIncludedDirectories(ResPathSelectionSettings settings)
        {
            var normalizedDirectories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var directory in settings.IncludedDirectories ?? new List<string>())
            {
                var normalizedDirectory = NormalizeAssetPath(directory);
                if (!string.IsNullOrEmpty(normalizedDirectory))
                {
                    normalizedDirectories.Add(normalizedDirectory);
                }
            }

            settings.IncludedDirectories = new List<string>(normalizedDirectories);
        }

        private static void AddSelectableDirectoriesUnderRoot(ISet<string> directories, string rootPath)
        {
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                return;
            }

            directories.Add(rootPath);
            foreach (var guid in AssetDatabase.FindAssets("t:DefaultAsset", new[] { rootPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    directories.Add(path);
                }
            }
        }

        private static void AddSelectableModuleScopeDirectories(ISet<string> directories, string scopeRootPath)
        {
            if (!AssetDatabase.IsValidFolder(scopeRootPath))
            {
                return;
            }

            AddSelectableDirectoriesUnderRoot(directories, scopeRootPath);
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim('/');
        }

        private static bool HasLabel(AddressableAssetEntry entry, string label)
        {
            if (entry?.labels == null)
            {
                return false;
            }

            foreach (var entryLabel in entry.labels)
            {
                if (string.Equals(entryLabel, label, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryValidateUniqueAddresses(IReadOnlyList<GeneratedAddressEntry> entries, out string message)
        {
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new List<string>();

            foreach (var entry in entries)
            {
                if (seen.TryGetValue(entry.Address, out var existingAssetPath))
                {
                    duplicates.Add($"Address '{entry.Address}' is shared by '{existingAssetPath}' and '{entry.AssetPath}'.");
                    continue;
                }

                seen.Add(entry.Address, entry.AssetPath);
            }

            if (duplicates.Count > 0)
            {
                message = "EFrame managed Addressables require unique generated addresses:" + Environment.NewLine + string.Join(Environment.NewLine, duplicates);
                return false;
            }

            message = "All generated Addressables addresses are unique.";
            return true;
        }

        private static Dictionary<string, string> BuildResPathMemberLookup(IReadOnlyList<GeneratedAddressEntry> entries)
        {
            var root = new ResPathNode("Root", "Root");
            foreach (var entry in entries)
            {
                AddAddress(root, entry.Address);
            }

            var lookup = new Dictionary<string, string>(StringComparer.Ordinal);
            AppendResPathLookup(lookup, root, "ResPath.Generated", null);
            return lookup;
        }

        private static void AppendResPathLookup(IDictionary<string, string> lookup, ResPathNode node, string prefix, string enclosingTypeIdentifier)
        {
            var usedMemberNames = new HashSet<string>(StringComparer.Ordinal);
            var currentTypeIdentifier = enclosingTypeIdentifier ?? node.Identifier;
            if (!string.IsNullOrEmpty(node.Address))
            {
                usedMemberNames.Add("Value");
                lookup[node.Address] = $"{prefix}.Value";
            }

            foreach (var child in node.Children.Values)
            {
                if (child.Children.Count == 0)
                {
                    var constIdentifier = MakeUniqueMemberIdentifier(child.Identifier, usedMemberNames, currentTypeIdentifier, "Value");
                    lookup[child.Address] = $"{prefix}.{constIdentifier}";
                    continue;
                }

                var classIdentifier = MakeUniqueMemberIdentifier(child.Identifier, usedMemberNames, currentTypeIdentifier, $"{child.Identifier}Node");
                var childPrefix = $"{prefix}.{classIdentifier}";
                if (!string.IsNullOrEmpty(child.Address))
                {
                    lookup[child.Address] = $"{childPrefix}.Value";
                }

                AppendResPathLookup(lookup, child, childPrefix, classIdentifier);
            }
        }

        private static void AddDirectoryRuleIssues(DirectoryStructureReport report)
        {
            AddAssetsUnderPath(report, AppResRootPath, "t:Scene", "Error", "Scene files belong under Assets/Scenes or Assets/Modules/<Name>/Scenes, not Assets/App/Res.");
            AddAssetsUnderPath(report, AppResRootPath, "t:MonoScript", "Warning", "Runtime/editor scripts should live under Runtime or Editor folders, not managed resource folders.");

            AddDirectFilesWarning(report, AppResRootPath, "Assets/App/Res should stay as a category root. Put assets in Bootstrap, UI, FX, Config, SceneAssets, or another documented subfolder.");
            AddDirectFilesWarning(report, "Assets/App/Runtime", "Assets/App/Runtime should stay as a category root. Put scripts in Common, Procedure, UI, Services, Data, Events, Config, Scene, or Generated.");

            if (AssetDatabase.IsValidFolder(ModulesRootPath))
            {
                var moduleGuids = AssetDatabase.FindAssets("t:DefaultAsset", new[] { ModulesRootPath });
                foreach (var guid in moduleGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!AssetDatabase.IsValidFolder(path) || !IsDirectChild(path, ModulesRootPath))
                    {
                        continue;
                    }

                    CheckModuleFolder(report, path);
                }
            }
        }

        private static List<GeneratedAddressEntry> FilterResPathEntries(IEnumerable<GeneratedAddressEntry> entries)
        {
            var resPathEntries = new List<GeneratedAddressEntry>();
            foreach (var entry in entries)
            {
                if (IsAssetIncludedInGeneratedResPath(entry.AssetPath))
                {
                    resPathEntries.Add(entry);
                }
            }

            return resPathEntries;
        }

        private static void CheckModuleFolder(DirectoryStructureReport report, string modulePath)
        {
            var allowedChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Editor",
                "Res",
                "Runtime",
                "Scenes"
            };

            foreach (var child in AssetDatabase.GetSubFolders(modulePath))
            {
                var childName = Path.GetFileName(child);
                if (allowedChildren.Contains(childName))
                {
                    continue;
                }

                report.WarningCount++;
                report.Issues.Add(new DirectoryStructureIssue("Warning", child, "Module root should use Editor, Res, Runtime, and Scenes as top-level folders."));
            }

            if (!AssetDatabase.IsValidFolder($"{modulePath}/Runtime"))
            {
                report.WarningCount++;
                report.Issues.Add(new DirectoryStructureIssue("Warning", $"{modulePath}/Runtime", "Module runtime code should live under Runtime when this module contains code."));
            }

            if (!AssetDatabase.IsValidFolder($"{modulePath}/Res"))
            {
                report.WarningCount++;
                report.Issues.Add(new DirectoryStructureIssue("Warning", $"{modulePath}/Res", "Module private resources should live under Res when this module owns assets."));
            }
        }

        private static void AddAssetsUnderPath(DirectoryStructureReport report, string rootPath, string filter, string severity, string message)
        {
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { rootPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AddDirectoryIssue(report, severity, path, message);
            }
        }

        private static void AddDirectFilesWarning(DirectoryStructureReport report, string rootPath, string message)
        {
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { rootPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) || !IsDirectChild(path, rootPath))
                {
                    continue;
                }

                AddDirectoryIssue(report, "Warning", path, message);
            }
        }

        private static void AddDirectoryIssue(DirectoryStructureReport report, string severity, string path, string message)
        {
            if (string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase))
            {
                report.ErrorCount++;
            }
            else
            {
                report.WarningCount++;
            }

            report.Issues.Add(new DirectoryStructureIssue(severity, path, message));
        }

        private static bool IsDirectChild(string path, string parentPath)
        {
            var normalizedPath = path.Replace('\\', '/').Trim('/');
            var normalizedParent = parentPath.Replace('\\', '/').Trim('/');
            if (!normalizedPath.StartsWith(normalizedParent + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relativePath = normalizedPath.Substring(normalizedParent.Length + 1);
            return !relativePath.Contains("/");
        }

        private static string BuildRelativeAddress(string assetPath, string rootPath)
        {
            var normalizedPath = assetPath.Replace('\\', '/');
            var normalizedRoot = rootPath.Replace('\\', '/');
            var relativePath = normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase)
                ? normalizedPath.Substring(normalizedRoot.Length + 1)
                : Path.GetFileName(normalizedPath);

            var extension = Path.GetExtension(relativePath);
            if (!string.IsNullOrEmpty(extension))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - extension.Length);
            }

            return relativePath.Replace('\\', '/');
        }

        private static void EnsureFolderExists(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private static bool GenerateResPathCode(IReadOnlyList<GeneratedAddressEntry> entries, out string message)
        {
            try
            {
                EnsureFolderExists("Assets/App/Runtime");
                EnsureFolderExists("Assets/App/Runtime/Generated");
                EnsureFolderExists("Assets/App/Runtime/Generated/Res");

                var fileContent = BuildGeneratedResPathContent(GeneratedResPathNamespace, entries);
                var generatedResPath = GetProjectAbsolutePath(GeneratedResPathFilePath);
                if (File.Exists(generatedResPath) && string.Equals(File.ReadAllText(generatedResPath), fileContent, StringComparison.Ordinal))
                {
                    message = $"{GeneratedResPathFilePath} is current.";
                    return true;
                }

                File.WriteAllText(generatedResPath, fileContent, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(GeneratedResPathFilePath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();
                message = $"Generated ResPath code at {GeneratedResPathFilePath}.";
                return true;
            }
            catch (Exception exception)
            {
                message = $"Failed to generate ResPath code: {exception.Message}";
                return false;
            }
        }

        private static string BuildGeneratedResPathContent(string namespaceName, IReadOnlyList<GeneratedAddressEntry> entries)
        {
            var root = new ResPathNode("Root", "Root");
            foreach (var entry in entries)
            {
                AddAddress(root, entry.Address);
            }

            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Generated by EFrameAddressablesBootstrapUtility. Do not edit manually.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceName}");
            builder.AppendLine("{");
            builder.AppendLine("    public static partial class ResPath");
            builder.AppendLine("    {");
            builder.AppendLine("        public static class Generated");
            builder.AppendLine("        {");

            if (root.Children.Count == 0)
            {
                builder.AppendLine("            // No managed Addressables entries were found.");
            }
            else
            {
                AppendNodeMembers(builder, root, 3);
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AddAddress(ResPathNode root, string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            var current = root;
            foreach (var segment in address.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                current = GetOrCreateChild(current, segment);
            }

            current.Address = address;
        }

        private static ResPathNode GetOrCreateChild(ResPathNode parent, string segment)
        {
            var baseIdentifier = ToCodeIdentifier(segment);
            var candidate = baseIdentifier;
            var suffix = 2;

            while (parent.Children.TryGetValue(candidate, out var existing))
            {
                if (string.Equals(existing.OriginalName, segment, StringComparison.OrdinalIgnoreCase))
                {
                    return existing;
                }

                candidate = $"{baseIdentifier}{suffix}";
                suffix++;
            }

            var node = new ResPathNode(segment, candidate);
            parent.Children[candidate] = node;
            return node;
        }

        private static void AppendNodeMembers(StringBuilder builder, ResPathNode node, int indentLevel, string enclosingTypeIdentifier = null)
        {
            var usedMemberNames = new HashSet<string>(StringComparer.Ordinal);
            var currentTypeIdentifier = enclosingTypeIdentifier ?? node.Identifier;
            if (!string.IsNullOrEmpty(node.Address))
            {
                usedMemberNames.Add("Value");
            }

            foreach (var child in node.Children.Values)
            {
                var indent = new string(' ', indentLevel * 4);
                if (child.Children.Count == 0)
                {
                    var constIdentifier = MakeUniqueMemberIdentifier(child.Identifier, usedMemberNames, currentTypeIdentifier, "Value");
                    builder.AppendLine($"{indent}public const string {constIdentifier} = \"{EscapeStringLiteral(child.Address)}\";");
                    continue;
                }

                var classIdentifier = MakeUniqueMemberIdentifier(child.Identifier, usedMemberNames, currentTypeIdentifier, $"{child.Identifier}Node");
                builder.AppendLine($"{indent}public static class {classIdentifier}");
                builder.AppendLine($"{indent}{{");
                if (!string.IsNullOrEmpty(child.Address))
                {
                    builder.AppendLine($"{indent}    public const string Value = \"{EscapeStringLiteral(child.Address)}\";");
                }

                AppendNodeMembers(builder, child, indentLevel + 1, classIdentifier);
                builder.AppendLine($"{indent}}}");
            }
        }

        private static string MakeUniqueMemberIdentifier(string identifier, ISet<string> usedMemberNames, string enclosingTypeIdentifier, string conflictFallback)
        {
            var baseIdentifier = identifier;
            if (string.Equals(baseIdentifier, enclosingTypeIdentifier, StringComparison.Ordinal))
            {
                baseIdentifier = string.IsNullOrEmpty(conflictFallback) ? $"{identifier}Item" : conflictFallback;
            }

            if (string.IsNullOrEmpty(baseIdentifier))
            {
                baseIdentifier = "Item";
            }

            var candidate = baseIdentifier;
            var suffix = 2;
            while (usedMemberNames.Contains(candidate) || string.Equals(candidate, enclosingTypeIdentifier, StringComparison.Ordinal))
            {
                candidate = $"{baseIdentifier}{suffix}";
                suffix++;
            }

            usedMemberNames.Add(candidate);
            return candidate;
        }

        private static string ToCodeIdentifier(string rawSegment)
        {
            var parts = Regex.Split(rawSegment ?? string.Empty, "[^A-Za-z0-9]+");
            var builder = new StringBuilder();

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                if (part.Length <= 3 && string.Equals(part, part.ToUpperInvariant(), StringComparison.Ordinal))
                {
                    builder.Append(part);
                    continue;
                }

                builder.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    builder.Append(part.Substring(1));
                }
            }

            var identifier = builder.Length == 0 ? "Item" : builder.ToString();
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
            {
                identifier = $"_{identifier}";
            }

            if (CSharpKeywords.Contains(identifier))
            {
                identifier = $"_{identifier}";
            }

            return identifier;
        }

        private static string EscapeStringLiteral(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string GetProjectAbsolutePath(string assetRelativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

    }
}
#endif
