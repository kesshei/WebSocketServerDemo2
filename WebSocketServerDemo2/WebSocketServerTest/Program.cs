using WebSocketLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketServerTest2
{
    class Program
    {
        static WebSocketServer WebSocketServer;
        static List<UserToken> userTokens = new List<UserToken>();
        static void Main(string[] args)
        {
            Console.Title = "WebSocket Server Demo 2 蓝总创精英团队!";
            WebSocketServer = new WebSocketServer(5000);
            WebSocketServer.OnMessage += WebSocketServer_OnMessage;
            WebSocketServer.OnOpen += WebSocketServer_OnOpen;
            WebSocketServer.OnClose += WebSocketServer_OnClose;
            WebSocketServer.Listen();
            Console.WriteLine("开始监听!");
            Console.ReadLine();
        }

        private static void WebSocketServer_OnClose(UserToken userToken, byte[] data)
        {
            userTokens.Remove(userToken);
            Console.WriteLine($"{DateTime.Now} 用户退出，现有用户数:{userTokens.Count}");
        }

        private static void WebSocketServer_OnOpen(UserToken userToken, byte[] data)
        {
            userTokens.Add(userToken);
            Console.WriteLine($"{DateTime.Now} 登录用户数:{userTokens.Count}");
            //Task.Run(async () =>
            //{
            //    Thread.Sleep(5 * 1000);
            //    var websocket = userToken.WebSocket;
            //    int i = 0;
            //    while (true)
            //    {
            //        var str = Encoding.UTF8.GetBytes($"数据:({i})");
            //        if (websocket != null && websocket.State == WebSocketState.Open)
            //        {
            //            await smartWebSocketServer.Send(userToken, str);
            //        }
            //        i++;
            //        Thread.Sleep(500);
            //    }
            //});
        }

        private static void WebSocketServer_OnMessage(UserToken userToken, byte[] data)
        {
            Log(userToken, data);
        }
        private static void Log(UserToken userToken, byte[] data)
        {
            var str = string.Empty;
            if (data?.Length > 2000)
            {
                str = $"获取到信息长度:{data.Length}";
            }
            else
            {
                str = Encoding.UTF8.GetString(data);
                WebSocketServer.Send(userToken, Encoding.UTF8.GetBytes($"服务器已收到:{str}")).ConfigureAwait(true);
            }
            Console.WriteLine($"{DateTime.Now} {userToken.RemoteAddress} :{str}");
        }
    }
}
