using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesktopApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string CurrentAccount;
        WebSocketLoginListener _webSocketLoginListener;
        public MainWindow()
        {
            InitializeComponent();

            WebSocketListener();
        }

        #region WebSocketListener
        private void WebSocketListener()
        {
            this._webSocketLoginListener = new WebSocketLoginListener(1105);
            this._webSocketLoginListener.OnConnected += _webSocketLoginListener_OnConnected;
            this._webSocketLoginListener.Start();//第一次防火牆會要求可以通過
        }

        WebSocketConnection webSocketConnection;

        private void _webSocketLoginListener_OnConnected(WebSocketConnection sender, EventArgs ev)
        {
            webSocketConnection = sender;
            sender.OnDataReceived += Sender_OnDataReceived;

        }

        private void Sender_OnDataReceived(WebSocketConnection sender, DataReceivedEventArgs ev)
        {

            _webSocketLoginListener.SendToClient(CurrentAccount + "-" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), sender);
        }
        #endregion

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random(DateTime.Now.Millisecond);//亂數種子
            var i = random.NextDouble() * random.Next(1, 100);
            _webSocketLoginListener.SendToClient($"{i} KG", webSocketConnection);

        }
    }
}
