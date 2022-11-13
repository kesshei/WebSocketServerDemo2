# 回顾
之前已经写过关于《WebSocket 原生socket实现》和《.Net Core WebSocket 服务端与客户端完整示例》以及《基于.Net TcpListener 实现 WebSocketServer 通讯》。

其中除了 《.Net Core WebSocket 服务端与客户端完整示例》外，都是基于自己对Websocket协议的实现，这种实现在生产环境的时候，会有各种需要解决的问题，那么，就又回到了WebSocket的协议上去了。

所以，还是缺少一种轻量级的WebSocket客户端和服务端的支持，对于那些不想用第三方框架的人来说(第三方可能重，也可能复杂)，所以，一个官方支持websocket使用，才是最好的。

这样，就不用自己去实现握手啊，Websocket底层协议了，以及Websocket实现的大包分包了和所谓socket的粘包，半包了。

# 正视WebSocket协议

![](https://tupian.wanmeisys.com/markdown/1668316952998-5310bffe-8e2e-4790-ab09-91bdc63c7d70.png)

之前可能都是简单的使用，并没有太过认真的去瞧这个协议。

现在认真去看，才发现，其实WebSocket已经把所谓的大包分包和socket的粘包，掉包（半包）等问题通过它自己的协议给解决了。

Websocket 自己还实现了ping和pong心跳支持。

这对通过原生socket支持来讲，相当于已经有人把使用socket的顾虑都给解决了，还是通过标准协议解决的，那么，我们使用的时候，应该更加简单，方便才对，而不是更加的复杂，难用。

这是我对这个协议又深入研究后的结果。

# 用新的方法实现WebSocket服务端
这里我介绍除了 《.Net Core WebSocket 服务端与客户端完整示例》的另外三种实现方式，主要是针对服务端，客户端的话，直接用ClientWebSocket就搞定了。

## 原生socket支持WebSocket Server
大部分在刚开始实现的时候，因为找不到库，只能用第三方，或者自己使用协议实现。

但是，其实，也是可以直接把Socket 转为 WebSocket对象的，这个方法，我也是在看官方源码的时候，找到的，才知道它的用处（网上一搜，用的人真少。）

后来发现少的原因，原来这个方法的支持是在.NET Core 2.1版本之后才有的。

原生的实现都得自己实现握手协议，也很简单。

### WebSocketHelper.cs
```csharp
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
```

### 核心代码

主要是对 socket 转 websocket server对象部分。
```csharp
socket = ServerSocket.Accept();
WebSocket = WebSocket.CreateFromStream(new NetworkStream(socket), true, null, TimeSpan.FromSeconds(5));
```
原生socket接收到的客户端socket对象，直接转换就可以了。

## TcpListener 支持WebSocket Server
大致同上，同一个原理，都需要自己处理握手

```csharp
TcpClient s = listener.AcceptTcpClient();
WebSocket = WebSocket.CreateFromStream(s.GetStream(), true, null, TimeSpan.FromSeconds(5));
```

## HttpListener 方式实现 WebSocket Server
这种方式，是我认为最轻量化的实现方式，连握手都不需要处理。

```csharp
HttpListenerContext httpListenerContext = await this.listener.GetContextAsync();
HttpListenerWebSocketContext webSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
WebSocket = webSocketContext.WebSocket;
```
### 方式对比
加上以前的方案，目前一共有四种官方实现WebSocket Server 的方案。
1. asp.net core  useWebSocket
2. socket  WebSocket.CreateFromStream
3. tcpListener  WebSocket.CreateFromStream
4. HttpListener AcceptWebSocketAsync

我个人认为，最好的方式就是第一种和第四种。其他两种自己还要实现握手。

另外，第一种与第四种的区别是起服务的引擎不同。 asp.net core 是内置了 Kestrel 与 HTTP.sys 两种 webserver 服务引擎，而 HttpListener 可能就是默认的实现了 (具体也没查到)，相对来讲更轻量一些。

## 结果展示
测试的大致逻辑是
1. 先启动服务端，再启动客户端
2. 客户端循环发送数据，先发送19次文字，再发送一个633M的视频大文件
3. 服务端收到后，如果是文字，就回复给客户端，如果不是文字，就输出大文件的大小

这个逻辑用来测试，应该没啥大问题。

### socket 方案 1
![](https://tupian.wanmeisys.com/markdown/1668322680323-5adcada7-4436-4661-94ec-f99267c23a2c.png)
### tcpListener 方案 2
![](https://tupian.wanmeisys.com/markdown/1668322840966-abfa759c-0281-4f63-9137-93de1eb80600.png)
### HttpListener 方案 3
![](https://tupian.wanmeisys.com/markdown/1668327221294-d367843f-3123-4eb6-94e3-4359c1a1de2b.png)

## 需注意

特别是发送的操作，发送本身不能并发。

官方 资料引用 [2]
![](https://tupian.wanmeisys.com/markdown/1668327594873-9a9e3a83-a667-42e0-a4ac-80ad747687d3.png)

所以，在处理接收的时候，我们一般都是一个接收，但是，发送的地方经常被忽视。这个地方需要特殊处理的。

## 总结
>只有复杂的实际应用才能深入了解各个方面，果真是天降大任于斯人也，深入挖掘才能掌握更多知识。

>还是要勤于思考，勤于总结，我发现好多自己写过的东西都直接帮助到了我，这也许就是反哺己身吧。


## 资料引用

>https://learn.microsoft.com/zh-cn/dotnet/api/system.net.websockets.websocket.createfromstream

>https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket.sendasync

## 代码地址
https://github.com/kesshei/WebSocketServerDemo2.git

https://gitee.com/kesshei/WebSocketServerDemo2.git

