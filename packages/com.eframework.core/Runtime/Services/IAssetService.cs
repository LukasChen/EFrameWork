using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace EFrameWork.Runtime.Asset
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

    public interface IAssetService : IDisposable
    {
        bool Initialized { get; }
        bool InitializeFailed { get; }
        UniTask<bool> InitializeAsync();
        UniTask<AssetHandle<T>> LoadAsync<T>(string assetId) where T : Object;
        UniTask<AssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference) where T : Object;
        UniTask<InstanceHandle> InstantiateAsync(string assetId, Transform parent = null);
        UniTask<InstanceHandle> InstantiateAsync(AssetReferenceGameObject reference, Transform parent = null);
        void ReleaseUnusedAssets();
    }
}
