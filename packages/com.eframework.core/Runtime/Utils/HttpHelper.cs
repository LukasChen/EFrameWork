using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HttpHelper : MonoBehaviour
{
    #region Request Params

    private const int TIMEOUT = 30;

    #endregion

    #region Init

    private static HttpHelper m_instance;
    private static readonly object s_locker = new object();
    private static bool m_appQuit;
    public static bool AppQuit
    {
        get { return m_appQuit; }
        private set { m_appQuit = value; }
    }

    public static HttpHelper Instance
    {
        get
        {
            lock (s_locker)
            {
                if (m_instance == null)
                {
                    m_instance = FindFirstObjectByType<HttpHelper>();
                    if (FindObjectsByType<HttpHelper>(FindObjectsSortMode.None).Length > 1)
                    {
                        Debug.LogWarning($">>> HttpHelper >> 存在多个实例！");
                        return m_instance;
                    }

                    if (m_instance == null)
                    {
                        var singleton = new GameObject();
                        m_instance = singleton.AddComponent<HttpHelper>();
                        singleton.name = "[HttpHelper]";
                        singleton.hideFlags = HideFlags.None;
                        if (Application.isPlaying) DontDestroyOnLoad(singleton);
                    }
                }

                m_instance.hideFlags = HideFlags.None;
                return m_instance;
            }
        }
    }

    private void Awake()
    {
        m_appQuit = false;
    }

    private void OnDestroy()
    {
        m_appQuit = true;
        StopAllCoroutines();
    }

    #endregion

    #region Request Api

    public void Get(string url, IHttpRequestCall callback, Dictionary<string, string> paramDict = null, Dictionary<string, string> headerDict = null)
    {
        url = _AddParams(url, paramDict);
        StartCoroutine(_Get(url, callback, headerDict));
    }

    public void Post(string url, List<IMultipartFormSection> formData, IHttpRequestCall callback, Dictionary<string, string> headerDict = null)
    {
        StartCoroutine(_Post(url, formData, callback, headerDict));
    }

    public void Upload(string url, byte[] content, IHttpRequestCall callback)
    {
        StartCoroutine(_UploadByPut(url, content, callback));
    }

    public void PostJson(string url, string content, Action<string> callback, Dictionary<string, string> headerDict = null)
    {
        StartCoroutine(_PostString(url, content, callback, headerDict));
    }

    public void PutJson(string url, string content, Action<string> callback, Dictionary<string, string> headerDict = null)
    {
        StartCoroutine(_PutString(url, content, callback, headerDict));
    }

    #endregion

    #region Inner Request Methods

    IEnumerator _Get(string url, IHttpRequestCall callback, Dictionary<string, string> headerDict)
    {
        using (var uwr = UnityWebRequest.Get(url))
        {
            _AddHeader(uwr, headerDict);
            uwr.timeout = TIMEOUT;
            uwr.SendWebRequest();
            while (!uwr.isDone)
            {
                callback.OnProgress(uwr.downloadProgress);
                yield return null;
            }

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"HttpHelper => Get Request fail, url={uwr.url}, error={uwr.error}");
                callback.OnFail((int)uwr.responseCode, uwr.error);
            }
            else
            {
                // Debug.Log("HttpHelper => Get Request success, url=" + url);
                callback.OnProgress(1);
                callback.OnSuccess(uwr.downloadHandler.text);
                callback.OnSuccess(uwr.downloadHandler.data);
                callback.OnSuccess(uwr);
            }

            yield return null;
        }
    }

    IEnumerator _Post(string url, List<IMultipartFormSection> formData, IHttpRequestCall callback, Dictionary<string, string> headerDict)
    {
        using (var uwr = UnityWebRequest.Post(url, formData))
        {
            _AddHeader(uwr, headerDict);
            uwr.timeout = TIMEOUT;
            uwr.SendWebRequest();

            while (!uwr.isDone)
            {
                callback.OnProgress(uwr.downloadProgress);
                yield return null;
            }

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"HttpHelper => Get Request fail, url={uwr.url}, error={uwr.error}");
                callback.OnFail((int)uwr.responseCode, uwr.error);
            }
            else
            {
                Debug.Log("HttpHelper => Get Request success, url=" + url);
                callback.OnProgress(1);
                callback.OnSuccess(uwr.downloadHandler.text);
                callback.OnSuccess(uwr.downloadHandler.data);
                callback.OnSuccess(uwr);
            }

            yield return null;
        }
    }

    public IEnumerator _PutString(string url, string param, Action<string> callback, Dictionary<string, string> headerDict = null)
    {
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "PUT"))
        {
            Debug.Log(url + ",http request params=" + param);
            byte[] jsonToSend = new UTF8Encoding().GetBytes(param);
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            if (headerDict == null) webRequest.SetRequestHeader("Content-Type", "application/json");
            _AddHeader(webRequest, headerDict);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(url + ",http response=" + webRequest.downloadHandler.text);
                callback?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                Debug.Log(url + ",http response=" + webRequest.result);
                callback?.Invoke(null);
            }
        }
    }

    IEnumerator _UploadByPut(string url, byte[] content, IHttpRequestCall callback, string contentType = "application/octet-stream")
    {
        var uwr = new UnityWebRequest(url);
        var uploadHandler = new UploadHandlerRaw(content);
        uploadHandler.contentType = contentType;
        uwr.uploadHandler = uploadHandler;
        uwr.SendWebRequest();

        while (!uwr.isDone)
        {
            callback.OnProgress(uwr.downloadProgress);
            yield return null;
        }

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogWarning($"HttpHelper => Get Request fail, url={uwr.url}, error={uwr.error}");
            callback.OnFail((int)uwr.responseCode, uwr.error);
        }
        else
        {
            Debug.Log("HttpHelper => Get Request success, url=" + url);
            callback.OnProgress(1);
            callback.OnSuccess(true);
        }

        yield return null;
    }

    public IEnumerator _PostString(string url, string param, Action<string> callback, Dictionary<string, string> headerDict = null)
    {
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            Debug.Log(url + ",http request params=" + param);
            byte[] jsonToSend = new UTF8Encoding().GetBytes(param);
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            if (headerDict == null) webRequest.SetRequestHeader("Content-Type", "application/json");
            _AddHeader(webRequest, headerDict);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(url + ",http response=" + webRequest.downloadHandler.text);
                callback?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                Debug.Log(url + ",http response=" + webRequest.result);
                callback?.Invoke(null);
            }
        }
    }

    private static void _AddHeader(UnityWebRequest request, Dictionary<string, string> headerDict)
    {
        if (headerDict == null) return;
        foreach (string key in headerDict.Keys) request.SetRequestHeader(key, headerDict[key]);
    }

    private static string _AddParams(string url, Dictionary<string, string> paramsDict)
    {
        if (paramsDict == null) return url;
        StringBuilder query = new StringBuilder();
        foreach (string key in paramsDict.Keys)
        {
            if (query.Length > 0) query.Append("&");
            query.Append(key).Append("=").Append(paramsDict[key]);
        }

        return url + query;
    }

    #endregion
}

public interface IHttpRequestCall
{
    void OnSuccess(string resp);
    void OnSuccess(byte[] resp);
    void OnSuccess(bool result);
    void OnSuccess(UnityWebRequest result);
    void OnFail(int code, string msg);
    void OnProgress(float progress);
}

public class HttpRequestCallImpl : IHttpRequestCall
{
    private readonly Action<int, string> m_failedCall;
    private readonly Action<string> m_successStringCall;
    private readonly Action<byte[]> m_successByteCall;
    private readonly Action<bool> m_successBoolCall;
    private readonly Action<UnityWebRequest> m_successUwrCall;
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
        m_successUwrCall = successCall;
        m_failedCall = failedCall;
        m_progressCall = progressCall;
    }

    public void OnSuccess(string resp)
    {
        m_successStringCall?.Invoke(resp);
    }

    public void OnSuccess(byte[] resp)
    {
        m_successByteCall?.Invoke(resp);
    }

    public void OnSuccess(bool result)
    {
        m_successBoolCall?.Invoke(result);
    }

    public void OnSuccess(UnityWebRequest result)
    {
        m_successUwrCall?.Invoke(result);
    }

    public void OnFail(int code, string msg)
    {
        m_failedCall.Invoke(code, msg);
    }

    public void OnProgress(float progress)
    {
        m_progressCall?.Invoke(progress);
    }
}