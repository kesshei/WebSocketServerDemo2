using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace WebSocketClient
{
    class Program
    {
        //定义处理程序委托
        public delegate bool ConsoleCtrlDelegate(int ctrlType);
        //导入SetCtrlHandlerHandler API
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        //当用户关闭Console时，系统会发送次消息
        private const int CTRL_CLOSE_EVENT = 2;
        //Ctrl+C，系统会发送次消息
        private const int CTRL_C_EVENT = 0;
        //Ctrl+break，系统会发送次消息
        private const int CTRL_BREAK_EVENT = 1;
        //用户退出（注销），系统会发送次消息
        private const int CTRL_LOGOFF_EVENT = 5;
        //系统关闭，系统会发送次消息
        private const int CTRL_SHUTDOWN_EVENT = 6;
        private static WebSocket WebSocket;
        static async Task Main(string[] args)
        {
            ConsoleCtrlDelegate consoleDelegete = new ConsoleCtrlDelegate(HandlerRoutineAsync);
            var FilePath = @"H:\阿里云\唐朝诡事录_1080P_31.mp4";
            bool bRet = SetConsoleCtrlHandler(consoleDelegete, true);
            Console.Title = "WebSocket Client Demo 蓝总创精英团队!";
            WebSocket = await CreateAsync("ws://localhost:5000");
            if (WebSocket != null)
            {
                Console.WriteLine("服务开始执行!");
                //接收到的数据的任务
                _ = Task.Run(async () =>
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024 * 1);
                    try
                    {
                        var bufferData = new List<byte>();
                        while (WebSocket.State == WebSocketState.Open)
                        {
                            var result = await WebSocket.ReceiveAsync(buffer, CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            }
                            bufferData.AddRange(buffer.AsSpan(0, result.Count).ToArray());
                            if (result.EndOfMessage)
                            {
                                var str = Encoding.UTF8.GetString(bufferData.ToArray());
                                Console.WriteLine(str);
                                bufferData.Clear();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                });
                //监控Websocket状态的任务
                _ = Task.Run(() =>
                {
                    while (true)
                    {
                        Console.WriteLine($"WebSocket状态:{WebSocket?.State}");
                        Thread.Sleep(10 * 1000);
                    }
                });
                //发送文本和文件的任务
                _ = Task.Run(async () =>
                {
                    int i = 0;
                    while (true)
                    {
                        try
                        {
                            if (WebSocket.State == WebSocketState.Open)
                            {
                                if (i != 0 && i % 20 == 0)
                                {
                                    var data = File.ReadAllBytes(FilePath);
                                    await WebSocket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
                                    Console.WriteLine($"{DateTime.Now} {i} 发送大文件:{data.Length}");
                                }
                                else
                                {
                                    var bytes = Encoding.UTF8.GetBytes($"文字 {i}");
                                    await WebSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                                i++;
                            }
                            else
                            {
                                Console.WriteLine($"未连接好!");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                });
                Console.WriteLine("服务执行完毕!");
            }
            else
            {
                Console.WriteLine("服务连接失败!");
            }
            var text = string.Empty;
            while (text != "exit")
            {
                text = Console.ReadLine();
                if (text == "close")
                {
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }
        /// <summary>
        /// 处理程序例程，在这里编写对指定事件的处理程序代码
        /// 注意：在VS中调试执行时，在这里设置断点，但不会中断；会提示：无可用源；
        /// </summary>
        ///<param name="CtrlType">
        /// <returns></returns>
        private static bool HandlerRoutineAsync(int ctrlType)
        {
            WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            switch (ctrlType)
            {
                case CTRL_C_EVENT:
                    Console.WriteLine("C");
                    return true; //这里返回true，表示阻止响应系统对该程序的操作
                                 //break;
                case CTRL_BREAK_EVENT:
                    Console.WriteLine("BREAK");
                    break;
                case CTRL_CLOSE_EVENT:
                    Console.WriteLine("CLOSE");
                    break;
                case CTRL_LOGOFF_EVENT:
                    Console.WriteLine("LOGOFF");
                    break;
                case CTRL_SHUTDOWN_EVENT:
                    Console.WriteLine("SHUTDOWN");
                    break;
            }
            //return true;//表示阻止响应系统对该程序的操作
            return false;//忽略处理，让系统进行默认操作
        }
        /// <summary>
        /// 创建客户端实例
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ClientWebSocket> CreateAsync(string ServerUri)
        {
            var webSocket = new ClientWebSocket();
            webSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };

            await webSocket.ConnectAsync(new Uri(ServerUri), CancellationToken.None);
            if (webSocket.State == WebSocketState.Open)
            {
                return webSocket;
            }
            return null;
        }
    }
}
