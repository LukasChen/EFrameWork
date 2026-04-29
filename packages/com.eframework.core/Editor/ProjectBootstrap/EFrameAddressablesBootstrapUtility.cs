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

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal static class EFrameAddressablesBootstrapUtility
    {
        internal const string AppResRootPath = "Assets/App/Res";
        internal const string AppBootstrapRootPath = "Assets/App/Res/Bootstrap";
        internal const string AppSceneAssetsRootPath = "Assets/App/Res/SceneAssets";
        internal const string ProjectScenesRootPath = "Assets/Scenes";
        internal const string ModulesRootPath = "Assets/Modules";
        internal const string GeneratedResPathFilePath = "Assets/App/Runtime/Generated/Res/ResPath.Generated.cs";
        internal const string HandwrittenResPathFilePath = "Assets/App/Runtime/Common/ResPath.cs";

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

        private const string ManagedLabel = "eframe-managed";
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
            return SyncManagedAssets(out message);
        }

        private static bool SyncManagedAssets(out string message)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                message = "Initialize Addressables before configuring project Addressables groups.";
                return false;
            }

            var targetPaths = CollectAllManagedAssetPaths();
            var targetSet = new HashSet<string>(targetPaths, StringComparer.OrdinalIgnoreCase);
            var groupCache = new Dictionary<string, AddressableAssetGroup>(StringComparer.OrdinalIgnoreCase);

            EnsureGroup(settings, AppSharedGroupName, groupCache);
            EnsureGroup(settings, AppBootstrapGroupName, groupCache);
            EnsureGroup(settings, AppSceneAssetsGroupName, groupCache);
            EnsureGroup(settings, AppScenesGroupName, groupCache);

            RemoveStaleManagedEntries(settings, targetSet);

            var registeredCount = 0;
            var touchedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var generatedEntries = new List<GeneratedAddressEntry>();

            foreach (var assetPath in targetPaths)
            {
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (mainAssetType == typeof(DefaultAsset) || mainAssetType == typeof(MonoScript))
                {
                    continue;
                }

                if (!TryResolveGroupBinding(assetPath, out var binding))
                {
                    continue;
                }

                var group = EnsureGroup(settings, binding.GroupName, groupCache);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null)
                {
                    continue;
                }

                entry.address = binding.Address;
                entry.SetLabel(ManagedLabel, true, true, false);
                registeredCount++;
                touchedGroups.Add(binding.GroupName);
                generatedEntries.Add(new GeneratedAddressEntry(binding.GroupName, binding.Address, assetPath));
            }

            AssetDatabase.SaveAssets();
            if (!GenerateResPathCode(generatedEntries, out var generatedMessage))
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
            var entries = new List<GeneratedAddressEntry>();
            foreach (var assetPath in CollectAllManagedAssetPaths())
            {
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                if (TryResolveGroupBinding(assetPath, out var binding))
                {
                    entries.Add(new GeneratedAddressEntry(binding.GroupName, binding.Address, assetPath));
                }
            }

            return GenerateResPathCode(entries, out message);
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
            foreach (var rootPath in ManagedRootPaths)
            {
                if (assetPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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

                var fileContent = BuildGeneratedResPathContent(ResolveResPathNamespace(), entries);
                File.WriteAllText(GetProjectAbsolutePath(GeneratedResPathFilePath), fileContent, new UTF8Encoding(false));
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

        private static string ResolveResPathNamespace()
        {
            var handwrittenFilePath = GetProjectAbsolutePath(HandwrittenResPathFilePath);
            if (File.Exists(handwrittenFilePath))
            {
                var content = File.ReadAllText(handwrittenFilePath);
                var match = Regex.Match(content, @"namespace\s+([A-Za-z0-9_\.]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            var projectName = Directory.GetParent(Application.dataPath)?.Name ?? "GameApp";
            return $"{ToNamespaceSegment(projectName)}.Common";
        }

        private static string ToNamespaceSegment(string rawValue)
        {
            var sanitized = Regex.Replace(rawValue ?? string.Empty, "[^A-Za-z0-9_]", string.Empty);
            if (string.IsNullOrEmpty(sanitized))
            {
                return "GameApp";
            }

            if (!char.IsLetter(sanitized[0]) && sanitized[0] != '_')
            {
                sanitized = $"_{sanitized}";
            }

            return sanitized;
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
