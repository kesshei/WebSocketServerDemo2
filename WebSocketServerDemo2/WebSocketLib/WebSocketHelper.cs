using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketLib
{
    /// <summary>
    /// WebSocket帮助类
    /// </summary>
    public static class WebSocketHelper
    {
        /// <summary>
        /// 协议处理-http协议握手
        /// </summary>
        public static byte[] HandshakeMessage(string data)
        {
            /// <summary>
            /// 获取返回验证的key
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            static string getResponseKey(string key)
            {
                var MagicKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                if (string.IsNullOrEmpty(key))
                {
                    return string.Empty;
                }
                else
                {
                    key += MagicKey;
                    key = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key.Trim())));
                    return key;
                }
            }

            string key = string.Empty;
            string info = data;
            //一步一步来
            string[] list = info.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in list.Reverse())
            {
                if (item.IndexOf("Sec-WebSocket-Key") > -1)
                {
                    key = item.Split(new string[] { ": " }, StringSplitOptions.None)[1];
                    break;
                }
            }
            //获取标准的key
            key = getResponseKey(key);
            //拼装返回的协议内容
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + "\r\n");
            responseBuilder.Append("Upgrade: websocket" + "\r\n");
            responseBuilder.Append("Connection: Upgrade" + "\r\n");
            responseBuilder.Append("Sec-WebSocket-Accept: " + key + "\r\n\r\n");
            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }
    }
}
