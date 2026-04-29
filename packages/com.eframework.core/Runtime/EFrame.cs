using System.Collections;
using DG.Tweening;
using EFrameWork.Runtime.Asset;
using EFrameWork.Runtime.Audio;
using EFrameWork.Runtime.DataStorage;
using EFrameWork.Runtime.Event;
using EFrameWork.Runtime.UI;
using EFrameWork.Runtime.Utils;
using EFrameWork.Runtime.Vibration;
using GameFramework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EFrameWork.Runtime
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
                LastInitializationResult = EFrameInitializationResult.Failure("EFrameComponent is null.");
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

            DefaultFrameData defaultFrameData = null;
            AudioManager audioService = null;
            AudioEventManager audioEventService = null;
            QVibration vibrationService = null;

            yield return AssetManager.InitializeCoroutine();
            if (AssetManager.InitializeFailed)
            {
                LastInitializationResult = EFrameInitializationResult.Failure("Addressables initialization failed.");
                Debug.LogError($"[EFrame] {LastInitializationResult.Message}");
                yield break;
            }

            GameTimeService.Init();

            dataService.RegisterTable<DefaultFrameData>();
            defaultFrameData = dataService.GetTable<DefaultFrameData>();
            if (defaultFrameData == null)
            {
                LastInitializationResult = EFrameInitializationResult.Failure("DefaultFrameData registration failed.");
                Debug.LogError($"[EFrame] {LastInitializationResult.Message}");
                dataService.Dispose();
                coroutineService.Dispose();
                yield break;
            }

            vibrationService = new QVibration(defaultFrameData.VibrationOn);
            audioService = new AudioManager(component);
            audioService.Initialize(defaultFrameData.MusicOn, defaultFrameData.SoundOn);
            audioEventService = new AudioEventManager(component, audioService, vibrationService);

            var context = new EFrameContext(
                component,
                assetService,
                uiService,
                audioService,
                audioEventService,
                dataService,
                eventService,
                coroutineService,
                vibrationService);

            Current = context;
            component.BindContext(context);

            var designSize = component.ResolvedDesignSize;
            uiService.Init(component.UICamera, designSize.x, designSize.y, component.ResolvedFitMode);
            audioEventService.Initialize();

            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(500, 50);

            LastInitializationResult = EFrameInitializationResult.Success();
            Debug.Log("[EFrame] Initialize completed.");
            yield return null;
        }

        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (!Initialized) return;

            GameFrameworkEntry.Update(deltaTime, unscaledDeltaTime);
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
            GameFrameworkEntry.Shutdown();

            Current?.Dispose();
            Current = null;
            LastInitializationResult = default;
            s_frameCount = 0;
            s_timePassed = 0f;
            s_averageFPS = 0f;
        }
    }
}
