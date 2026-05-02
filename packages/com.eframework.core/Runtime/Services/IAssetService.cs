using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace EFrame.Runtime.Asset
{
    public readonly struct AssetHandle<T> : IDisposable where T : Object
    {
        private readonly AsyncOperationHandle<T> m_handle;

        public AssetHandle(AsyncOperationHandle<T> handle)
        {
            m_handle = handle;
        }

        public T Asset => m_handle.IsValid() ? m_handle.Result : null;
        public bool IsValid => m_handle.IsValid();

        public void Dispose()
        {
            if (m_handle.IsValid())
            {
                Addressables.Release(m_handle);
            }
        }
    }

    public readonly struct InstanceHandle : IDisposable
    {
        private readonly AsyncOperationHandle<GameObject> m_handle;

        public InstanceHandle(AsyncOperationHandle<GameObject> handle)
        {
            m_handle = handle;
        }

        public GameObject GameObject => m_handle.IsValid() ? m_handle.Result : null;
        public bool IsValid => m_handle.IsValid();

        public void Dispose()
        {
            if (m_handle.IsValid())
            {
                Addressables.ReleaseInstance(m_handle);
            }
        }
    }

    public interface IAssetPreloadScope : IDisposable
    {
        UniTask<bool> PreloadAsync<T>(string assetId) where T : Object;
        bool Contains(string assetId);
        void ReleaseAll();
    }

    public interface IAssetService : IDisposable
    {
        bool Initialized { get; }
        bool InitializeFailed { get; }
        UniTask<bool> InitializeAsync();
        IAssetPreloadScope CreatePreloadScope();
        UniTask<AssetHandle<T>> LoadAsync<T>(string assetId) where T : Object;
        UniTask<bool> PreloadAssetAsync<T>(string assetId) where T : Object;
        bool TryGetPreloadedAsset<T>(string assetId, out T asset) where T : Object;
        void ReleasePreloadedAsset(string assetId);
        void ReleaseAllPreloadedAssets();
        GameObject Instantiate(string assetId, Transform parent = null);
        GameObject GetFromPool(string assetId, Transform parent, Vector3 position, float recycleTime = 0f);
        GameObject GetFromPool(string assetId, Vector3 position, float recycleTime = 0f);
        void RecycleToPool(string assetId, GameObject gameObject);
        void ReleasePathPool(string assetId);
        UniTask<InstanceHandle> InstantiateAsync(string assetId, Transform parent = null);
        UniTask<bool> IsValidPathAsync(string assetId);
        void ReleaseAllPools();
        void ReleaseUnusedAssets();
    }
}
