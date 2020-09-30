using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace WebsockAppLab
{
    /// <summary>
    /// WebSocketSharp
    /// https://dotblogs.com.tw/EganBlog/2019/05/25/WebSocket_Vue_WebSocketSharp
    /// </summary>
    class Program
    {
        //Kim 純發送版
        static void Main(string[] args)
        {
            CultureInfo culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            //WebSocketServer 監聽 PORT 55688
            var wssv = new UgozWebSocketService(55688);
            wssv.Start();
            var msgCreater = MsgThread(wssv);
            //WebSocketServer 停止行為
            Console.WriteLine("\nPress Enter key to stop the server...");
            Console.ReadLine();
            msgCreater.Abort();// 停用廣播訊息產生執行緒
            wssv.Stop();
        }

        static Thread MsgThread(UgozWebSocketService wssv)
        {
            var msgCreater = new Thread(() =>
            {
                while (true)
                {
                    // MessageQueueSingleton.Instance().AddMsg();
                    Random random = new Random(DateTime.Now.Millisecond);//亂數種子
                    var kg = random.NextDouble() * random.Next(1, 100);
                    wssv.SendScales(kg.ToString("0.000"));
                    Thread.Sleep(3000);
                }
            });
            msgCreater.Start();
            return msgCreater;
        }



        
    }
}
