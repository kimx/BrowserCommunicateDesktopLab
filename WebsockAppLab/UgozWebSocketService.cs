using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebsockAppLab
{
    public class UgozWebSocketService
    {
        private WebSocketServer _webSocketServer;
        public UgozWebSocketService(int port)
        {
            _webSocketServer = new WebSocketServer(port);
            _webSocketServer.AddWebSocketService<UgozBroadcastBehavior>("/scales");
        }

        public void Start()
        {
            _webSocketServer.Start();
            if (_webSocketServer.IsListening)
            {
                Console.WriteLine("Listening on port {0}, and providing WebSocket services:", _webSocketServer.Port);
                foreach (var path in _webSocketServer.WebSocketServices.Paths)
                {
                    Console.WriteLine("- {0}", path);
                }
            }
        }

        public void Stop()
        {
            _webSocketServer.Stop();
        }

        public void SendScales(string value)
        {
            _webSocketServer.WebSocketServices["/scales"].Sessions.Broadcast(value);
        }


    }



    public class UgozBroadcastBehavior : WebSocketBehavior
    {
        /// <summary>
        /// 建構子
        /// </summary>
        public UgozBroadcastBehavior() { }

        //收到通知事件
        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            Console.WriteLine(e.Data);
            Send("I got your message : " + DateTime.Now);//針對目前的Session作回覆
        }
    }
}
