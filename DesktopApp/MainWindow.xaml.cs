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
        HttpLoginListener _httpLoginListener;
        WebSocketLoginListener _webSocketLoginListener;
        public MainWindow()
        {
            InitializeComponent();
            this.gdDisplay.IsEnabled = false;
            this._httpLoginListener = new HttpLoginListener();

            //WebSocketListener();
        }

        #region WebSocketListener
        private void WebSocketListener()
        {
            this._webSocketLoginListener = new WebSocketLoginListener(1105);
            this._webSocketLoginListener.OnConnected += _webSocketLoginListener_OnConnected;
            this._webSocketLoginListener.Start();//第一次防火牆會要求可以通過
        }

        private void _webSocketLoginListener_OnConnected(WebSocketConnection sender, EventArgs ev)
        {
            sender.OnDataReceived += Sender_OnDataReceived;
        }

        private void Sender_OnDataReceived(WebSocketConnection sender, DataReceivedEventArgs ev)
        {
            _webSocketLoginListener.SendToClient(CurrentAccount + "-" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), sender);
        }
        #endregion


        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            this.lblWelcome.Content = string.Format("Welcome {0} !", txtAccount.Text);
            CurrentAccount = txtAccount.Text;
            this.gdDisplay.IsEnabled = true;
            this.gdLogin.IsEnabled = false;
            this._httpLoginListener.Start("http://localhost:60099/");
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            this.lblWelcome.Content = "";
            this.gdDisplay.IsEnabled = false;
            this.gdLogin.IsEnabled = true;
            this._httpLoginListener.Close();
        }
    }
}
