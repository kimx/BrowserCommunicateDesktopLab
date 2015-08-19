using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DesktopApp
{
    //http://limitedcode.blogspot.tw/2014/05/websocket-websocket-server-console.html
    public delegate void ClientConnectedHandler(WebSocketConnection sender, EventArgs ev);
    public class WebSocketLoginListener
    {
        // Server端的Socket
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        // SHA1加密
        private SHA1 _sha1 = SHA1CryptoServiceProvider.Create();
        // WebSocket專用GUID                          
        private static readonly String GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        // 儲存所有Client連線的佇列             
        private List<WebSocketConnection> _connections = new List<WebSocketConnection>();
        // 建立連線後觸發的事件                       
        public event ClientConnectedHandler OnConnected;
        // 通訊埠
        public Int32 Port { get; private set; }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="port"></param>
        public WebSocketLoginListener(Int32 port)
        {
            Port = port;
        }

        /// <summary>
        /// 啟動WebSocket Server
        /// </summary>
        public void Start()
        {
            // 啟動Server Socket並監聽
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            _serverSocket.Listen(128);
            // Server Socket準備接收Client端連線
            _serverSocket.BeginAccept(new AsyncCallback(onConnect), null);
        }

        /// <summary>
        /// 當Client連線上進行的動作
        /// </summary>
        /// <param name="result"></param>
        private void onConnect(IAsyncResult result)
        {
            var clientSocket = _serverSocket.EndAccept(result);
            // 進行ShakeHand動作
            ShakeHands(clientSocket);
        }

        /// <summary>
        /// 進行HandShake
        /// </summary>
        /// <param name="socket"></param>
        private void ShakeHands(Socket socket)
        {
            // 存放Request資料的Buffer
            byte[] buffer = new byte[2048];//origin 1024 but too short
            // 接收的Request長度
            var length = socket.Receive(buffer);
            // 將buffer中的資料解碼成字串
            var data = Encoding.UTF8.GetString(buffer, 0, length);
            Console.WriteLine(data);

            // 將資料字串中的空白位元移除
            var dataArray = data.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            // 從Client傳來的Request Header訊息中取
            var key = dataArray.Where(s => s.Contains("Sec-WebSocket-Key: ")).Single().Replace("Sec-WebSocket-Key: ", String.Empty).Trim();
            var acceptKey = CreateAcceptKey(key);
            // WebSocket Protocol定義的ShakeHand訊息
            var handShakeMsg =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + acceptKey + "\r\n\r\n";

            socket.Send(Encoding.UTF8.GetBytes(handShakeMsg));

            Console.WriteLine(handShakeMsg);
            // 產生WebSocketConnection實體並加入佇列中管理
            var clientConn = new WebSocketConnection(socket);
            _connections.Add(clientConn);
            // 註冊Disconnected事件
            clientConn.OnDisconnected += new ClientDisconnectedEventHandler(DisconnectedWork);

            // 確認Connection是否繼續存在，並持續監聽
            if (OnConnected != null)
                OnConnected(clientConn, EventArgs.Empty);
            _serverSocket.BeginAccept(new AsyncCallback(onConnect), null);
        }

        /// <summary>
        /// DisConnected事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        private void DisconnectedWork(WebSocketConnection sender, EventArgs ev)
        {
            _connections.Remove(sender);
            sender.Close();
        }

        /// <summary>
        /// 產生HandShake的Socket Accept Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private String CreateAcceptKey(String key)
        {
            String keyStr = key + GUID;
            byte[] hashBytes = ComputeHash(keyStr);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 以SHA1進行加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte[] ComputeHash(String str)
        {
            return _sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
        }

        public void SendToAllClient(String data)
        {
            _connections.ForEach(c => c.Send(data));
        }

        public void SendToAllExceptSelf(String data, WebSocketConnection self)
        {
            _connections.Where(c => c != self).ToList().ForEach(c => c.Send(data));
        }

        public void SendToClient(String data, WebSocketConnection self)
        {
            _connections.Where(c => c == self).ToList().ForEach(c => c.Send(data));
        }
    }

    public delegate void DataReceivedEventHandler(WebSocketConnection sender, DataReceivedEventArgs ev);
    // 處理Disconnected的Event Handler
    public delegate void ClientDisconnectedEventHandler(WebSocketConnection sender, EventArgs ev);

    public class WebSocketConnection : IDisposable
    {
        
        private Socket _connection = null;
        // 存放資料的buffter
        private Byte[] _dataBuffer = new Byte[256];
        public event DataReceivedEventHandler OnDataReceived;
        public event ClientDisconnectedEventHandler OnDisconnected;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="socket"></param>
        public WebSocketConnection(Socket socket)
        {
            _connection = socket;
            listen();

        }

        /// <summary>
        /// 對該Client Socket監聽是否有資料傳遞
        /// </summary>
        private void listen()
        {
            _connection.BeginReceive(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, Read, null);
        }

        /// <summary>
        /// 讀取傳遞過來的資料封包進行解析
        /// </summary>
        /// <param name="result"></param>
        private void Read(IAsyncResult result)
        {
            var receivedSize = _connection.EndReceive(result);
            if (receivedSize > 2)
            {
                // 判斷是否為最後一個Frame(第一個bit為FIN若為1代表此Frame為最後一個Frame)，超過一個Frame暫不處理
                if (!((_dataBuffer[0] & 0x80) == 0x80))
                {
                    Console.WriteLine("Exceed 1 Frame. Not Handle");
                    return;
                }
                // 是否包含Mask(第一個bit為1代表有Mask)，沒有Mask則不處理
                if (!((_dataBuffer[1] & 0x80) == 0x80))
                {
                    Console.WriteLine("Exception: No Mask");
                    OnDisconnected(this, EventArgs.Empty);
                    return;
                }
                // 資料長度 = dataBuffer[1] - 127
                var payloadLen = _dataBuffer[1] & 0x7F;
                var masks = new Byte[4];
                var payloadData = filterPayloadData(ref payloadLen, ref masks);
                // 使用WebSocket Protocol中的公式解析資料
                for (var i = 0; i < payloadLen; i++)
                    payloadData[i] = (Byte)(payloadData[i] ^ masks[i % 4]);

                // 解析出的資料
                var content = Encoding.UTF8.GetString(payloadData);
                Console.WriteLine("Received Data: {0}", content);

                // 確認是否繼續接收資料，並持續監聽
                if (OnDataReceived != null)
                    OnDataReceived(this, new DataReceivedEventArgs(content));
                listen();
            }
            else
            {
                Console.WriteLine("Receive Error Data Frame");
                if (OnDisconnected != null)
                    OnDisconnected(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 判斷資料長度格式
        /// </summary>
        /// <param name="length"></param>
        /// <param name="masks"></param>
        /// <returns></returns>
        private Byte[] filterPayloadData(ref int length, ref Byte[] masks)
        {
            Byte[] payloadData;
            switch (length)
            {
                // 包含16 bit Extend Payload Length
                case 126:
                    Array.Copy(_dataBuffer, 4, masks, 0, 4);
                    length = (UInt16)(_dataBuffer[2] << 8 | _dataBuffer[3]);
                    payloadData = new Byte[length];
                    Array.Copy(_dataBuffer, 8, payloadData, 0, length);
                    break;
                // 包含 64 bit Extend Payload Length
                case 127:
                    Array.Copy(_dataBuffer, 10, masks, 0, 4);
                    var uInt64Bytes = new Byte[8];
                    for (int i = 0; i < 8; i++)
                    {
                        uInt64Bytes[i] = _dataBuffer[9 - i];
                    }
                    UInt64 len = BitConverter.ToUInt64(uInt64Bytes, 0);

                    payloadData = new Byte[len];
                    for (UInt64 i = 0; i < len; i++)
                        payloadData[i] = _dataBuffer[i + 14];
                    break;
                // 沒有 Extend Payload Length
                default:
                    Array.Copy(_dataBuffer, 2, masks, 0, 4);
                    payloadData = new Byte[length];
                    Array.Copy(_dataBuffer, 6, payloadData, 0, length);
                    break;
            }
            return payloadData;
        }

        public void Close()
        {
            _connection.Close();
        }

        /// <summary>
        /// 將要傳送的資料字串轉換成WebSocket Protocal中的傳送封包格式後送出
        /// </summary>
        /// <param name="data"></param>
        public void Send(Object data)
        {
            if (_connection.Connected)
            {
                try
                {
                    // 將資料字串轉成Byte
                    var contentByte = Encoding.UTF8.GetBytes(data.ToString());
                    var dataBytes = new List<Byte>();

                    if (contentByte.Length < 126)   // 資料長度小於126，Type1格式
                    {
                        // 未切割的Data Frame開頭
                        dataBytes.Add((Byte)0x81);
                        dataBytes.Add((Byte)contentByte.Length);
                        dataBytes.AddRange(contentByte);
                    }
                    else if (contentByte.Length <= 65535)       // 長度介於126與65535(0xFFFF)之間，Type2格式
                    {
                        dataBytes.Add((Byte)0x81);
                        dataBytes.Add((Byte)0x7E);              // 126
                        // Extend Data 加長至2Byte
                        dataBytes.Add((Byte)((contentByte.Length >> 8) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length) & 0xFF));
                        dataBytes.AddRange(contentByte);
                    }
                    else                 // 長度大於65535，Type3格式
                    {
                        dataBytes.Add((Byte)0x81);
                        dataBytes.Add((Byte)0x7F);              // 127
                        // Extned Data 加長至8Byte
                        dataBytes.Add((Byte)((contentByte.Length >> 56) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length >> 48) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length >> 40) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length >> 32) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length >> 24) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length >> 16) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length >> 8) & 0xFF));
                        dataBytes.Add((Byte)((contentByte.Length) & 0xFF));
                        dataBytes.AddRange(contentByte);
                    }
                    _connection.Send(dataBytes.ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (OnDisconnected != null)
                        OnDisconnected(this, EventArgs.Empty);
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        // OnReceive事件發生時傳入的資料字串
        public String Data { get; private set; }

        public DataReceivedEventArgs(String data)
        {
            Data = data;
        }
    }
}
