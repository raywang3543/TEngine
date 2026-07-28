/*
 * @Author: layne
 * @Date:  2025-10-23 10:43
 * @LastEditTime: 2025-10-23 10:43
 * @LastEditors: layne
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TEngine;

namespace GameLogic
{
    public static partial class Tool
    {
        /// <summary>
        /// 字符相关的实用函数。
        /// </summary>
        public static class Text
        {
            
            public static string AppendHttpUrl(string url, params (string key, string value)[] parameters)
            {
                if (string.IsNullOrEmpty(url))
                {
                    Log.Debug("URL is null or empty.");
                    return string.Empty;
                }
                if (parameters == null || parameters.Length == 0)
                {
                    return url;
                }
                var sb = new StringBuilder();
                sb.Append(url);
                sb.Append("?");
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) sb.Append("&");
                    sb.Append(Uri.EscapeDataString(parameters[i].key));
                    sb.Append("=");
                    sb.Append(Uri.EscapeDataString(parameters[i].value ?? string.Empty));
                }
                return sb.ToString();
            }
            
        }
    }
}
