#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using EFramework.Runtime;
using EFramework.Runtime.Procedure;
using EFramework.Runtime.UI;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
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
        private const string StartUpGuidePath = "Assets/Scenes/StartUp_SETUP.md";
        private const string ModulesRootPath = "Assets/Modules";
        private const string ExtensionShowcaseTargetPath = "Assets/Modules/EFrameExtensionShowcase";
        private const string ExtensionShowcaseTemplatePath = "Packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase";
        private const string BasicStartupProcedureName = "GameApp.Procedure.ProcedureLauncher";
        private const string ExtensionShowcaseStartupProcedureName = "GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry";
        private const string AutoPopupSessionKey = "EFrame.ProjectInitializationWindow.AutoPopupShown";
        private const string ModuleNamePrefsKey = "EFrame.ProjectInitializationWindow.ModuleName";
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

        private static readonly Queue<(string packageName, string packageSpec)> PendingPackageAdds = new();
        private static AddRequest s_packageAddRequest;
        private static string s_currentPackageName;

        private string m_moduleName;
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

                if (GUILayout.Button("Install Showcase"))
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
            m_moduleName = EditorPrefs.GetString(ModuleNamePrefsKey, string.Empty);
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
                SetStatus("Framework root was not detected as a local repo. Running scene initialization only.", false);
            }

            if (!EnsureAddressablesInitialized())
            {
                return;
            }

            if (!EnsureUISortingLayers())
            {
                return;
            }

            if (!EnsureSampleUIPrefabs())
            {
                return;
            }

            if (!EnsureAudioSetup())
            {
                return;
            }

            EnsureOptionalDotweenSetup();

            CreateOrRefreshStartUpScene();
        }

        private void CreateOrRefreshStartUpScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var sceneDirectory = Path.GetDirectoryName(StartUpScenePath);
            if (!string.IsNullOrEmpty(sceneDirectory) && !Directory.Exists(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
            }

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mainCameraObject = new GameObject("Main Camera", typeof(Camera));
            mainCameraObject.tag = "MainCamera";

            var camera = mainCameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.orthographic = false;

            var bootObject = new GameObject("Boot", typeof(EFrameProcedureComponent), typeof(EFrameComponent));
            var procedureComponent = bootObject.GetComponent<EFrameProcedureComponent>();
            var eframeComponent = bootObject.GetComponent<EFrameComponent>();

            ConfigureProcedureComponent(procedureComponent, false, BasicStartupProcedureName);
            ConfigureEFrameComponent(eframeComponent, procedureComponent, camera);

            EnsureStartUpSceneInBuildSettings(StartUpScenePath);

            if (!EditorSceneManager.SaveScene(newScene, StartUpScenePath))
            {
                SetStatus("Failed to save StartUp scene.", true);
                return;
            }

            Selection.activeObject = bootObject;
            SetStatus($"Created or refreshed {StartUpScenePath}.", false);
            AssetDatabase.Refresh();
        }

        private static void ConfigureProcedureComponent(EFrameProcedureComponent procedureComponent, bool includeExtensionShowcase, string entranceProcedureTypeName)
        {
            var procedureObject = new SerializedObject(procedureComponent);
            var availableProcedures = procedureObject.FindProperty("m_availableProcedureTypeNames");
            var entranceProcedure = procedureObject.FindProperty("m_entranceProcedureTypeName");

            var procedureTypeNames = new List<string>
            {
                $"{AppNamespace}.Procedure.ProcedureLauncher",
                $"{AppNamespace}.Procedure.ProcedureHome",
                $"{AppNamespace}.Modules.SampleModule.Procedure.ProcedureSampleModuleEntry"
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

        private static void ConfigureEFrameComponent(EFrameComponent eframeComponent, EFrameProcedureComponent procedureComponent, Camera mainCamera)
        {
            var eframeObject = new SerializedObject(eframeComponent);
            eframeObject.FindProperty("m_procedureComponent").objectReferenceValue = procedureComponent;
            eframeObject.FindProperty("UICamera").objectReferenceValue = mainCamera;
            eframeObject.FindProperty("SceneCamera").objectReferenceValue = mainCamera;

            var designSize = eframeObject.FindProperty("DesignSize");
            designSize.FindPropertyRelative("x").intValue = 1080;
            designSize.FindPropertyRelative("y").intValue = 1920;

            eframeObject.FindProperty("FitMode").enumValueIndex = (int)ScreenFitMode.Auto;
            eframeObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureStartUpSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene => scene.path == scenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private void OpenStartUpGuide()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(StartUpGuidePath);
            if (asset == null)
            {
                SetStatus($"Guide not found at {StartUpGuidePath}. Run bootstrap generation first.", true);
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            SetStatus($"Opened guide at {StartUpGuidePath}.", false);
        }

        private bool EnsureAddressablesInitialized()
        {
            var success = EFrameAddressablesBootstrapUtility.EnsureAddressablesInitialized(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool EnsureProjectAddressablesGroups()
        {
            var success = EFrameAddressablesBootstrapUtility.EnsureProjectAddressablesGroups(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool GenerateResPathCode()
        {
            var success = EFrameAddressablesBootstrapUtility.GenerateResPathCode(out var message);
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

        private bool OpenDotweenSettings()
        {
            var success = EFrameDotweenBootstrapUtility.OpenDotweenSettings(out var message);
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

        private bool EnsureSampleUIPrefabs()
        {
            var success = EFrameSampleUIPrefabUtility.EnsureBootstrapSampleUIPrefabs(out var message);
            if (!success)
            {
                SetStatus(message, true);
                return false;
            }

            return SyncAddressablesAfterAssetChanges(message);
        }

        private bool EnsureUISortingLayers()
        {
            var success = EFrameUIBootstrapUtility.EnsureUISortingLayers(out var message);
            SetStatus(message, !success);
            return success;
        }

        private void ImportInitialTemplates()
        {
            EnsureSampleUIPrefabs();
        }

        private void ImportBasicTemplate()
        {
            if (!RunToolScript("Initialize-EFrameBootstrapCode.ps1", $"-TargetRoot \"{ProjectRootPath}\""))
            {
                return;
            }

            if (!EnsureUISortingLayers())
            {
                return;
            }

            if (!EnsureSampleUIPrefabs())
            {
                return;
            }

            if (!EnsureAudioSetup())
            {
                return;
            }

            if (!EnsureProjectAddressablesGroups())
            {
                return;
            }

            SyncAddressablesAfterAssetChanges("Imported Basic sample/template.");
        }

        private void InstallExtensionShowcaseModule()
        {
            TryInstallLocalExtensionPackages(out var packageMessage);
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

            SetStatus($"{copyMessage} {packageMessage} {startupMessage}".Trim(), false);
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
            _ = Task.Run(() =>
            {
                var syncSucceeded = RunToolScriptBackground(
                    frameworkRoot,
                    "Initialize-EFrameAI.ps1",
                    $"-TargetRoot \"{projectRoot}\" -Clients {AllAiClientsArgument} -StatusOnly",
                    out var syncResult);

                var healthSucceeded = RunToolScriptBackground(
                    frameworkRoot,
                    "Test-EFrameAIProject.ps1",
                    $"-TargetRoot \"{projectRoot}\" -FrameworkRoot \"{frameworkRoot}\"",
                    out var healthResult);

                EditorApplication.delayCall += () =>
                {
                    LogToolScriptResult(syncResult);
                    LogToolScriptResult(healthResult);

                    m_aiChecksRunning = false;
                    m_aiCheckIsError = !syncSucceeded || !healthSucceeded;
                    m_aiCheckMessage = BuildAiCheckMessage(syncSucceeded, syncResult, healthSucceeded, healthResult);
                    SetStatus(m_aiCheckIsError ? "AI checks completed with issues." : "AI checks passed.", m_aiCheckIsError);
                };
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

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (var sourceFile in Directory.GetFiles(sourceDirectory))
            {
                if (sourceFile.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = Path.GetFileName(sourceFile);
                if (fileName.EndsWith(".cs.txt", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - ".txt".Length);
                }

                var targetFile = Path.Combine(targetDirectory, fileName);
                File.Copy(sourceFile, targetFile, false);
            }

            foreach (var sourceChildDirectory in Directory.GetDirectories(sourceDirectory))
            {
                var targetChildDirectory = Path.Combine(targetDirectory, Path.GetFileName(sourceChildDirectory));
                CopyDirectory(sourceChildDirectory, targetChildDirectory);
            }
        }

        private bool TryInstallLocalExtensionPackages(out string message)
        {
            if (!TryGetFrameworkRoot(out var frameworkRoot, out var error))
            {
                message = $"{error} Extension packages were not installed automatically.";
                return false;
            }

            var packageParent = Directory.GetParent(frameworkRoot)?.FullName;
            if (string.IsNullOrEmpty(packageParent))
            {
                message = "Could not resolve the package parent directory. Install extension packages manually if needed.";
                return false;
            }

            var started = 0;
            var missing = new List<string>();
            foreach (var packageName in ExtensionPackageNames)
            {
                if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{packageName}") != null)
                {
                    continue;
                }

                var siblingPath = Path.Combine(packageParent, packageName);
                if (!Directory.Exists(siblingPath))
                {
                    missing.Add(packageName);
                    continue;
                }

                EnqueuePackageAdd(packageName, $"file:{siblingPath.Replace('\\', '/')}");
                started++;
            }

            if (started > 0)
            {
                StartPackageInstallQueue();
            }

            message = BuildExtensionPackageInstallMessage(started, missing);
            return started > 0;
        }

        private static string BuildExtensionPackageInstallMessage(int started, List<string> missing)
        {
            var parts = new List<string>();
            if (started > 0)
            {
                parts.Add($"Started installing {started} local extension package(s).");
            }
            else
            {
                parts.Add("No local extension package install was started.");
            }

            if (missing.Count > 0)
            {
                parts.Add($"Missing sibling packages: {string.Join(", ", missing)}. Install them manually from git or UPM if this is not a local framework checkout.");
            }

            return string.Join(" ", parts);
        }

        private static void EnqueuePackageAdd(string packageName, string packageSpec)
        {
            PendingPackageAdds.Enqueue((packageName, packageSpec));
        }

        private static void StartPackageInstallQueue()
        {
            EditorApplication.update -= ProcessPackageInstallQueue;
            EditorApplication.update += ProcessPackageInstallQueue;
            ProcessPackageInstallQueue();
        }

        private static void ProcessPackageInstallQueue()
        {
            if (s_packageAddRequest != null)
            {
                if (!s_packageAddRequest.IsCompleted)
                {
                    return;
                }

                if (s_packageAddRequest.Status == StatusCode.Success)
                {
                    Debug.Log($"[EFrame] Installed extension package: {s_currentPackageName}");
                }
                else
                {
                    Debug.LogWarning($"[EFrame] Failed to install extension package {s_currentPackageName}: {s_packageAddRequest.Error?.message}");
                }

                s_packageAddRequest = null;
                s_currentPackageName = string.Empty;
            }

            if (PendingPackageAdds.Count == 0)
            {
                EditorApplication.update -= ProcessPackageInstallQueue;
                AssetDatabase.Refresh();
                return;
            }

            var next = PendingPackageAdds.Dequeue();
            s_currentPackageName = next.packageName;
            Debug.Log($"[EFrame] Installing extension package {next.packageName} from {next.packageSpec}");
            s_packageAddRequest = Client.Add(next.packageSpec);
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

        private void CreateModuleScaffold()
        {
            var sanitizedModuleName = SanitizeIdentifier(m_moduleName);
            if (string.IsNullOrWhiteSpace(sanitizedModuleName))
            {
                SetStatus("Enter a valid module name before creating a module scaffold.", true);
                return;
            }

            var targetPath = $"{ModulesRootPath}/{sanitizedModuleName}";
            if (AssetDatabase.IsValidFolder(targetPath) || File.Exists(Path.Combine(ProjectRootPath, targetPath)))
            {
                SetStatus($"Module already exists: {targetPath}", true);
                return;
            }

            try
            {
                CreateModuleScaffoldFiles(sanitizedModuleName);
                if (!EFrameSampleUIPrefabUtility.EnsureModuleViewTemplate(sanitizedModuleName, out var templateMessage))
                {
                    SetStatus(templateMessage, true);
                    return;
                }

                if (!SyncAddressablesAfterAssetChanges(templateMessage))
                {
                    return;
                }

                EditorPrefs.SetString(ModuleNamePrefsKey, sanitizedModuleName);
                AssetDatabase.Refresh();

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }

                SetStatus($"Created module scaffold at {targetPath}.", false);
            }
            catch (Exception exception)
            {
                SetStatus($"Failed to create module scaffold: {exception.Message}", true);
            }
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

        private static bool RunToolScriptBackground(string frameworkRoot, string scriptName, string arguments, out ToolScriptResult result)
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
                    FileName = GetPowerShellExecutable(),
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

        private static string SanitizeIdentifier(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            var characters = new List<char>(rawValue.Length);
            foreach (var character in rawValue)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                {
                    characters.Add(character);
                }
            }

            return new string(characters.ToArray());
        }

        private void CreateModuleScaffoldFiles(string moduleName)
        {
            var moduleNamespace = $"{AppNamespace}.Modules.{moduleName}";
            var moduleRoot = Path.Combine(ProjectRootPath, "Assets", "Modules", moduleName);

            WriteScaffoldFile(Path.Combine(moduleRoot, "README.md"), BuildModuleGuideContent(moduleName));
            WriteScaffoldFile(Path.Combine(moduleRoot, "Runtime", "Procedure", $"Procedure{moduleName}Entry.cs"), BuildModuleProcedureContent(moduleName, moduleNamespace));
            WriteScaffoldFile(Path.Combine(moduleRoot, "Runtime", "UI", "Views", $"{moduleName}MainView.cs"), BuildModuleViewContent(moduleName, moduleNamespace));
            WriteScaffoldFile(Path.Combine(moduleRoot, "Runtime", "UI", "Controllers", $"{moduleName}MainViewController.cs"), BuildModuleControllerContent(moduleName, moduleNamespace));

            Directory.CreateDirectory(Path.Combine(moduleRoot, "Editor"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Res", "UI", "Panels", $"{moduleName}Main"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Res", "UI", "Common"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Res", "SceneAssets", "Common"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Res", "FX", "Common"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Res", "FX", "UI"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Res", "FX", "Scene"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Res", "FX", "Gameplay"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "Scenes"));
        }

        private static void WriteScaffoldFile(string filePath, string content)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, content);
        }

        private static string BuildModuleGuideContent(string moduleName)
        {
            return $@"# {moduleName}

This is a formal business module scaffold generated by EFrame.

Recommended next steps:

1. Replace placeholder logs with actual module entry logic.
2. The scaffold already copies `{moduleName}MainView.prefab` into `Assets/Modules/{moduleName}/Res/UI/Panels/{moduleName}Main`; adjust that prefab in-place instead of rebuilding it from scratch.
3. Add module scenes under `Assets/Modules/{moduleName}/Scenes` if this module owns scene content.
4. Put module-private FX under `Assets/Modules/{moduleName}/Res/FX`; only move shared content back to `Assets/App/...` when it is truly common.
";
        }

        private string BuildModuleProcedureContent(string moduleName, string moduleNamespace)
        {
            return $@"using Cysharp.Threading.Tasks;
using EFramework.Generated;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Procedure;
using UnityEngine;

namespace {moduleNamespace}.Procedure
{{
    public sealed class Procedure{moduleName}Entry : EFrameProcedure
    {{
        protected override async UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
        {{
            await assets.PreloadAsync<GameObject>(ResPath.Generated.Modules.{moduleName}.Res.UI.Panels.{moduleName}Main.{moduleName}MainView);
        }}

        protected override void OnEnter(ProcedureEnterContext context)
        {{
            base.OnEnter(context);
            Debug.Log(""[Procedure{moduleName}Entry] Entered. TODO: open {moduleName}MainView."");
        }}

        protected override void OnLeave(bool isShutdown)
        {{
            base.OnLeave(isShutdown);
        }}
    }}
}}
";
        }

        private static string BuildModuleViewContent(string moduleName, string moduleNamespace)
        {
            return $@"using EFramework.Runtime.UI;
using UnityEngine.UI;

namespace {moduleNamespace}.UI.Views
{{
    public sealed class {moduleName}MainView : BindingViewBase
    {{
        public {moduleName}MainView()
        {{
        }}

        protected override void OnBindingSet()
        {{
            base.OnBindingSet();
            CacheComponents();
        }}

        public Button BackButton {{ get; private set; }}

        private void CacheComponents()
        {{
            BackButton = Binding == null ? null : Binding.transform.Find(""Panel/BackButton"")?.GetComponent<Button>();
        }}
    }}
}}
";
        }

        private string BuildModuleControllerContent(string moduleName, string moduleNamespace)
        {
            return $@"using System;
using EFramework.Generated;
using EFramework.Runtime.UI;
using {moduleNamespace}.UI.Views;

namespace {moduleNamespace}.UI.Controllers
{{
    public sealed class {moduleName}MainViewController : UIControllerBase<{moduleName}MainView>
    {{
        public Action BackRequested {{ get; set; }}

        protected override string AssetPath => ResPath.Generated.Modules.{moduleName}.Res.UI.Panels.{moduleName}Main.{moduleName}MainView;

        protected override void OnViewCreated()
        {{
            base.OnViewCreated();
            AddButtonClickListener(CurrentView.BackButton, OnBackButtonClick);
        }}

        protected override void OnViewDestroyed()
        {{
            BackRequested = null;
            base.OnViewDestroyed();
        }}

        private void OnBackButtonClick()
        {{
            BackRequested?.Invoke();
        }}
    }}
}}
";
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
