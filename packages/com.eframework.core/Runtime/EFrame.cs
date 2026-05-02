using System;
using System.Collections;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Audio;
using EFramework.Runtime.DataStorage;
using EFramework.Runtime.Event;
using EFramework.Runtime.UI;
using EFramework.Runtime.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EFramework.Runtime
{
    public static class EFrame
    {
        private static int s_frameCount;
        private static float s_timePassed;
        private static float s_averageFPS;

        public static EFrameContext Current { get; private set; }
        public static EFrameInitializationResult LastInitializationResult { get; private set; }
        public static bool Initialized => Current != null && LastInitializationResult.Succeeded;
        public static float SampleDuration = 1f;
        public static float FPS => s_averageFPS;
        public static IAssetService Assets => RequireInitializedContext().Assets;
        public static IUIService UI => RequireInitializedContext().UI;
        public static IAudioService Audio => RequireInitializedContext().Audio;
        public static IDataService Data => RequireInitializedContext().Data;
        public static IEventService Events => RequireInitializedContext().Events;
        public static ICoroutineService Coroutine => RequireInitializedContext().Coroutine;
        public static Camera SceneCamera => RequireInitializedContext().SceneCamera;
        public static Camera UICamera => RequireInitializedContext().UICamera;

        public static bool IsLtsDev
        {
            get
            {
#if LTS_DEV
                return true;
#else
                return false;
#endif
            }
        }

        public static IEnumerator Initialize(EFrameComponent component)
        {
            if (component == null)
            {
                LastInitializationResult = EFrameInitializationResult.Failure("EFrameComponent is null.", EFrameInitializationStage.Component);
                yield break;
            }

            if (Current != null)
            {
                Debug.LogWarning("[EFrame] Existing context found. Disposing it before reinitialization.");
                Dispose();
            }

            Debug.Log("[EFrame] Initialize started.");
            yield return null;

            var assetService = new AssetManager();
            var coroutineService = new CoroutineManager(component);
            var dataService = new DataManager(new JsonFileStorage());
            var eventService = new EventService();
            var uiService = new QUI();
            EFrameContext context = null;

            DefaultFrameData defaultFrameData = null;
            AudioManager audioService = null;

            yield return AssetManager.InitializeCoroutine();
            if (AssetManager.InitializeFailed)
            {
                LastInitializationResult = EFrameInitializationResult.Failure("Addressables initialization failed.", EFrameInitializationStage.Assets);
                Debug.LogError($"[EFrame] {LastInitializationResult.Message}");
                DisposeCreatedServices(audioService, uiService, assetService, dataService, coroutineService, eventService);
                yield break;
            }

            GameTimeService.Init();

            dataService.RegisterTable<DefaultFrameData>();
            defaultFrameData = dataService.GetTable<DefaultFrameData>();
            if (defaultFrameData == null)
            {
                LastInitializationResult = EFrameInitializationResult.Failure("DefaultFrameData registration failed.", EFrameInitializationStage.Data);
                Debug.LogError($"[EFrame] {LastInitializationResult.Message}");
                DisposeCreatedServices(audioService, uiService, assetService, dataService, coroutineService, eventService);
                yield break;
            }

            audioService = new AudioManager(component);
            audioService.Initialize(defaultFrameData.MusicOn, defaultFrameData.SoundOn);

            context = new EFrameContext(
                component,
                assetService,
                uiService,
                audioService,
                dataService,
                eventService,
                coroutineService);

            Current = context;
            component.BindContext(context);

            var designSize = component.ResolvedDesignSize;
            uiService.Init(component.UICamera, designSize.x, designSize.y, component.ResolvedFitMode, component.ResolvedEnableScreenFitDebugLog);

            LastInitializationResult = EFrameInitializationResult.Success();
            Debug.Log("[EFrame] Initialize completed.");
            yield return null;
        }

        private static EFrameContext RequireInitializedContext()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("EFrame.Initialize(...) must complete successfully before accessing framework services.");
            }

            return Current;
        }

        private static void DisposeCreatedServices(
            AudioManager audioService,
            QUI uiService,
            AssetManager assetService,
            DataManager dataService,
            CoroutineManager coroutineService,
            EventService eventService)
        {
            audioService?.Dispose();
            uiService?.Dispose();
            assetService?.Dispose();
            dataService?.Dispose();
            coroutineService?.Dispose();
            eventService?.ClearAll();
        }

        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (!Initialized) return;

            s_frameCount++;
            s_timePassed += Time.unscaledDeltaTime;
            if (s_timePassed >= SampleDuration)
            {
                s_averageFPS = s_frameCount / s_timePassed;
                Current.FPS = s_averageFPS;
                s_frameCount = 0;
                s_timePassed = 0f;
            }
        }

        public static void Dispose()
        {
            TaskQueue.Reset();

            Current?.Dispose();
            Current = null;
            LastInitializationResult = default;
            s_frameCount = 0;
            s_timePassed = 0f;
            s_averageFPS = 0f;
        }
    }
}
