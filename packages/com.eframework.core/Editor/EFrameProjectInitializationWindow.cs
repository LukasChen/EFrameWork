#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EFrameWork.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;
using Debug = UnityEngine.Debug;

namespace EFrameWork.Editor.ProjectBootstrap
{
    public sealed class EFrameProjectInitializationWindow : EditorWindow
    {
        private const string WindowTitle = "EFrame Project Init";
        private const string PackageName = "com.eframework.core";
        private const string StartUpScenePath = "Assets/Scenes/StartUp.unity";
        private const string StartUpGuidePath = "Assets/Scenes/StartUp_SETUP.md";
        private const string AutoPopupSessionKey = "EFrameWork.ProjectInitializationWindow.AutoPopupShown";
        private const string NamespacePrefsKey = "EFrameWork.ProjectInitializationWindow.RootNamespace";

        private string m_rootNamespace;
        private string m_statusMessage;
        private bool m_statusIsError;

        [MenuItem("EFrame Tools/项目初始化向导", false, -100)]
        public static void OpenWindow()
        {
            var window = GetWindow<EFrameProjectInitializationWindow>(true, WindowTitle);
            window.minSize = new Vector2(640f, 460f);
            window.InitializeState();
            window.Show();
        }

        private void OnEnable()
        {
            InitializeState();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("EFrame Project Initialization", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Project", ProjectRootPath);
                EditorGUILayout.LabelField("Framework Root", TryGetFrameworkRoot(out var frameworkRoot, out var rootError) ? frameworkRoot : rootError);
                EditorGUILayout.LabelField("AI Sync Available", IsAiSyncAvailable(out _, out _) ? "Yes" : "No");
                EditorGUILayout.LabelField("Addressables Ready", EFrameAddressablesBootstrapUtility.AreAddressablesInitialized() ? "Yes" : "No");
                EditorGUILayout.LabelField("App Res Group Ready", EFrameAddressablesBootstrapUtility.IsAppResAddressablesReady() ? "Yes" : "No");
                EditorGUILayout.LabelField("Audio Ready", EFrameAudioBootstrapUtility.IsAudioSetupReady() ? "Yes" : "No");
            }

            EditorGUILayout.Space();

            m_rootNamespace = EditorGUILayout.TextField("Root Namespace", m_rootNamespace);

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("One-Click", EditorStyles.boldLabel);

                if (GUILayout.Button("Full Initialize Project", GUILayout.Height(34f)))
                {
                    RunFullInitialization();
                }

                EditorGUILayout.HelpBox("Runs cold start if the framework repo is available locally, then creates or refreshes StartUp.unity with Boot + Main Camera.", MessageType.None);
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

                if (GUILayout.Button("Initialize Addressables"))
                {
                    EnsureAddressablesInitialized();
                }

                if (GUILayout.Button("Setup App Res Addressables Group"))
                {
                    EnsureAppResAddressablesGroup();
                }

                if (GUILayout.Button("Initialize Audio"))
                {
                    EnsureAudioSetup();
                }

                if (GUILayout.Button("Create Or Refresh StartUp Scene"))
                {
                    CreateOrRefreshStartUpScene();
                }

                if (GUILayout.Button("Open StartUp Setup Guide"))
                {
                    OpenStartUpGuide();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!IsAiSyncAvailable(out _, out var aiReason)))
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("AI", EditorStyles.boldLabel);

                if (!string.IsNullOrEmpty(aiReason))
                {
                    EditorGUILayout.HelpBox(aiReason, MessageType.Info);
                }

                if (GUILayout.Button("Cold Start Project Files"))
                {
                    RunToolScript("Initialize-EFrameColdStart.ps1", $"-TargetRoot \"{ProjectRootPath}\" -RootNamespace \"{m_rootNamespace}\" -Force");
                }

                if (GUILayout.Button("Sync AI Workspace"))
                {
                    RunToolScript("Initialize-EFrameAI.ps1", $"-TargetRoot \"{ProjectRootPath}\" -Force");
                }

                if (GUILayout.Button("Install Project AI Updater"))
                {
                    RunToolScript("Install-EFrameAIProjectUpdater.ps1", $"-TargetRoot \"{ProjectRootPath}\" -Force");
                }

                if (GUILayout.Button("Generate Project Overlay"))
                {
                    RunToolScript("New-EFrameProjectAIOverlay.ps1", $"-TargetRoot \"{ProjectRootPath}\" -Force");
                }

                if (GUILayout.Button("Generate Bootstrap Code"))
                {
                    RunToolScript("Initialize-EFrameBootstrapCode.ps1", $"-TargetRoot \"{ProjectRootPath}\" -RootNamespace \"{m_rootNamespace}\" -Force");
                }
            }

            if (!string.IsNullOrEmpty(m_statusMessage))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(m_statusMessage, m_statusIsError ? MessageType.Error : MessageType.Info);
            }
        }

        private void InitializeState()
        {
            m_rootNamespace = EditorPrefs.GetString(NamespacePrefsKey, GetDefaultNamespace());
        }

        private static string ProjectRootPath => Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        private static string ProjectName => new DirectoryInfo(ProjectRootPath).Name;

        private void RunFullInitialization()
        {
            if (IsAiSyncAvailable(out _, out _))
            {
                if (!RunToolScript("Initialize-EFrameColdStart.ps1", $"-TargetRoot \"{ProjectRootPath}\" -RootNamespace \"{m_rootNamespace}\" -Force"))
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

            if (!EnsureAppResAddressablesGroup())
            {
                return;
            }

            if (!EnsureAudioSetup())
            {
                return;
            }

            CreateOrRefreshStartUpScene();
        }

        private void CreateOrRefreshStartUpScene()
        {
            EditorPrefs.SetString(NamespacePrefsKey, m_rootNamespace);

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

            var mainCameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            mainCameraObject.tag = "MainCamera";

            var camera = mainCameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.orthographic = false;

            var bootObject = new GameObject("Boot", typeof(ProcedureComponent), typeof(EFrameComponent));
            var procedureComponent = bootObject.GetComponent<ProcedureComponent>();
            var eframeComponent = bootObject.GetComponent<EFrameComponent>();

            ConfigureProcedureComponent(procedureComponent, m_rootNamespace);
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

        private static void ConfigureProcedureComponent(ProcedureComponent procedureComponent, string rootNamespace)
        {
            var procedureObject = new SerializedObject(procedureComponent);
            var availableProcedures = procedureObject.FindProperty("m_AvailableProcedureTypeNames");
            var entranceProcedure = procedureObject.FindProperty("m_EntranceProcedureTypeName");

            var launcher = $"{rootNamespace}.Procedure.ProcedureLauncher";
            var home = $"{rootNamespace}.Procedure.ProcedureHome";

            availableProcedures.arraySize = 2;
            availableProcedures.GetArrayElementAtIndex(0).stringValue = launcher;
            availableProcedures.GetArrayElementAtIndex(1).stringValue = home;
            entranceProcedure.stringValue = launcher;

            procedureObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEFrameComponent(EFrameComponent eframeComponent, ProcedureComponent procedureComponent, Camera mainCamera)
        {
            var eframeObject = new SerializedObject(eframeComponent);
            eframeObject.FindProperty("m_procedureComponent").objectReferenceValue = procedureComponent;
            eframeObject.FindProperty("UICamera").objectReferenceValue = mainCamera;
            eframeObject.FindProperty("SceneCamera").objectReferenceValue = mainCamera;

            var designSize = eframeObject.FindProperty("DesignSize");
            designSize.FindPropertyRelative("x").intValue = 1080;
            designSize.FindPropertyRelative("y").intValue = 1920;

            eframeObject.FindProperty("FitMode").enumValueIndex = 1;
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

        private bool EnsureAppResAddressablesGroup()
        {
            var success = EFrameAddressablesBootstrapUtility.EnsureAppResAddressablesGroup(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool EnsureAudioSetup()
        {
            var success = EFrameAudioBootstrapUtility.EnsureAudioSetup(out var message);
            SetStatus(message, !success);
            return success;
        }

        private bool RunToolScript(string scriptName, string arguments)
        {
            if (!TryGetFrameworkRoot(out var frameworkRoot, out var error))
            {
                SetStatus(error, true);
                return false;
            }

            var scriptPath = Path.Combine(frameworkRoot, "tools", scriptName);
            if (!File.Exists(scriptPath))
            {
                SetStatus($"Tool script not found: {scriptPath}", true);
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
                    SetStatus("Failed to start PowerShell process.", true);
                    return false;
                }

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

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
                SetStatus($"Failed to execute {scriptName}: {exception.Message}", true);
                return false;
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

            var toolsPath = Path.Combine(frameworkRoot, "tools", "Initialize-EFrameAI.ps1");
            if (File.Exists(toolsPath))
            {
                reason = string.Empty;
                return true;
            }

            reason = "Framework tools were not found. AI sync integration works when the project references a local cloned EFrameWork repo.";
            return false;
        }

        private static bool TryGetFrameworkRoot(out string frameworkRoot, out string error)
        {
            frameworkRoot = string.Empty;
            error = string.Empty;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{PackageName}");
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                error = "Could not resolve the EFrameWork package path.";
                return false;
            }

            var packageRoot = packageInfo.resolvedPath;
            var packagesDirectory = Directory.GetParent(packageRoot);
            var repoRoot = packagesDirectory?.Parent;

            if (repoRoot == null)
            {
                error = "Could not infer framework repo root from package path.";
                return false;
            }

            frameworkRoot = repoRoot.FullName;
            return true;
        }

        private static string GetDefaultNamespace()
        {
            var candidate = ProjectName.Replace(" ", string.Empty).Replace("-", string.Empty);
            return string.IsNullOrWhiteSpace(candidate) ? "GameApp" : candidate;
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
            var appResGroupReady = EFrameAddressablesBootstrapUtility.IsAppResAddressablesReady();
            var audioReady = EFrameAudioBootstrapUtility.IsAudioSetupReady();
            return !startUpSceneExists || !aiManifestExists || !addressablesReady || !appResGroupReady || !audioReady;
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