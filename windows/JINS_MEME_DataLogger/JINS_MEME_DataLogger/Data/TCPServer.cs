using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// ＴＣＰサーバー通信クラス
    /// </summary>
    public class TCPServer
    {
        /// <summary>
        /// ソケット状態イベント定義
        /// </summary>
        /// <param name="progress"></param>
        public delegate void SocketStatusHandler(string status);
        /// <summary>
        /// ソケット状態通知イベント
        /// </summary>
        public event SocketStatusHandler SocketStatusEvent = null;

        
        /// <summary>
        /// ＩＰアドレス
        /// </summary>
        private string ipAddress = string.Empty;
        public string IpAddress
        {
            get { return this.ipAddress.ToString(); }
            set { this.ipAddress = value; }
        }

        /// <summary>
        /// ポート番号
        /// </summary>
        private int socketPort = 60000;
        public int SocketPort
        {
            get { return this.socketPort; }
            set { this.socketPort = value; }
        }

        /// <summary>
        /// オープン状態取得（待ち状態含む）
        /// </summary>
        public bool IsOpen
        {
            get { return this.serverSocket != null; }
        }

        /// <summary>
        /// サーバーソケット
        /// </summary>
        private Socket serverSocket = null;

        /// <summary>
        /// クライアントソケット
        /// </summary>
        private Socket clientSocket = null;

        /// <summary>
        /// 受信バッファ（ダミー用）
        /// </summary>
        private byte[] recvBuffer = new byte[10];

        /// <summary>
        /// ソケット通信スレッド
        /// </summary>
        private Thread socketThread = null;

        /// <summary>
        /// ソケット通信スレッド継続状態
        /// </summary>
        private bool socketThreadLoop = false;

        /// <summary>
        /// 送信データアクセスMutex
        /// </summary>
        private Mutex sendDataMutex = new Mutex();

        /// <summary>
        /// 送信データ保持リスト
        /// </summary>
        private List<string> sendDataList = new List<string>();



        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TCPServer()
        {            
        }

        /// <summary>
        /// ソケットオープン
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Open(string address, int port)
        {
            bool result = true;

            Tracer.WriteInformation("Socket Open Start : IP={0}  Port={1}", address, port);
            try
            {
                // 一度クローズ
                this.Close();

                this.IpAddress = address;
                this.SocketPort = port;

                if (address.Equals(string.Empty) == false)
                {
                    // クライアント接続待ち準備
                    this.startListening();

                    // 計測データ送信スレッド起動
                    this.socketThreadLoop = true;
                    this.socketThread = new Thread(new ThreadStart(this.sendMeasureData));
                    this.socketThread.Name = "Socket thread";
                    this.socketThread.Start();
                }
                else
                {
                    Tracer.WriteInformation("Unknown ip address {0}", address);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
                result = false;
            }
            Tracer.WriteInformation("Socket Open End : Result={0}", result);

            return result;
        }

        /// <summary>
        /// クライアント接続待ち準備
        /// </summary>
        private void startListening()
        {
            // TCPソケット接続を１つに制限
            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // TIME_WAIT状態のソケットを利用可能とする
            this.serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // KeepAlive設定
            //this.serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            byte[] inBuffer  = new byte[12];
            BitConverter.GetBytes(1).CopyTo(inBuffer, 0);       // KeepAlive On/Off
            BitConverter.GetBytes(5000).CopyTo(inBuffer, 4);    // KeepAlive Time
            BitConverter.GetBytes(0).CopyTo(inBuffer, 8);       // KeepAlive Interval (回数は１０回固定なので、Interval設定時は、Interval x 10 となる）
            this.serverSocket.IOControl(IOControlCode.KeepAliveValues, inBuffer, null);

            this.serverSocket.Bind(new IPEndPoint(IPAddress.Parse(this.ipAddress), this.socketPort));
            this.serverSocket.Listen(1);

            // クライアント接続待ち開始
            this.startAccept();
        }

        /// <summary>
        /// クライアント接続待ち開始
        /// </summary>
        /// <param name="server"></param>
        private void startAccept()
        {
            if (this.serverSocket == null)
            {
                return;
            }

            this.serverSocket.BeginAccept(new AsyncCallback(this.acceptCallback), null);
            Tracer.WriteInformation("Client connection waiting ...");

            this.SocketStatusEvent("Listen");
        }

        /// <summary>
        /// クライアント接続コールバック
        /// </summary>
        /// <param name="asyncResult"></param>
        private void acceptCallback(IAsyncResult asyncResult)
        {
            if (this.serverSocket == null)
            {
                return;
            }

            // クライアントSocketの取得
            try
            {
                this.clientSocket = this.serverSocket.EndAccept(asyncResult);
                this.clientSocket.BeginReceive(recvBuffer, 0, recvBuffer.Length, 0, new AsyncCallback(this.receiveCallback), this.clientSocket);
                string clientAddress = ((IPEndPoint)this.clientSocket.RemoteEndPoint).Address.ToString();
                Tracer.WriteInformation("Connect client.  {0}", clientAddress);

                this.SocketStatusEvent(string.Format("Connected  ---  {0}", clientAddress));
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
                if (this.clientSocket != null)
                {
                    this.clientSocket.Shutdown(SocketShutdown.Both);
                    this.clientSocket.Close();
                    this.clientSocket = null;
                }

                // クライアント接続待ち開始
                this.startAccept();
            }
        }

        /// <summary>
        /// 受信コールバック
        /// </summary>
        /// <param name="asyncResult"></param>
        public void receiveCallback(IAsyncResult asyncResult)
        {
            if (this.clientSocket == null)
            {
                return;
            }

            try
            {
                Socket client = (Socket)asyncResult.AsyncState;
                int bytesRead = client.EndReceive(asyncResult);
                if (bytesRead > 0)
                {
                    // 受信データ無視
                    Tracer.WriteInformation("Receive client data.  {0}byte", bytesRead);

                    // 受信コールバック設定
                    client.BeginReceive(recvBuffer, 0, recvBuffer.Length, 0, new AsyncCallback(this.receiveCallback), client);
                }
                else
                {
                    Tracer.WriteInformation("Disconnect client.");
                    if (this.clientSocket != null)
                    {
                        this.clientSocket.Shutdown(SocketShutdown.Both);
                        this.clientSocket.Close();
                        this.clientSocket = null;
                    }

                    // クライアント接続待ち開始
                    this.startAccept();
                }
            }
            catch(Exception ex)
            {
                Tracer.WriteException(ex);
                if (this.clientSocket != null)
                {
                    this.clientSocket.Shutdown(SocketShutdown.Both);
                    this.clientSocket.Close();
                    this.clientSocket = null;
                }

                // クライアント接続待ち開始
                this.startAccept();
            }
        }

        /// <summary>
        /// ソケットクローズ
        /// </summary>
        public void Close()
        {
            try
            {
                // スレッドの終了
                if (this.socketThreadLoop)
                {
                    this.socketThreadLoop = false;
                    this.socketThread.Join(500);
                    this.socketThread = null;
                }

                // ソケットクローズ
                this.socketClose();
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
        }

        /// <summary>
        /// ソケットクローズ
        /// </summary>
        private void socketClose()
        {
            try
            {
                if (this.clientSocket != null)
                {
                    this.clientSocket.Shutdown(SocketShutdown.Both);
                    this.clientSocket.Close();
                    this.clientSocket = null;
                    Tracer.WriteInformation("Close client socket.");
                }
                if (this.serverSocket != null)
                {
                    this.serverSocket.Close();
                    this.serverSocket = null;
                    Tracer.WriteInformation("Close server socket.");
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
        }

        /// <summary>
        /// 計測データ送信スレッド
        /// </summary>
        private void sendMeasureData()
        {
            byte[] dummyBuff = new byte[10];

            this.clearSendData();
            
            while (this.socketThreadLoop)
            {
                try
                {
                    List<string> sendDataList = this.GetSendData();
                    if (sendDataList == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (this.clientSocket == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    foreach (string sendData in sendDataList)
                    {
                        byte[] sendBytes = Encoding.ASCII.GetBytes(sendData);
                        int sendLength = this.clientSocket.Send(sendBytes, 0, sendBytes.Length, SocketFlags.None);
                        //if (sendLength != sendBytes.Length)
                        //{
                        //    Tracer.WriteInformation("Unmatch send data length.  {0} != {1}", sendLength, sendBytes.Length);
                        //}
                    }
                }
                catch (Exception ex)
                {
                    Tracer.WriteException(ex);
                    this.socketClose();
                }
            }

            this.socketThreadLoop = false;
            Tracer.WriteInformation("End socket thread.");
        }

        /// <summary>
        /// 送信データ取得
        /// </summary>
        /// <returns></returns>
        public List<string> GetSendData()
        {
            List<string> result = null;

            try
            {
                // 排他する
                this.sendDataMutex.WaitOne();

                int listCount = this.sendDataList.Count;
                if (listCount != 0)
                {
                    result = this.sendDataList.GetRange(0, listCount);
                    this.sendDataList.RemoveRange(0, listCount);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // Mutexロック解放
                this.sendDataMutex.ReleaseMutex();
            }
            return result;
        }

        /// <summary>
        /// 送信データ設定
        /// </summary>
        /// <param name="data"></param>
        public void SetSendData(string data)
        {
            try
            {
                // 排他する
                this.sendDataMutex.WaitOne();

                this.sendDataList.Add(data);
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // Mutexロック解放
                this.sendDataMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 送信データ消去
        /// </summary>
        private void clearSendData()
        {
            try
            {
                // 排他する
                this.sendDataMutex.WaitOne();

                this.sendDataList.Clear();
            }
            catch (Exception ex)
            {
                Tracer.WriteException(ex);
            }
            finally
            {
                // Mutexロック解放
                this.sendDataMutex.ReleaseMutex();
            }
        }

    }
}
