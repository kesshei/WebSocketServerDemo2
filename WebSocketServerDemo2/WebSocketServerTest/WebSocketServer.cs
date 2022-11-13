using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebSocketLib;

namespace WebSocketServerTest2
{
    /// <summary>
    /// WebSocketServer
    /// </summary>
    public class WebSocketServer
    {
        /// <summary>
        /// 核心监听方法
        /// </summary>
        TcpListener listener;
        /// <summary>
        /// 服务端监听的端口  作为服务端口
        /// </summary>
        public int ListenPort;
        /// <summary>
        /// 监听的端口
        /// </summary>
        /// <param name="port"></param>
        public WebSocketServer(int port)
        {
            this.ListenPort = port;
        }
        /// <summary>
        /// websocket 事件
        /// </summary>
        /// <param name="UserToken"></param>
        public delegate void WebSocketHandler(UserToken userToken, byte[] data);
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event WebSocketHandler OnOpen;
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event WebSocketHandler OnClose;
        /// <summary>
        /// 新用户的事件
        /// </summary>
        public event WebSocketHandler OnMessage;
        /// <summary>
        /// 开始监听
        /// </summary>
        /// <returns></returns>
        public WebSocketServer Listen()
        {
            listener = new TcpListener(IPAddress.Any, this.ListenPort);
            listener.Start();
            ServerStart();
            Task.Run(() =>
            {
                while (true)
                {
                    TcpClient s = listener.AcceptTcpClient();
                    //来一个新的链接
                    ThreadPool.QueueUserWorkItem(r => { _ = Accept(s); });
                }
            });
            return this;
        }
        /// <summary>
        /// 一个新的连接
        /// </summary>
        /// <param name="s"></param>
        public async Task Accept(TcpClient s)
        {
            var userToken = WebSocketHandshake(s);
            try
            {
                newAcceptHandler(userToken);
                var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024 * 1);
                try
                {
                    var WebSocket = userToken.WebSocket;
                    var bufferData = new List<byte>();
                    if (userToken.IsWebSocket)
                    {
                        while (WebSocket != null && WebSocket.State == WebSocketState.Open)
                        {
                            var result = await WebSocket.ReceiveAsync(buffer, CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await userToken.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            }
                            bufferData.AddRange(buffer.AsSpan(0, result.Count).ToArray());
                            if (result.EndOfMessage)
                            {
                                var data = bufferData.ToArray();
                                _ = Task.Run(() =>
                                {
                                    OnMessage?.Invoke(userToken, data);
                                });
                                bufferData.Clear();
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                s.Close();//客户端连接关闭
                newQuitHandler(userToken);
            }
        }
        private UserToken WebSocketHandshake(TcpClient s)
        {
            BinaryReader rs = new BinaryReader(s.GetStream());
            UserToken userToken = new UserToken();
            userToken.ConnectSocket = s.Client;
            userToken.ConnectTime = DateTime.Now;
            userToken.RemoteAddress = s.Client.RemoteEndPoint;
            userToken.IPAddress = ((IPEndPoint)(userToken.RemoteAddress)).Address;
            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024 * 1);
            try
            {
                var length = rs.Read(buffer, 0, buffer.Length);
                string info = Encoding.UTF8.GetString(buffer.Take(length).ToArray());
                if (info.IndexOf("websocket") > -1)
                {
                    var send = userToken.ConnectSocket.Send(WebSocketHelper.HandshakeMessage(info));
                    if (send > 0)
                    {
                        userToken.IsWebSocket = true;
                        userToken.WebSocket = WebSocket.CreateFromStream(s.GetStream(), true, null, TimeSpan.FromSeconds(5));
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return userToken;
        }
        /// <summary>
        /// 新的链接
        /// </summary>
        public void newAcceptHandler(UserToken userToken)
        {
            if (OnOpen != null)
            {
                OnOpen(userToken, null);
            }
            Console.WriteLine("一个新的用户:" + userToken.RemoteAddress.ToString());
        }
        /// <summary>
        /// 服务开始
        /// </summary>
        public void ServerStart()
        {
            Console.WriteLine("服务开启:local:" + this.ListenPort);
        }
        /// <summary>
        /// 用户退出
        /// </summary>
        public void newQuitHandler(UserToken userToken)
        {
            if (OnClose != null)
            {
                OnClose(userToken, null);
            }
            Console.WriteLine("用户退出:" + userToken.RemoteAddress.ToString());
        }
        /// <summary>
        /// 对客户发送数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> Send(UserToken token, string data)
        {
            return await Send(token, Encoding.UTF8.GetBytes(data));
        }
        /// <summary>
        /// 对客户发送数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> Send(UserToken token, byte[] data)
        {
            int length = -1;
            try
            {
                if (token.WebSocket != null && token.WebSocket.State == WebSocketState.Open)
                {
                    await token.WebSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                    length = data.Length;
                }
            }
            catch (Exception)
            { }
            return length;
        }
    }
}
