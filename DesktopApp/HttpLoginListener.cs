using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DesktopApp
{
    public class HttpLoginListener
    {
        HttpListener _listener;
        public void Start(string urlPrefix)
        {
            this._listener = new HttpListener();
            this._listener.Prefixes.Add(urlPrefix);
            this._listener.Start();
            this._listener.BeginGetContext(new AsyncCallback(ListenerCallback), null);
        }
        public void ListenerCallback(IAsyncResult result)
        {
            if (_listener == null)
                return;
            HttpListenerContext context = _listener.EndGetContext(result);
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string responseString = string.Format("\"{0}\"", MainWindow.CurrentAccount) ;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "application/json";
                response.AddHeader("Access-Control-Allow-Origin", "*");//Cross site setting 。
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //繼續接收通知
                this._listener.BeginGetContext(new AsyncCallback(ListenerCallback), this._listener);
            }

        }

        public void Close()
        {
            this._listener.Close();
            this._listener = null;
        }
    }
}
