using Cysharp.Threading.Tasks;
using EFrameWork.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace EFrameWork.Runtime.Asset
{
    /// <summary>
    /// 资源管理器
    /// - 提供 Addressables 资源的同步加载、实例化、对象池管理
    /// - 支持两种对象池：AssetReference 池和 Path 池
    /// </summary>
    public sealed class AssetManager : IAssetService
    {
        private const int DefaultMaxPoolSizePerKey = 32;

        private static Dictionary<AssetReferenceGameObject, Stack<GameObject>> m_pools = new();
        private static Dictionary<string, Stack<GameObject>> m_pathPools = new();
        public static bool Initialized { get; private set; }
        bool IAssetService.Initialized => Initialized;
        public static int MaxPoolSizePerKey { get; set; } = DefaultMaxPoolSizePerKey;

        #region 初始化

        /// <summary>
        /// 初始化是否失败
        /// </summary>
        public static bool InitializeFailed { get; private set; }
        bool IAssetService.InitializeFailed => InitializeFailed;

        /// <summary>
        /// 初始化 Addressables 系统
        /// </summary>
        public static IEnumerator InitializeCoroutine()
        {
            if (Initialized) yield break;

            Initialized = false;
            InitializeFailed = false;

            AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> handle = default;

            try
            {
                handle = Addressables.InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] Addressables.InitializeAsync 异常: {e.Message}");
                InitializeFailed = true;
                yield break;
            }

            // 允许 Unity 在后台处理初始化，不会卡死主线程
            while (handle.IsValid() && !handle.IsDone)
            {
                yield return null;
            }

            if (!handle.IsValid())
            {
                // Handle 已被释放，说明初始化已同步完成
                Debug.Log("Addressables 初始化完成");
                Initialized = true;
            }
            else if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Addressables 本地初始化成功");
                Initialized = true;
            }
            else
            {
                Debug.LogError($"[AssetManager] 初始化失败: {handle.OperationException}");
                InitializeFailed = true;
            }
        }

        public async UniTask<bool> InitializeAsync()
        {
            if (Initialized) return true;

            Initialized = false;
            InitializeFailed = false;

            AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> handle;
            try
            {
                handle = Addressables.InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] Addressables.InitializeAsync exception: {e.Message}");
                InitializeFailed = true;
                return false;
            }

            while (handle.IsValid() && !handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (!handle.IsValid() || handle.Status == AsyncOperationStatus.Succeeded)
            {
                Initialized = true;
                return true;
            }

            Debug.LogError($"[AssetManager] Addressables initialization failed: {handle.OperationException}");
            InitializeFailed = true;
            return false;
        }

        public async UniTask<AssetHandle<T>> LoadAsync<T>(string assetId) where T : Object
        {
            if (string.IsNullOrEmpty(assetId))
            {
                Debug.LogError("[AssetManager] LoadAsync failed: assetId is empty.");
                return default;
            }

            var handle = Addressables.LoadAssetAsync<T>(assetId);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AssetManager] LoadAsync failed: {assetId} - {handle.OperationException}");
                if (handle.IsValid()) Addressables.Release(handle);
                return default;
            }

            return new AssetHandle<T>(handle);
        }

        public async UniTask<AssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference) where T : Object
        {
            if (reference == null)
            {
                Debug.LogError("[AssetManager] LoadAsync failed: reference is null.");
                return default;
            }

            var handle = Addressables.LoadAssetAsync<T>(reference);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AssetManager] LoadAsync failed: {reference.RuntimeKey} - {handle.OperationException}");
                if (handle.IsValid()) Addressables.Release(handle);
                return default;
            }

            return new AssetHandle<T>(handle);
        }

        public async UniTask<InstanceHandle> InstantiateAsync(string assetId, Transform parent = null)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                Debug.LogError("[AssetManager] InstantiateAsync failed: assetId is empty.");
                return default;
            }

            var handle = parent == null
                ? Addressables.InstantiateAsync(assetId)
                : Addressables.InstantiateAsync(assetId, parent);

            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AssetManager] InstantiateAsync failed: {assetId} - {handle.OperationException}");
                if (handle.IsValid()) Addressables.Release(handle);
                return default;
            }

            EFrame.Current?.InjectInto(handle.Result);
            return new InstanceHandle(handle);
        }

        public async UniTask<InstanceHandle> InstantiateAsync(AssetReferenceGameObject reference, Transform parent = null)
        {
            if (reference == null)
            {
                Debug.LogError("[AssetManager] InstantiateAsync failed: reference is null.");
                return default;
            }

            var handle = parent == null
                ? Addressables.InstantiateAsync(reference)
                : Addressables.InstantiateAsync(reference, parent);

            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AssetManager] InstantiateAsync failed: {reference.RuntimeKey} - {handle.OperationException}");
                if (handle.IsValid()) Addressables.Release(handle);
                return default;
            }

            EFrame.Current?.InjectInto(handle.Result);
            return new InstanceHandle(handle);
        }

        public async UniTask<bool> IsValidPathAsync(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return false;

            var handle = Addressables.LoadResourceLocationsAsync(assetId);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            var isValid = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null && handle.Result.Count > 0;
            if (handle.IsValid()) Addressables.Release(handle);
            return isValid;
        }

        /// <summary>
        /// 检查资源路径是否有效
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>Addressable 路径是否有效</returns>
        public static bool IsValidPath(string path)
        {
            var handle = Addressables.LoadResourceLocationsAsync(path);
            var locations = handle.WaitForCompletion();
            var isValid = locations != null && locations.Count > 0;
            if (handle.IsValid()) Addressables.Release(handle);
            return isValid;
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 加载资源 (通用类型)
        /// </summary>
        /// <param name="assetId">资源地址</param>
        /// <returns>加载的资源对象</returns>
        public static Object LoadAsset(string assetId)
        {
            try
            {
                if (string.IsNullOrEmpty(assetId))
                {
                    Debug.LogError("[AssetManager] LoadAsset 失败: assetId 为空");
                    return null;
                }

                var handle = Addressables.LoadAssetAsync<Object>(assetId);
                handle.WaitForCompletion();

                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError($"[AssetManager] LoadAsset 失败: {assetId} - {handle.OperationException}");
                    return null;
                }

                return handle.Result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] LoadAsset 异常: {assetId} - {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 加载资源 (泛型，通过 AssetReference)
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="reference">资源引用</param>
        /// <returns>加载的资源对象</returns>
        public static T LoadAsset<T>(AssetReferenceT<T> reference) where T : Object
        {
            return Addressables.LoadAssetAsync<T>(reference).WaitForCompletion();
        }

        /// <summary>
        /// 加载资源 (泛型，通过路径)
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="assetId">资源地址</param>
        /// <returns>加载的资源对象</returns>
        public static T LoadAsset<T>(string assetId) where T : Object
        {
            try
            {
                if (string.IsNullOrEmpty(assetId))
                {
                    Debug.LogError("[AssetManager] LoadAsset 失败: assetId 为空");
                    return null;
                }

                var handle = Addressables.LoadAssetAsync<T>(assetId);
                handle.WaitForCompletion();

                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError($"[AssetManager] LoadAsset 失败: {assetId} - {handle.OperationException}");
                    return null;
                }

                return handle.Result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] LoadAsset 异常: {assetId} - {e.Message}");
                return null;
            }
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源 (通过 AssetReference)
        /// </summary>
        /// <param name="reference">资源引用</param>
        public static void UnloadAsset(AssetReference reference)
        {
            Addressables.Release(reference);
        }

        /// <summary>
        /// 释放资源 (通过资源对象)
        /// </summary>
        /// <param name="asset">资源对象</param>
        public static void UnloadAsset(Object asset)
        {
            Addressables.Release(asset);
        }

        /// <summary>
        /// 释放未使用的资源，触发垃圾回收
        /// </summary>
        public static void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        public void ReleaseUnusedAssets()
        {
            UnloadUnusedAssets();
        }

        private static GameObject BindInstance(GameObject go)
        {
            EFrame.Current?.InjectInto(go);
            return go;
        }

        #endregion

        #region 实例化 (Path)

        /// <summary>
        /// 实例化 GameObject
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(string assetPath)
        {
            return BindInstance(Addressables.InstantiateAsync(assetPath).WaitForCompletion());
        }

        /// <summary>
        /// 实例化 GameObject 并设置父节点
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="parent">父节点</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(string assetPath, Transform parent)
        {
            return BindInstance(Addressables.InstantiateAsync(assetPath, parent).WaitForCompletion());
        }

        /// <summary>
        /// 实例化 GameObject 并设置父节点和自动销毁时间
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="parent">父节点</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(string assetPath, Transform parent, float destroyTime)
        {
            var go = BindInstance(Addressables.InstantiateAsync(assetPath, parent).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置自动销毁时间
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(string assetPath, float destroyTime)
        {
            var go = BindInstance(Addressables.InstantiateAsync(assetPath).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置位置和自动销毁时间
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(string assetPath, Vector3 pos, float destroyTime)
        {
            var go = BindInstance(Addressables.InstantiateAsync(assetPath, pos, Quaternion.identity).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置父节点、位置和自动销毁时间
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(string assetPath, Transform parent, Vector3 pos, float destroyTime)
        {
            var go = BindInstance(Addressables.InstantiateAsync(assetPath, pos, Quaternion.identity, parent).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化并获取组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="assetPath">资源路径</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(string assetPath) where T : Component
        {
            var go = Instantiate(assetPath);
            return go?.GetComponent<T>();
        }

        /// <summary>
        /// 实例化并获取组件，设置父节点
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="assetPath">资源路径</param>
        /// <param name="parent">父节点</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(string assetPath, Transform parent) where T : Component
        {
            var go = Instantiate(assetPath, parent);
            return go?.GetComponent<T>();
        }

        #endregion

        #region 实例化 (AssetReference)

        /// <summary>
        /// 实例化 GameObject (通过 AssetReference)
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(AssetReferenceGameObject reference, float destroyTime = 0)
        {
            GameObject go = BindInstance(Addressables.InstantiateAsync(reference).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置位置 (通过 AssetReference)
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(AssetReferenceGameObject reference, Vector3 pos, float destroyTime = 0)
        {
            GameObject go = BindInstance(Addressables.InstantiateAsync(reference, pos, Quaternion.identity).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置位置和旋转 (通过 AssetReference)
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(AssetReferenceGameObject reference, Vector3 pos, Quaternion quaternion, float destroyTime = 0)
        {
            GameObject go = BindInstance(Addressables.InstantiateAsync(reference, pos, quaternion).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置父节点 (通过 AssetReference)
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(AssetReferenceGameObject reference, Transform parent, float destroyTime = 0)
        {
            GameObject go = BindInstance(Addressables.InstantiateAsync(reference, parent).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置父节点和位置 (通过 AssetReference)
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(AssetReferenceGameObject reference, Transform parent, Vector3 pos, float destroyTime = 0)
        {
            GameObject go = BindInstance(Addressables.InstantiateAsync(reference, pos, Quaternion.identity, parent).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化 GameObject 并设置父节点、位置和旋转 (通过 AssetReference)
        /// </summary>
        /// <param name="reference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化的 GameObject</returns>
        public static GameObject Instantiate(AssetReferenceGameObject reference, Transform parent, Vector3 pos, Quaternion quaternion,
            float destroyTime = 0)
        {
            GameObject go = BindInstance(Addressables.InstantiateAsync(reference, pos, quaternion, parent).WaitForCompletion());
            if (go != null && destroyTime > 0) Object.Destroy(go, destroyTime);
            return go;
        }

        /// <summary>
        /// 实例化并获取组件 (通过 AssetReference)
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="reference">资源引用</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(AssetReferenceGameObject reference, float destroyTime = 0) where T : Component
        {
            GameObject go = Instantiate(reference, destroyTime);
            return go?.GetComponent<T>();
        }

        /// <summary>
        /// 实例化并获取组件，设置父节点 (通过 AssetReference)
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="reference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(AssetReferenceGameObject reference, Transform parent, float destroyTime = 0) where T : Component
        {
            GameObject go = Instantiate(reference, parent, destroyTime);
            return go?.GetComponent<T>();
        }

        /// <summary>
        /// 实例化并获取组件，设置位置 (通过 AssetReference)
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="reference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(AssetReferenceGameObject reference, Vector3 pos, float destroyTime = 0) where T : Component
        {
            GameObject go = Instantiate(reference, pos, destroyTime);
            return go?.GetComponent<T>();
        }

        /// <summary>
        /// 实例化并获取组件，设置父节点和位置 (通过 AssetReference)
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="reference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(AssetReferenceGameObject reference, Transform parent, Vector3 pos, float destroyTime = 0)
            where T : Component
        {
            GameObject go = Instantiate(reference, parent, pos, destroyTime);
            return go?.GetComponent<T>();
        }

        /// <summary>
        /// 实例化并获取组件，设置父节点、位置和旋转 (通过 AssetReference)
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="reference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(AssetReferenceGameObject reference, Transform parent, Vector3 pos, Quaternion quaternion,
            float destroyTime = 0) where T : Component
        {
            GameObject go = Instantiate(reference, parent, pos, quaternion, destroyTime);
            return go?.GetComponent<T>();
        }

        /// <summary>
        /// 实例化并获取组件，设置位置和旋转 (通过 AssetReference)
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="reference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <param name="destroyTime">自动销毁时间 (秒)，0 表示不自动销毁</param>
        /// <returns>实例化对象上的组件</returns>
        public static T Instantiate<T>(AssetReferenceGameObject reference, Vector3 pos, Quaternion quaternion, float destroyTime = 0)
            where T : Component
        {
            GameObject go = Instantiate(reference, pos, quaternion, destroyTime);
            return go?.GetComponent<T>();
        }

        #endregion

        #region 对象池 (Path)

        /// <summary>
        /// 从对象池获取 GameObject (通过路径)
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="recycleTime">自动回收时间 (秒)，0 表示不自动回收</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(string assetPath, Transform parent, Vector3 pos, float recycleTime = 0)
        {
            GameObject go;
            if (m_pathPools.ContainsKey(assetPath) && m_pathPools[assetPath].Count > 0)
            {
                go = m_pathPools[assetPath].Pop();
                go.SetActive(true);
                go.transform.SetParent(parent);
                go.transform.localScale = Vector3.one;
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
            }
            else
            {
                go = BindInstance(Addressables.InstantiateAsync(assetPath, parent).WaitForCompletion());
                go.transform.localScale = Vector3.one;
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
            }

            if (recycleTime > 0)
            {
                EFrame.Current?.Coroutine?.DelayCall(recycleTime, () => { RecycleToPool(assetPath, go); });
            }
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject (通过路径)
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="recycleTime">自动回收时间 (秒)，0 表示不自动回收</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(string assetPath, Vector3 pos, float recycleTime = 0)
        {
            GameObject go;
            if (m_pathPools.ContainsKey(assetPath) && m_pathPools[assetPath].Count > 0)
            {
                go = m_pathPools[assetPath].Pop();
                go.SetActive(true);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
            }
            else
            {
                go = BindInstance(Addressables.InstantiateAsync(assetPath, pos, Quaternion.identity).WaitForCompletion());
            }

            if (recycleTime > 0)
            {
                EFrame.Current?.Coroutine?.DelayCall(recycleTime, () => { RecycleToPool(assetPath, go); });
            }
            return go;
        }

        /// <summary>
        /// 回收 GameObject 到对象池 (通过路径)
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="go">要回收的 GameObject</param>
        public static void RecycleToPool(string assetPath, GameObject go)
        {
            if (go == null) return;
            if (!m_pathPools.ContainsKey(assetPath)) m_pathPools.Add(assetPath, new Stack<GameObject>());

            if (m_pathPools[assetPath].Count >= MaxPoolSizePerKey)
            {
                ReleasePooledInstance(go);
                return;
            }

            go.transform.SetParent(null, false);
            go.SetActive(false);
            m_pathPools[assetPath].Push(go);
        }

        /// <summary>
        /// 释放指定路径的对象池
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        public static void ReleasePathPool(string assetPath)
        {
            if (m_pathPools.TryGetValue(assetPath, out var pool))
            {
                foreach (GameObject go in pool)
                {
                    ReleasePooledInstance(go);
                }
                pool.Clear();
                m_pathPools.Remove(assetPath);
            }
        }

        /// <summary>
        /// 释放所有路径对象池
        /// </summary>
        public static void ReleaseAllPathPools()
        {
            foreach (var kvp in m_pathPools)
            {
                foreach (GameObject go in kvp.Value)
                {
                    ReleasePooledInstance(go);
                }
                kvp.Value.Clear();
            }
            m_pathPools.Clear();
        }

        #endregion

        #region 对象池 (AssetReference)

        /// <summary>
        /// 从对象池获取 GameObject (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference)
        {
            if (m_pools.ContainsKey(assetReference) && m_pools[assetReference].Count > 0)
            {
                GameObject go = m_pools[assetReference].Pop();
                go.SetActive(true);
                return go;
            }

            return Instantiate(assetReference);
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置父节点 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Transform parent)
        {
            GameObject go = GetFromPool(assetReference);
            go.transform.SetParent(parent);
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置位置 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Vector3 pos)
        {
            GameObject go = GetFromPool(assetReference);
            go.transform.position = pos;
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置父节点和位置 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Transform parent, Vector3 pos)
        {
            GameObject go = GetFromPool(assetReference);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置位置和旋转 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Vector3 pos, Quaternion quaternion)
        {
            GameObject go = GetFromPool(assetReference);
            go.transform.SetPositionAndRotation(pos, quaternion);
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置父节点、位置和旋转 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Transform parent, Vector3 pos, Quaternion quaternion)
        {
            GameObject go = GetFromPool(assetReference);
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(pos, quaternion);
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置自动回收 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="recycleTime">自动回收时间 (秒)</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, float recycleTime)
        {
            GameObject go = GetFromPool(assetReference);
            RecycleToPool(assetReference, go, recycleTime).Forget();
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置父节点和自动回收 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="recycleTime">自动回收时间 (秒)</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Transform parent, float recycleTime)
        {
            GameObject go = GetFromPool(assetReference, parent);
            RecycleToPool(assetReference, go, recycleTime).Forget();
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置位置和自动回收 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="recycleTime">自动回收时间 (秒)</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Vector3 pos, float recycleTime)
        {
            GameObject go = GetFromPool(assetReference, pos);
            RecycleToPool(assetReference, go, recycleTime).Forget();
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置父节点、位置和自动回收 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="recycleTime">自动回收时间 (秒)</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Transform parent, Vector3 pos, float recycleTime)
        {
            GameObject go = GetFromPool(assetReference, parent, pos);
            RecycleToPool(assetReference, go, recycleTime).Forget();
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置位置、旋转和自动回收 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <param name="recycleTime">自动回收时间 (秒)</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Vector3 pos, Quaternion quaternion, float recycleTime)
        {
            GameObject go = GetFromPool(assetReference, pos, quaternion);
            RecycleToPool(assetReference, go, recycleTime).Forget();
            return go;
        }

        /// <summary>
        /// 从对象池获取 GameObject 并设置父节点、位置、旋转和自动回收 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="parent">父节点</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="quaternion">旋转</param>
        /// <param name="recycleTime">自动回收时间 (秒)</param>
        /// <returns>池中的 GameObject</returns>
        public static GameObject GetFromPool(AssetReferenceGameObject assetReference, Transform parent, Vector3 pos, Quaternion quaternion,
            float recycleTime)
        {
            GameObject go = GetFromPool(assetReference, parent, pos, quaternion);
            RecycleToPool(assetReference, go, recycleTime).Forget();
            return go;
        }

        /// <summary>
        /// 延迟回收 GameObject 到对象池 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="go">要回收的 GameObject</param>
        /// <param name="recycleTime">延迟时间 (秒)</param>
        public static async UniTask RecycleToPool(AssetReferenceGameObject assetReference, GameObject go, float recycleTime)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(recycleTime));
            RecycleToPool(assetReference, go);
        }

        /// <summary>
        /// 回收 GameObject 到对象池 (通过 AssetReference)
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        /// <param name="go">要回收的 GameObject</param>
        public static void RecycleToPool(AssetReferenceGameObject assetReference, GameObject go)
        {
            if (go == null) return;
            if (!m_pools.ContainsKey(assetReference)) m_pools.Add(assetReference, new Stack<GameObject>());

            if (m_pools[assetReference].Count >= MaxPoolSizePerKey)
            {
                ReleasePooledInstance(go);
                return;
            }

            go.transform.SetParent(null, false);
            go.SetActive(false);
            m_pools[assetReference].Push(go);
        }

        /// <summary>
        /// 释放指定 AssetReference 的对象池
        /// </summary>
        /// <param name="assetReference">资源引用</param>
        public static void ReleasePool(AssetReferenceGameObject assetReference)
        {
            if (m_pools.TryGetValue(assetReference, out var pool))
            {
                foreach (GameObject go in pool)
                {
                    ReleasePooledInstance(go);
                }
                pool.Clear();
                m_pools.Remove(assetReference);
            }
        }

        /// <summary>
        /// 释放所有 AssetReference 对象池
        /// </summary>
        public static void ReleaseAllPools()
        {
            foreach (var kvp in m_pools)
            {
                foreach (GameObject go in kvp.Value)
                {
                    ReleasePooledInstance(go);
                }
                kvp.Value.Clear();
            }
            m_pools.Clear();
        }

        void IAssetService.ReleaseAllPools()
        {
            ReleaseAllPathPools();
            ReleaseAllPools();
        }

        private static void ReleasePooledInstance(GameObject go)
        {
            if (go == null) return;

            if (!Addressables.ReleaseInstance(go))
            {
                Object.Destroy(go);
            }
        }

        public void Dispose()
        {
            ReleaseAllPools();
            ReleaseAllPathPools();
            Initialized = false;
            InitializeFailed = false;
        }

        #endregion
    }
}
