/*
 * @Author: layne
 * @Date:  2025-10-23 10:43
 * @LastEditTime: 2025-10-23 10:43
 * @LastEditors: layne
 */

using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.Networking;

namespace GameLogic
{
    public static partial class Tool
    {
        /// <summary>
        /// http请求的实用函数。
        /// </summary>
        public static class Http
        {
            
            /// <summary>
            /// 发送json格式请求
            /// </summary>
            public static async UniTask<String> RequestJson(string url, Dictionary<string, string> headerDic, string jsonData, string method = "POST")
            {
                Log.Info($" 请求路径: @{url}");
                if (!string.IsNullOrEmpty(jsonData) && method == "POST")
                {
                    Log.Info($" 请求Body: {jsonData}");
                }
                using (var www = new UnityWebRequest(url, method))
                {
                    if (method == "POST" && !string.IsNullOrEmpty(jsonData))
                    {
                        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    }
                    www.downloadHandler = new DownloadHandlerBuffer();
                    www.SetRequestHeader("Accept", "application/json");
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Cookie", "");
                    foreach (var item in headerDic)
                    {
                        www.SetRequestHeader(item.Key, item.Value);
                    }
                    await www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        return www.downloadHandler.text;
                    }
                    else
                    {
                        Log.Warning(www.error);
                        return string.Empty;
                    }
                }
            }
            
            /// <summary>
            /// 发送Protobuf格式请求
            /// </summary>
            public static async UniTask<byte[]> RequestProtobuf(string url, Dictionary<string, string> headerDic, byte[] data)
            {
                Log.Info($" 请求路径: @{url}");
                using (var www = new UnityWebRequest(url, "POST"))
                {
                    www.uploadHandler = new UploadHandlerRaw(data);
                    www.downloadHandler = new DownloadHandlerBuffer();
                    www.SetRequestHeader("Content-Type", "application/x-protobuf");
                    www.SetRequestHeader("Accept", "application/x-protobuf");
                    www.SetRequestHeader("Cookie", "");
                    foreach (var item in headerDic)
                    {
                        www.SetRequestHeader(item.Key, item.Value);
                    }
                    await www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        return www.downloadHandler.data;
                    }
                    else
                    {
                        Log.Warning(www.error);
                        return null;
                    }
                }
            }
            
            /// <summary>
            /// 发送HTTP请求
            /// </summary>
            /// <returns>响应结果</returns>
            public static async UniTask<String> RequestUpload(string url, Dictionary<string, string> headerDic, byte[] fileData)
            {
                Log.Info($" 请求路径: @{url}");
                WWWForm form = new WWWForm();
                form.AddBinaryData("file", fileData, "image.jpg", "image/jpeg");
                using (var www = UnityWebRequest.Post(url, form))
                {
                    www.downloadHandler = new DownloadHandlerBuffer();
                    foreach (var item in headerDic)
                    {
                        www.SetRequestHeader(item.Key, item.Value);
                    }
                    await www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        return www.downloadHandler.text;
                    }
                    else
                    {
                        Log.Warning(www.error);
                        return string.Empty;
                    }
                }
            }
        }
    }
}
