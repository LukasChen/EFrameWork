using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EFramework.Runtime.Networking
{
    public sealed class HttpHelper : MonoBehaviour
    {
        private const int TimeoutSeconds = 30;

        private static HttpHelper s_instance;
        private static bool s_appQuit;

        public static bool AppQuit => s_appQuit;

        public static HttpHelper Instance
        {
            get
            {
                if (s_appQuit)
                {
                    return null;
                }

                if (s_instance != null)
                {
                    return s_instance;
                }

                foreach (var helper in Resources.FindObjectsOfTypeAll<HttpHelper>())
                {
                    if (IsRuntimeInstance(helper))
                    {
                        s_instance = helper;
                        return s_instance;
                    }
                }

                var singleton = new GameObject("[HttpHelper]");
                s_instance = singleton.AddComponent<HttpHelper>();
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(singleton);
                }

                return s_instance;
            }
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            s_appQuit = false;
        }

        private void OnApplicationQuit()
        {
            s_appQuit = true;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }

            StopAllCoroutines();
        }

        public void Get(string url, IHttpRequestCall callback, Dictionary<string, string> paramDict = null, Dictionary<string, string> headerDict = null)
        {
            StartCoroutine(Send(UnityWebRequest.Get(AddParams(url, paramDict)), callback, headerDict));
        }

        public void Post(string url, List<IMultipartFormSection> formData, IHttpRequestCall callback, Dictionary<string, string> headerDict = null)
        {
            StartCoroutine(Send(UnityWebRequest.Post(url, formData), callback, headerDict));
        }

        public void Upload(string url, byte[] content, IHttpRequestCall callback, string contentType = "application/octet-stream")
        {
            StartCoroutine(UploadByPut(url, content, contentType, callback));
        }

        public void PostJson(string url, string content, Action<string> callback, Dictionary<string, string> headerDict = null)
        {
            StartCoroutine(SendJson(url, "POST", content, callback, headerDict));
        }

        public void PutJson(string url, string content, Action<string> callback, Dictionary<string, string> headerDict = null)
        {
            StartCoroutine(SendJson(url, "PUT", content, callback, headerDict));
        }

        private IEnumerator Send(UnityWebRequest request, IHttpRequestCall callback, Dictionary<string, string> headerDict)
        {
            using (request)
            {
                AddHeader(request, headerDict);
                request.timeout = TimeoutSeconds;
                yield return SendRequest(request, callback);
            }
        }

        private IEnumerator UploadByPut(string url, byte[] content, string contentType, IHttpRequestCall callback)
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
            request.uploadHandler = new UploadHandlerRaw(content ?? Array.Empty<byte>())
            {
                contentType = contentType
            };
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = TimeoutSeconds;
            yield return SendRequest(request, callback);
        }

        private IEnumerator SendJson(string url, string method, string content, Action<string> callback, Dictionary<string, string> headerDict)
        {
            using var request = new UnityWebRequest(url, method);
            byte[] body = Encoding.UTF8.GetBytes(content ?? string.Empty);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            AddHeader(request, headerDict);

            yield return request.SendWebRequest();

            if (IsRequestError(request))
            {
                Debug.LogWarning($"HttpHelper => {method} failed, url={request.url}, code={request.responseCode}, error={request.error}");
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(request.downloadHandler.text);
        }

        private static IEnumerator SendRequest(UnityWebRequest request, IHttpRequestCall callback)
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                callback?.OnProgress(request.downloadProgress);
                yield return null;
            }

            if (IsRequestError(request))
            {
                Debug.LogWarning($"HttpHelper => {request.method} failed, url={request.url}, code={request.responseCode}, error={request.error}");
                callback?.OnFail((int)request.responseCode, request.error);
                yield break;
            }

            callback?.OnProgress(1f);
            callback?.OnSuccess(request);
        }

        private static bool IsRequestError(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.ProtocolError
                || request.result == UnityWebRequest.Result.DataProcessingError;
        }

        private static void AddHeader(UnityWebRequest request, Dictionary<string, string> headerDict)
        {
            if (headerDict == null)
            {
                return;
            }

            foreach (var kvp in headerDict)
            {
                request.SetRequestHeader(kvp.Key, kvp.Value);
            }
        }

        private static string AddParams(string url, Dictionary<string, string> paramsDict)
        {
            if (paramsDict == null || paramsDict.Count == 0)
            {
                return url;
            }

            var query = new StringBuilder();
            foreach (var kvp in paramsDict)
            {
                if (query.Length > 0)
                {
                    query.Append('&');
                }

                query
                    .Append(Uri.EscapeDataString(kvp.Key))
                    .Append('=')
                    .Append(Uri.EscapeDataString(kvp.Value ?? string.Empty));
            }

            char separator = url.Contains("?") ? '&' : '?';
            return $"{url}{separator}{query}";
        }

        private static bool IsRuntimeInstance(HttpHelper helper)
        {
            return helper != null
                && helper.gameObject.scene.IsValid()
                && helper.gameObject.activeInHierarchy;
        }
    }

    public interface IHttpRequestCall
    {
        void OnSuccess(UnityWebRequest request);
        void OnFail(int code, string msg);
        void OnProgress(float progress);
    }

    public sealed class HttpRequestCallImpl : IHttpRequestCall
    {
        private readonly Action<int, string> m_failedCall;
        private readonly Action<string> m_successStringCall;
        private readonly Action<byte[]> m_successByteCall;
        private readonly Action<bool> m_successBoolCall;
        private readonly Action<UnityWebRequest> m_successRequestCall;
        private readonly Action<float> m_progressCall;

        public HttpRequestCallImpl(Action<string> successCall, Action<int, string> failedCall, Action<float> progressCall = null)
        {
            m_successStringCall = successCall;
            m_failedCall = failedCall;
            m_progressCall = progressCall;
        }

        public HttpRequestCallImpl(Action<byte[]> successCall, Action<int, string> failedCall, Action<float> progressCall = null)
        {
            m_successByteCall = successCall;
            m_failedCall = failedCall;
            m_progressCall = progressCall;
        }

        public HttpRequestCallImpl(Action<bool> successCall, Action<int, string> failedCall, Action<float> progressCall = null)
        {
            m_successBoolCall = successCall;
            m_failedCall = failedCall;
            m_progressCall = progressCall;
        }

        public HttpRequestCallImpl(Action<UnityWebRequest> successCall, Action<int, string> failedCall, Action<float> progressCall = null)
        {
            m_successRequestCall = successCall;
            m_failedCall = failedCall;
            m_progressCall = progressCall;
        }

        public void OnSuccess(UnityWebRequest request)
        {
            m_successStringCall?.Invoke(request.downloadHandler?.text);
            m_successByteCall?.Invoke(request.downloadHandler?.data);
            m_successBoolCall?.Invoke(true);
            m_successRequestCall?.Invoke(request);
        }

        public void OnFail(int code, string msg)
        {
            m_failedCall?.Invoke(code, msg);
        }

        public void OnProgress(float progress)
        {
            m_progressCall?.Invoke(progress);
        }
    }
}
