#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EFramework.Runtime.Procedure;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace EFramework.Editor.ProjectBootstrap
{
    public sealed class EFrameProjectInitializationWindow : EditorWindow
    {
        private const string WindowTitle = "EFrame Project Init";
        private const string PackageName = "com.eframework.core";
        private const string AppNamespace = "GameApp";
        private const string LogoAssetPath = "Packages/com.eframework.core/Editor/ProjectBootstrap/Assets/EFrameLogo.png";
        private const string StartUpScenePath = "Assets/Scenes/StartUp.unity";
        private const string BasicTemplatePath = "Packages/com.eframework.core/Editor/Templates/Basic/Assets";
        private const string ExtensionShowcaseTargetPath = "Assets/Modules/EFrameExtensionShowcase";
        private const string ExtensionShowcaseTemplatePath = "Packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase";
        private const string BasicStartupProcedureName = "GameApp.Procedure.ProcedureLauncher";
        private const string ExtensionShowcaseStartupProcedureName = "GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry";
        private const string AutoPopupSessionKey = "EFrame.ProjectInitializationWindow.AutoPopupShown";
        private const string AllAiClientsArgument = "all";
        private const string AllAiClientsLabel = "Codex, GitHub Copilot, and Claude Code";

        private static readonly string[] ExtensionPackageNames =
        {
            "com.eframework.ui.virtual-list",
            "com.eframework.ui-extras",
            "com.eframework.effects",
            "com.eframework.gm-tools",
            "com.eframework.debug-console"
        };

        private static readonly Queue<Action> PendingEditorActions = new();
        private static readonly object PendingEditorActionsLock = new();
        private static bool s_pendingEditorActionProcessorRegistered;

        private string m_statusMessage;
        private string m_aiCheckMessage;
        private bool m_statusIsError;
        private bool m_aiCheckIsError;
        private bool m_aiChecksRunning;
        private Texture2D m_logoTexture;

        [MenuItem("EFrame Tools/项目初始化向导", false, -100)]
        public static void OpenWindow()
        {
            var window = GetWindow<EFrameProjectInitializationWindow>(true, WindowTitle);
            window.minSize = new Vector2(520f, 300f);
            window.InitializeState();
            window.Show();
        }

        private void OnEnable()
        {
            InitializeState();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                if (GUILayout.Button("Initialize / Repair Project", GUILayout.Height(34f)))
                {
                    RunFullInitialization();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Tween Backend", EditorStyles.boldLabel);
                DrawTweenBackendControls();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("AI Workspace", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sync / Repair AI Workspace"))
                    {
                        RunAiSync();
                    }

                    using (new EditorGUI.DisabledScope(m_aiChecksRunning))
                    {
                        if (GUILayout.Button(m_aiChecksRunning ? "Checking..." : "Run AI Checks"))
                        {
                            RunAiChecks();
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(m_aiCheckMessage))
                {
                    EditorGUILayout.HelpBox(m_aiCheckMessage, m_aiCheckIsError ? MessageType.Error : MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Samples And Optional Modules", EditorStyles.boldLabel);

                if (GUILayout.Button("Install Showcase", GUILayout.Height(28f)))
                {
                    InstallExtensionShowcaseModule();
                }
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(m_statusMessage))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(m_statusMessage, m_statusIsError ? MessageType.Error : MessageType.Info);
            }
        }

        private void InitializeState()
        {
        }

        private static string ProjectRootPath => Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        private void DrawHeader()
        {
            m_logoTexture ??= AssetDatabase.LoadAssetAtPath<Texture2D>(LogoAssetPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(180f)))
                {
                    if (m_logoTexture != null)
                    {
                        var rect = GUILayoutUtility.GetRect(76f, 76f, GUILayout.Width(76f), GUILayout.Height(76f));
                        rect.x += 52f;
                        GUI.DrawTexture(rect, m_logoTexture, ScaleMode.ScaleToFit, true);
                    }

                    var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 14
                    };
                    EditorGUILayout.LabelField("EFrame", titleStyle);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawTweenBackendControls()
        {
            var dotweenInstalled = EFrameDotweenBootstrapUtility.IsDotweenInstalled();
            var adapterEnabled = EFrameDotweenBootstrapUtility.IsDotweenAdapterEnabled();

            string status;
            string buttonLabel;
            bool buttonEnabled;
            Action buttonAction;

            if (adapterEnabled && dotweenInstalled)
            {
                status = "DOTween Adapter 已启用";
                buttonLabel = "已启用";
                buttonEnabled = false;
                buttonAction = null;
            }
            else if (adapterEnabled)
            {
                status = "DOTween Adapter 已启用，但未检测到 DOTween";
                buttonLabel = "禁用失效 Adapter";
                buttonEnabled = true;
                buttonAction = () => DisableDotweenAdapter();
            }
            else if (dotweenInstalled)
            {
                status = "DOTween detected. Adapter not enabled.";
                buttonLabel = "启用 DOTween Adapter";
                buttonEnabled = true;
                buttonAction = () => EnableDotweenAdapter();
            }
            else
            {
                status = "Fallback backend active. DOTween not detected.";
                buttonLabel = "未检测到 DOTween";
                buttonEnabled = false;
                buttonAction = null;
            }

            EditorGUILayout.LabelField("Adapter", status);
            using (new EditorGUI.DisabledScope(!buttonEnabled))
            {
                if (GUILayout.Button(buttonLabel))
                {
                    buttonAction?.Invoke();
                }
            }
        }

        private void RunFullInitialization()
        {
            if (IsAiSyncAvailable(out _, out _))
            {
                if (!RunToolScript("Initialize-EFrameColdStart.ps1", $"-TargetRoot \"{ProjectRootPath}\" -Force"))
                {
                    return;
                }
            }
            else
            {
                if (!CopyBasicTemplate(true, out var copyMessage))
                {
                    SetStatus(copyMessage, true);
                    return;
                }

                SetStatus($"Framework root was not detected as a local repo. {copyMessage}", false);
            }

            if (!EnsureAddressablesInitialized())
            {
                return;
            }

            if (!EnsureUISortingLayers())
            {
                return;
            }

            if (!EnsureAudioSetup())
            {
                return;
            }

            EnsureOptionalDotweenSetup();

            if (!SyncAddressablesAfterAssetChanges("Initialized or repaired Basic template."))
            {
                return;
            }

            EnsureStartUpSceneInBuildSettings(StartUpScenePath);
            AssetDatabase.Refresh();
            SetStatus($"Initialized or repaired Basic template and registered {StartUpScenePath}.", false);
        }

        private static void ConfigureProcedureComponent(EFrameProcedureComponent procedureComponent, bool includeExtensionShowcase, string entranceProcedureTypeName)
        {
            var procedureObject = new SerializedObject(procedureComponent);
            var availableProcedures = procedureObject.FindProperty("m_availableProcedureTypeNames");
            var entranceProcedure = procedureObject.FindProperty("m_entranceProcedureTypeName");

            var procedureTypeNames = new List<string>
            {
                $"{AppNamespace}.Procedure.ProcedureLauncher",
                $"{AppNamespace}.Procedure.ProcedureHome"
            };

            if (includeExtensionShowcase)
            {
                procedureTypeNames.Add(ExtensionShowcaseStartupProcedureName);
            }

            availableProcedures.arraySize = procedureTypeNames.Count;
            for (var index = 0; index < procedureTypeNames.Count; index++)
            {
                availableProcedures.GetArrayElementAtIndex(index).stringValue = procedureTypeNames[index];
            }

            entranceProcedure.stringValue = string.IsNullOrWhiteSpace(entranceProcedureTypeName)
                ? BasicStartupProcedureName
                : entranceProcedureTypeName;

            procedureObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureStartUpSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene => scene.path == scenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private bool EnsureAddressablesInitialized()
        {
            var success = EFrameAddressablesBootstrapUtility.EnsureAddressablesInitialized(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool EnsureAudioSetup()
        {
            var success = EFrameAudioBootstrapUtility.EnsureAudioSetup(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool EnsureOptionalDotweenSetup()
        {
            var success = EFrameDotweenBootstrapUtility.EnsureOptionalDotweenSetup(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool EnableDotweenAdapter()
        {
            var success = EFrameDotweenBootstrapUtility.EnableDotweenAdapter(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool DisableDotweenAdapter()
        {
            var success = EFrameDotweenBootstrapUtility.DisableDotweenAdapter(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool EnsureUISortingLayers()
        {
            var success = EFrameUIBootstrapUtility.EnsureUISortingLayers(out var message);
            SetStatus(message, !success);
            return success;
        }

        private void InstallExtensionShowcaseModule()
        {
            if (!CopyExtensionShowcaseTemplate(out var copyMessage))
            {
                SetStatus(copyMessage, true);
                return;
            }

            AssetDatabase.Refresh();
            var syncSucceeded = SyncAddressablesAfterAssetChanges(copyMessage);
            if (!syncSucceeded)
            {
                return;
            }

            if (!TrySetExtensionShowcaseAsStartup(out var startupMessage))
            {
                return;
            }

            TryInstallLocalExtensionPackages(out var packageMessage);
            SetStatus($"{copyMessage} {startupMessage} {packageMessage}".Trim(), false);
        }

        private void RefreshExtensionShowcaseModuleFromTemplate()
        {
            var targetFullPath = Path.Combine(ProjectRootPath, ExtensionShowcaseTargetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!AssetDatabase.IsValidFolder(ExtensionShowcaseTargetPath) && !Directory.Exists(targetFullPath))
            {
                SetStatus($"Extension Showcase module is not installed at {ExtensionShowcaseTargetPath}. Install it first.", true);
                return;
            }

            var shouldRefresh = EditorUtility.DisplayDialog(
                "Refresh Extension Showcase",
                $"This will replace {ExtensionShowcaseTargetPath} with the current package template. Local changes inside that module will be lost.",
                "Refresh",
                "Cancel");

            if (!shouldRefresh)
            {
                SetStatus("Extension Showcase refresh was cancelled.", false);
                return;
            }

            if (!DeleteExtensionShowcaseTarget(out var deleteMessage))
            {
                SetStatus(deleteMessage, true);
                return;
            }

            if (!CopyExtensionShowcaseTemplate(out var copyMessage))
            {
                SetStatus(copyMessage, true);
                return;
            }

            AssetDatabase.Refresh();
            if (!SyncAddressablesAfterAssetChanges("Refreshed Extension Showcase module from template."))
            {
                return;
            }

            SetStatus($"{copyMessage} Replaced the existing module from the current template.", false);
        }

        private void SetExtensionShowcaseAsStartup()
        {
            if (TrySetExtensionShowcaseAsStartup(out var message))
            {
                SetStatus(message, false);
            }
        }

        private void RestoreBasicStartup()
        {
            if (ConfigureStartUpProcedure(BasicStartupProcedureName, false, out var message))
            {
                SetStatus(message, false);
            }
        }

        private bool TrySetExtensionShowcaseAsStartup(out string message)
        {
            if (!AssetDatabase.IsValidFolder(ExtensionShowcaseTargetPath))
            {
                message = $"Extension Showcase module is not installed at {ExtensionShowcaseTargetPath}.";
                SetStatus(message, true);
                return false;
            }

            return ConfigureStartUpProcedure(ExtensionShowcaseStartupProcedureName, true, out message);
        }

        [MenuItem("EFrame Tools/AI/Check Sync Status", false, 40)]
        private static void MenuRunAiSyncStatus()
        {
            OpenWindow();
            GetWindow<EFrameProjectInitializationWindow>(true, WindowTitle).RunAiSyncStatus();
        }

        [MenuItem("EFrame Tools/AI/Sync Workspace", false, 41)]
        private static void MenuRunAiSync()
        {
            OpenWindow();
            GetWindow<EFrameProjectInitializationWindow>(true, WindowTitle).RunAiSync();
        }

        [MenuItem("EFrame Tools/AI/Run Health Check", false, 42)]
        private static void MenuRunAiHealthCheck()
        {
            OpenWindow();
            GetWindow<EFrameProjectInitializationWindow>(true, WindowTitle).RunAiHealthCheck();
        }

        [MenuItem("EFrame Tools/AI/Run Unity Compile Check", false, 43)]
        private static void MenuRunUnityCompileCheck()
        {
            OpenWindow();
            GetWindow<EFrameProjectInitializationWindow>(true, WindowTitle).RunUnityCompileCheck();
        }

        private void RunAiSyncStatus()
        {
            RunToolScript("Initialize-EFrameAI.ps1", $"-TargetRoot \"{ProjectRootPath}\" -Clients {AllAiClientsArgument} -StatusOnly");
        }

        private void RunAiSync()
        {
            if (!EditorUtility.DisplayDialog(
                    "Sync EFrame AI Workspace",
                    $"This will sync framework-managed AI files for {AllAiClientsLabel}, while preserving project-owned instruction text outside EFrame managed blocks.",
                    "Sync",
                    "Cancel"))
            {
                return;
            }

            m_aiCheckMessage = string.Empty;
            if (!RunToolScript("Initialize-EFrameAI.ps1", $"-TargetRoot \"{ProjectRootPath}\" -Clients {AllAiClientsArgument} -Force"))
            {
                return;
            }

            if (TryGetFrameworkRoot(out var frameworkRoot, out _))
            {
                RunToolScript("Install-EFrameAIProjectUpdater.ps1", $"-TargetRoot \"{ProjectRootPath}\" -FrameworkRoot \"{frameworkRoot}\" -Force");
            }

        }

        private void RunAiChecks()
        {
            if (m_aiChecksRunning)
            {
                return;
            }

            if (!TryGetFrameworkRoot(out var frameworkRoot, out var error))
            {
                var failedResult = ToolScriptResult.Failed("Test-EFrameAIProject.ps1", error);
                m_aiCheckIsError = true;
                m_aiCheckMessage = BuildAiCheckMessage(false, failedResult, false, failedResult);
                SetStatus("AI checks completed with issues.", true);
                return;
            }

            m_aiChecksRunning = true;
            m_aiCheckIsError = false;
            m_aiCheckMessage = "AI checks running...";
            SetStatus("AI checks running...", false);

            var projectRoot = ProjectRootPath;
            var shell = GetPowerShellExecutable();
            EnsurePendingEditorActionProcessor();
            _ = Task.Run(() =>
            {
                var syncSucceeded = false;
                var healthSucceeded = false;
                var syncResult = ToolScriptResult.Failed("Initialize-EFrameAI.ps1", "AI sync check did not run.");
                var healthResult = ToolScriptResult.Failed("Test-EFrameAIProject.ps1", "AI health check did not run.");

                try
                {
                    syncSucceeded = RunToolScriptBackground(
                        frameworkRoot,
                        shell,
                        "Initialize-EFrameAI.ps1",
                        $"-TargetRoot \"{projectRoot}\" -Clients {AllAiClientsArgument} -StatusOnly",
                        out syncResult);

                    healthSucceeded = RunToolScriptBackground(
                        frameworkRoot,
                        shell,
                        "Test-EFrameAIProject.ps1",
                        $"-TargetRoot \"{projectRoot}\" -FrameworkRoot \"{frameworkRoot}\"",
                        out healthResult);
                }
                catch (Exception exception)
                {
                    syncResult = ToolScriptResult.Failed("Run AI Checks", exception.Message);
                    healthResult = ToolScriptResult.Failed("Run AI Checks", exception.Message);
                }

                EnqueueEditorAction(() =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    LogToolScriptResult(syncResult);
                    LogToolScriptResult(healthResult);

                    m_aiChecksRunning = false;
                    m_aiCheckIsError = !syncSucceeded || !healthSucceeded;
                    m_aiCheckMessage = BuildAiCheckMessage(syncSucceeded, syncResult, healthSucceeded, healthResult);
                    SetStatus(m_aiCheckIsError ? "AI checks completed with issues." : "AI checks passed.", m_aiCheckIsError);
                });
            });
        }

        private void RunAiHealthCheck()
        {
            if (!TryGetFrameworkRoot(out var frameworkRoot, out var error))
            {
                SetStatus(error, true);
                return;
            }

            RunToolScript("Test-EFrameAIProject.ps1", $"-TargetRoot \"{ProjectRootPath}\" -FrameworkRoot \"{frameworkRoot}\"");
        }

        private void RunUnityCompileCheck()
        {
            RunToolScript("Test-EFrameUnityCompile.ps1", $"-TargetRoot \"{ProjectRootPath}\"");
        }

        private static string BuildAiCheckMessage(bool syncSucceeded, ToolScriptResult syncResult, bool healthSucceeded, ToolScriptResult healthResult)
        {
            var lines = new List<string>
            {
                syncSucceeded
                    ? "AI sync: OK"
                    : $"AI sync: FAIL - {ExtractToolSummary(syncResult)}",
                healthSucceeded
                    ? "AI health: OK"
                    : $"AI health: FAIL - {ExtractToolSummary(healthResult)}"
            };

            return string.Join(Environment.NewLine, lines);
        }

        private static string ExtractToolSummary(ToolScriptResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                return FirstMeaningfulLine(result.StandardError);
            }

            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return FirstMeaningfulLine(result.StandardOutput);
            }

            if (!string.IsNullOrWhiteSpace(result.StatusMessage))
            {
                return result.StatusMessage;
            }

            return result.ExitCode == 0 ? "Completed." : $"Exit code {result.ExitCode}.";
        }

        private static string FirstMeaningfulLine(string value)
        {
            using var reader = new StringReader(value);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return line.Trim();
                }
            }

            return value.Trim();
        }

        private bool CopyBasicTemplate(bool overwrite, out string message)
        {
            var sourceFullPath = AssetPathToFullPath(BasicTemplatePath);
            if (!Directory.Exists(sourceFullPath))
            {
                message = $"Basic template was not found: {BasicTemplatePath}.";
                return false;
            }

            var targetFullPath = Path.Combine(ProjectRootPath, "Assets");
            CopyDirectory(sourceFullPath, targetFullPath, overwrite);
            AssetDatabase.Refresh();
            message = overwrite
                ? "Repaired Basic template from the package template."
                : "Imported missing Basic template files from the package template.";
            return true;
        }

        private bool CopyExtensionShowcaseTemplate(out string message)
        {
            if (AssetDatabase.IsValidFolder(ExtensionShowcaseTargetPath) || Directory.Exists(Path.Combine(ProjectRootPath, ExtensionShowcaseTargetPath)))
            {
                message = $"Extension Showcase module already exists at {ExtensionShowcaseTargetPath}.";
                return true;
            }

            var sourceFullPath = AssetPathToFullPath(ExtensionShowcaseTemplatePath);
            if (!Directory.Exists(sourceFullPath))
            {
                message = $"Extension Showcase template was not found: {ExtensionShowcaseTemplatePath}.";
                return false;
            }

            var targetFullPath = Path.Combine(ProjectRootPath, ExtensionShowcaseTargetPath.Replace('/', Path.DirectorySeparatorChar));
            CopyDirectory(sourceFullPath, targetFullPath);
            message = $"Installed Extension Showcase module at {ExtensionShowcaseTargetPath}.";
            return true;
        }

        private static bool DeleteExtensionShowcaseTarget(out string message)
        {
            try
            {
                if (AssetDatabase.IsValidFolder(ExtensionShowcaseTargetPath))
                {
                    if (!AssetDatabase.DeleteAsset(ExtensionShowcaseTargetPath))
                    {
                        message = $"Failed to delete existing Extension Showcase module at {ExtensionShowcaseTargetPath}.";
                        return false;
                    }
                }
                else
                {
                    var targetFullPath = Path.Combine(ProjectRootPath, ExtensionShowcaseTargetPath.Replace('/', Path.DirectorySeparatorChar));
                    if (Directory.Exists(targetFullPath))
                    {
                        Directory.Delete(targetFullPath, true);
                    }

                    var metaPath = $"{targetFullPath}.meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }

                message = $"Deleted existing Extension Showcase module at {ExtensionShowcaseTargetPath}.";
                return true;
            }
            catch (Exception exception)
            {
                message = $"Failed to delete existing Extension Showcase module: {exception.Message}";
                return false;
            }
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            if (assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var relativePath = assetPath.Substring("Packages/".Length);
                var separatorIndex = relativePath.IndexOf('/');
                if (separatorIndex > 0)
                {
                    var packageName = relativePath.Substring(0, separatorIndex);
                    var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{packageName}");
                    if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
                    {
                        var packageRelativePath = relativePath.Substring(separatorIndex + 1).Replace('/', Path.DirectorySeparatorChar);
                        return Path.Combine(packageInfo.resolvedPath, packageRelativePath);
                    }
                }
            }

            return Path.Combine(ProjectRootPath, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory, bool overwrite = false)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (var sourceFile in Directory.GetFiles(sourceDirectory))
            {
                var fileName = Path.GetFileName(sourceFile);
                if (fileName.EndsWith(".cs.txt.meta", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - ".txt.meta".Length) + ".meta";
                }
                else if (fileName.EndsWith(".cs.txt", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - ".txt".Length);
                }

                var targetFile = Path.Combine(targetDirectory, fileName);
                if (File.Exists(targetFile) && !overwrite)
                {
                    continue;
                }

                File.Copy(sourceFile, targetFile, overwrite);
            }

            foreach (var sourceChildDirectory in Directory.GetDirectories(sourceDirectory))
            {
                var targetChildDirectory = Path.Combine(targetDirectory, Path.GetFileName(sourceChildDirectory));
                CopyDirectory(sourceChildDirectory, targetChildDirectory, overwrite);
            }
        }

        private bool TryInstallLocalExtensionPackages(out string message)
        {
            if (!TryGetFrameworkRoot(out var frameworkRoot, out var error))
            {
                message = $"{error} Extension packages were not installed automatically.";
                return false;
            }

            if (!TryResolveLocalExtensionPackageRoot(frameworkRoot, out var packageParent))
            {
                message = "Could not resolve local sibling extension packages. Install them manually from git or UPM if this is not a local framework checkout.";
                return false;
            }

            var added = 0;
            var missing = new List<string>();
            var alreadyInstalled = 0;
            var dependenciesToAdd = new Dictionary<string, string>();
            foreach (var packageName in ExtensionPackageNames)
            {
                if (IsPackageDependencyPresent(packageName))
                {
                    alreadyInstalled++;
                    continue;
                }

                var siblingPath = Path.Combine(packageParent, packageName);
                if (!Directory.Exists(siblingPath))
                {
                    missing.Add(packageName);
                    continue;
                }

                dependenciesToAdd[packageName] = BuildLocalPackageSpec(siblingPath);
            }

            if (dependenciesToAdd.Count > 0)
            {
                if (!TryAddDependenciesToProjectManifest(dependenciesToAdd, out added, out var manifestMessage))
                {
                    message = manifestMessage;
                    return false;
                }

                Client.Resolve();
            }

            message = BuildExtensionPackageInstallMessage(added, alreadyInstalled, missing);
            return added > 0;
        }

        private static string BuildExtensionPackageInstallMessage(int added, int alreadyInstalled, List<string> missing)
        {
            var parts = new List<string>();
            if (added > 0)
            {
                parts.Add($"Added {added} local extension package {FormatDependencyNoun(added)} to Packages/manifest.json.");
            }
            else
            {
                parts.Add(alreadyInstalled > 0
                    ? "All available local extension package dependencies were already present."
                    : "No local extension package dependency was added.");
            }

            if (missing.Count > 0)
            {
                parts.Add($"Missing sibling packages: {string.Join(", ", missing)}. Install them manually from git or UPM if this is not a local framework checkout.");
            }

            return string.Join(" ", parts);
        }

        private static bool TryResolveLocalExtensionPackageRoot(string frameworkRoot, out string packageParent)
        {
            packageParent = string.Empty;

            var current = new DirectoryInfo(frameworkRoot);
            while (current != null)
            {
                if (IsExtensionPackageRoot(current.FullName))
                {
                    packageParent = current.FullName;
                    return true;
                }

                var packagesPath = Path.Combine(current.FullName, "packages");
                if (IsExtensionPackageRoot(packagesPath))
                {
                    packageParent = packagesPath;
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        private static bool IsExtensionPackageRoot(string packageParent)
        {
            if (string.IsNullOrEmpty(packageParent) || !Directory.Exists(packageParent))
            {
                return false;
            }

            foreach (var packageName in ExtensionPackageNames)
            {
                if (Directory.Exists(Path.Combine(packageParent, packageName)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPackageDependencyPresent(string packageName)
        {
            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{packageName}") != null)
            {
                return true;
            }

            var manifestPath = GetProjectManifestPath();
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            var manifest = File.ReadAllText(manifestPath);
            if (!TryFindDependenciesObject(manifest, out var dependenciesStart, out var dependenciesEnd))
            {
                return false;
            }

            var dependencies = manifest.Substring(dependenciesStart, dependenciesEnd - dependenciesStart + 1);
            return Regex.IsMatch(dependencies, $"\"{Regex.Escape(packageName)}\"\\s*:", RegexOptions.CultureInvariant);
        }

        private static string BuildLocalPackageSpec(string packagePath)
        {
            var manifestDirectory = Path.GetDirectoryName(GetProjectManifestPath());
            var specPath = string.IsNullOrEmpty(manifestDirectory)
                ? packagePath
                : Path.GetRelativePath(manifestDirectory, packagePath);

            return $"file:{specPath.Replace('\\', '/')}";
        }

        private static bool TryAddDependenciesToProjectManifest(IReadOnlyDictionary<string, string> dependenciesToAdd, out int added, out string message)
        {
            added = 0;
            var manifestPath = GetProjectManifestPath();
            if (!File.Exists(manifestPath))
            {
                message = $"Project manifest was not found at {manifestPath}. Extension packages were not installed automatically.";
                return false;
            }

            var manifest = File.ReadAllText(manifestPath);
            if (!TryFindDependenciesObject(manifest, out var dependenciesStart, out var dependenciesEnd))
            {
                message = "Project manifest does not contain a dependencies object. Extension packages were not installed automatically.";
                return false;
            }

            var insertIndex = dependenciesEnd;
            while (insertIndex > dependenciesStart + 1 && char.IsWhiteSpace(manifest[insertIndex - 1]))
            {
                insertIndex--;
            }

            var dependenciesContent = manifest.Substring(dependenciesStart + 1, insertIndex - dependenciesStart - 1);
            var hasExistingDependencies = Regex.IsMatch(dependenciesContent, "\"[^\"]+\"\\s*:", RegexOptions.CultureInvariant);
            var newline = DetectNewLine(manifest);
            var insertion = new StringBuilder();
            if (hasExistingDependencies)
            {
                insertion.Append(",");
            }

            foreach (var dependency in dependenciesToAdd)
            {
                if (added > 0)
                {
                    insertion.Append(",");
                }

                insertion.Append(newline);
                insertion.Append("    \"");
                insertion.Append(dependency.Key);
                insertion.Append("\": \"");
                insertion.Append(dependency.Value);
                insertion.Append("\"");
                added++;
            }

            if (added == 0)
            {
                message = "No local extension package dependency was added.";
                return true;
            }

            File.WriteAllText(manifestPath, manifest.Insert(insertIndex, insertion.ToString()), new UTF8Encoding(false));
            message = $"Added {added} local extension package {FormatDependencyNoun(added)} to Packages/manifest.json.";
            return true;
        }

        private static string FormatDependencyNoun(int count)
        {
            return count == 1 ? "dependency" : "dependencies";
        }

        private static string DetectNewLine(string text)
        {
            return text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
        }

        private static bool TryFindDependenciesObject(string manifest, out int objectStart, out int objectEnd)
        {
            objectStart = -1;
            objectEnd = -1;

            var match = Regex.Match(manifest, "\"dependencies\"\\s*:\\s*\\{", RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                return false;
            }

            objectStart = manifest.IndexOf('{', match.Index + match.Length - 1);
            if (objectStart < 0)
            {
                return false;
            }

            var depth = 0;
            var inString = false;
            var escaping = false;
            for (var index = objectStart; index < manifest.Length; index++)
            {
                var character = manifest[index];
                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (character == '\\')
                    {
                        escaping = true;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                    continue;
                }

                if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        objectEnd = index;
                        return true;
                    }
                }
            }

            objectStart = -1;
            return false;
        }

        private static string GetProjectManifestPath()
        {
            return Path.Combine(ProjectRootPath, "Packages", "manifest.json");
        }

        private bool ConfigureStartUpProcedure(string entranceProcedureTypeName, bool includeExtensionShowcase, out string message)
        {
            if (!File.Exists(Path.Combine(ProjectRootPath, StartUpScenePath)))
            {
                message = $"StartUp scene was not found at {StartUpScenePath}. Run Initialize / Repair Project first.";
                SetStatus(message, true);
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                message = "StartUp scene update was cancelled.";
                return false;
            }

            var scene = EditorSceneManager.OpenScene(StartUpScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                message = $"Failed to open StartUp scene: {StartUpScenePath}.";
                SetStatus(message, true);
                return false;
            }

            var procedureComponent = UnityEngine.Object.FindFirstObjectByType<EFrameProcedureComponent>();
            if (procedureComponent == null)
            {
                message = "StartUp scene does not contain an EFrameProcedureComponent on the Boot object.";
                SetStatus(message, true);
                return false;
            }

            ConfigureProcedureComponent(procedureComponent, includeExtensionShowcase, entranceProcedureTypeName);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = $"Failed to save StartUp scene after setting {entranceProcedureTypeName}.";
                SetStatus(message, true);
                return false;
            }

            message = $"Set StartUp entrance procedure to {entranceProcedureTypeName}.";
            return true;
        }

        private bool RunToolScript(string scriptName, string arguments)
        {
            return RunToolScript(scriptName, arguments, out _);
        }

        private bool RunToolScript(string scriptName, string arguments, out ToolScriptResult result)
        {
            if (!TryGetFrameworkRoot(out var frameworkRoot, out var error))
            {
                result = ToolScriptResult.Failed(scriptName, error);
                SetStatus(error, true);
                return false;
            }

            var scriptPath = Path.Combine(frameworkRoot, "Tools~", scriptName);
            if (!File.Exists(scriptPath))
            {
                var message = $"Tool script not found: {scriptPath}";
                result = ToolScriptResult.Failed(scriptName, message);
                SetStatus(message, true);
                return false;
            }

            try
            {
                var shell = GetPowerShellExecutable();
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = frameworkRoot
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    const string message = "Failed to start PowerShell process.";
                    result = ToolScriptResult.Failed(scriptName, message);
                    SetStatus(message, true);
                    return false;
                }

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();
                result = new ToolScriptResult
                {
                    ScriptName = scriptName,
                    StandardOutput = standardOutput,
                    StandardError = standardError,
                    ExitCode = process.ExitCode,
                    StatusMessage = process.ExitCode == 0 ? $"Completed {scriptName}." : $"Tool script failed: {scriptName}"
                };

                if (!string.IsNullOrWhiteSpace(standardOutput))
                {
                    Debug.Log(standardOutput.Trim());
                }

                if (!string.IsNullOrWhiteSpace(standardError))
                {
                    Debug.LogWarning(standardError.Trim());
                }

                if (process.ExitCode != 0)
                {
                    SetStatus($"Tool script failed: {scriptName}", true);
                    return false;
                }

                AssetDatabase.Refresh();
                SetStatus($"Completed {scriptName}.", false);
                return true;
            }
            catch (Exception exception)
            {
                var message = $"Failed to execute {scriptName}: {exception.Message}";
                result = ToolScriptResult.Failed(scriptName, message);
                SetStatus(message, true);
                return false;
            }
        }

        private static void EnsurePendingEditorActionProcessor()
        {
            if (s_pendingEditorActionProcessorRegistered)
            {
                return;
            }

            EditorApplication.update += ProcessPendingEditorActions;
            s_pendingEditorActionProcessorRegistered = true;
        }

        private static void EnqueueEditorAction(Action action)
        {
            if (action == null)
            {
                return;
            }

            lock (PendingEditorActionsLock)
            {
                PendingEditorActions.Enqueue(action);
            }
        }

        private static void ProcessPendingEditorActions()
        {
            while (true)
            {
                Action action;
                lock (PendingEditorActionsLock)
                {
                    if (PendingEditorActions.Count == 0)
                    {
                        return;
                    }

                    action = PendingEditorActions.Dequeue();
                }

                try
                {
                    action.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static bool RunToolScriptBackground(string frameworkRoot, string shell, string scriptName, string arguments, out ToolScriptResult result)
        {
            var scriptPath = Path.Combine(frameworkRoot, "Tools~", scriptName);
            if (!File.Exists(scriptPath))
            {
                result = ToolScriptResult.Failed(scriptName, $"Tool script not found: {scriptPath}");
                return false;
            }

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = frameworkRoot
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    result = ToolScriptResult.Failed(scriptName, "Failed to start PowerShell process.");
                    return false;
                }

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                result = new ToolScriptResult
                {
                    ScriptName = scriptName,
                    StandardOutput = standardOutput,
                    StandardError = standardError,
                    ExitCode = process.ExitCode,
                    StatusMessage = process.ExitCode == 0 ? $"Completed {scriptName}." : $"Tool script failed: {scriptName}"
                };
                return process.ExitCode == 0;
            }
            catch (Exception exception)
            {
                result = ToolScriptResult.Failed(scriptName, $"Failed to execute {scriptName}: {exception.Message}");
                return false;
            }
        }

        private static void LogToolScriptResult(ToolScriptResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                Debug.Log(result.StandardOutput.Trim());
            }

            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                Debug.LogWarning(result.StandardError.Trim());
            }
        }

        private struct ToolScriptResult
        {
            public string ScriptName;
            public string StandardOutput;
            public string StandardError;
            public string StatusMessage;
            public int ExitCode;

            public static ToolScriptResult Failed(string scriptName, string message)
            {
                return new ToolScriptResult
                {
                    ScriptName = scriptName,
                    StandardError = message,
                    StatusMessage = message,
                    ExitCode = -1
                };
            }
        }

        private static string GetPowerShellExecutable()
        {
            return Application.platform == RuntimePlatform.WindowsEditor ? "powershell.exe" : "pwsh";
        }

        private static bool IsAiSyncAvailable(out string frameworkRoot, out string reason)
        {
            var available = TryGetFrameworkRoot(out frameworkRoot, out reason);
            if (!available)
            {
                return false;
            }

            var toolsPath = Path.Combine(frameworkRoot, "Tools~", "Initialize-EFrameAI.ps1");
            if (File.Exists(toolsPath))
            {
                reason = string.Empty;
                return true;
            }

            reason = "EFrame package AI tools were not found. Reimport or update the EFrame package.";
            return false;
        }

        private static bool TryGetFrameworkRoot(out string frameworkRoot, out string error)
        {
            frameworkRoot = string.Empty;
            error = string.Empty;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{PackageName}");
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                error = "Could not resolve the EFrame package path.";
                return false;
            }

            frameworkRoot = packageInfo.resolvedPath;
            return true;
        }

        private bool SyncAddressablesAfterAssetChanges(string changeMessage)
        {
            if (!EFrameAddressablesBootstrapUtility.SyncProjectAddressablesAndGenerateResPath(out var syncMessage))
            {
                SetStatus(syncMessage, true);
                return false;
            }

            SetStatus($"{changeMessage} Synced Addressables groups and regenerated ResPath code.", false);
            return true;
        }

        private void SetStatus(string message, bool isError)
        {
            m_statusMessage = message;
            m_statusIsError = isError;
            Repaint();
        }

        private static bool NeedsInitialization()
        {
            var startUpSceneExists = File.Exists(Path.Combine(ProjectRootPath, StartUpScenePath));
            var aiManifestExists = File.Exists(Path.Combine(ProjectRootPath, ".github", "eframe-ai.manifest.json"));
            var addressablesReady = EFrameAddressablesBootstrapUtility.AreAddressablesInitialized();
            var appResGroupReady = EFrameAddressablesBootstrapUtility.AreProjectAddressablesGroupsReady();
            var uiSortingLayersReady = EFrameUIBootstrapUtility.AreUISortingLayersReady();
            var audioReady = EFrameAudioBootstrapUtility.IsAudioSetupReady();
            var generatedResPathReady = EFrameAddressablesBootstrapUtility.IsGeneratedResPathReady();
            return !startUpSceneExists || !aiManifestExists || !addressablesReady || !appResGroupReady || !generatedResPathReady || !uiSortingLayersReady || !audioReady;
        }

        [InitializeOnLoadMethod]
        private static void AutoOpenOnProjectLoad()
        {
            if (SessionState.GetBool(AutoPopupSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(AutoPopupSessionKey, true);

            EditorApplication.delayCall += () =>
            {
                if (NeedsInitialization())
                {
                    OpenWindow();
                }
            };
        }
    }
}
#endif
