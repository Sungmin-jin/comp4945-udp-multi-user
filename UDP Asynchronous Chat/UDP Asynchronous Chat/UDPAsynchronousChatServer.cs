using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace UDP_Asynchronous_Chat
{
    public class UDPAsynchronousChatServer :  UDPChatGeneral
    {
        Socket mSockBroadcastReceiver;
        IPEndPoint mIPEPLocal;
        private int retryCount;

        List<EndPoint> mListOfClients;
        public UDPAsynchronousChatServer()
        {
            mSockBroadcastReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            mIPEPLocal = new IPEndPoint(IPAddress.Any, 23000);
            mSockBroadcastReceiver.EnableBroadcast = true;
            retryCount = 0;

            mListOfClients = new List<EndPoint>();
        }

        public void StartReceivingData()
        {
            try
            {
                SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
                saea.SetBuffer(new byte[1024], 0, 1024);
                saea.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                if (!mSockBroadcastReceiver.IsBound)
                {
                    mSockBroadcastReceiver.Bind(mIPEPLocal);
                }
                saea.Completed += ReceiveCompletedCallback;
                if (!mSockBroadcastReceiver.ReceiveFromAsync(saea))
                {
                    Console.WriteLine($"Failed to receive data - socket error: {saea.SocketError}");
                    OnRaisePrintStringEvent(new PrintStringEventArgs($"Failed to receive data - socket error: {saea.SocketError}"));
                    if (retryCount++ >= 10)
                    {
                        return;
                    }
                    else
                    {
                        StartReceivingData();
                    }
                    StartReceivingData();

                }
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                throw;
            }
        }

        private void ReceiveCompletedCallback(object sender, SocketAsyncEventArgs e)
        {
            string textReceived = Encoding.ASCII.GetString(e.Buffer, 0, e.BytesTransferred);
            Console.WriteLine(
                $"Text received {textReceived}{Environment.NewLine}" +
                $"Number of bytes received {e.BytesTransferred}{Environment.NewLine}" +
                $"Received data from endpoint: {e.RemoteEndPoint}{Environment.NewLine}"
                );
            if (textReceived.Equals("<DISCOVER>"))
            {
                if (!mListOfClients.Contains(e.RemoteEndPoint))
                {
                    mListOfClients.Add(e.RemoteEndPoint);
                    Console.WriteLine("Total clients: " + mListOfClients.Count);
                    OnRaisePrintStringEvent(new PrintStringEventArgs(($"New client: {e.RemoteEndPoint} - Total clients: " + mListOfClients.Count)));
                }

                SendTextToEndPoint("<CONFIRM>", e.RemoteEndPoint);
            }
            else
            {
                foreach(IPEndPoint remEP in mListOfClients)
                {
                    if (!remEP.Equals(e.RemoteEndPoint))
                    {
                        SendTextToEndPoint(textReceived, remEP);
                    }
                }
            }
            StartReceivingData();
        }

        private void SendTextToEndPoint(string textToSend, EndPoint remoteEndPoint)
        {
            if(string.IsNullOrEmpty(textToSend) || remoteEndPoint == null)
            {
                return;
            }
            SocketAsyncEventArgs saeaSend = new SocketAsyncEventArgs();
            saeaSend.RemoteEndPoint = remoteEndPoint;
            var bytesToSend = Encoding.ASCII.GetBytes(textToSend);

            saeaSend.SetBuffer(bytesToSend, 0, bytesToSend.Length);

            saeaSend.Completed += SendTextToEndPointCompleted;
            mSockBroadcastReceiver.SendToAsync(saeaSend);
        }

        private void SendTextToEndPointCompleted(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine($"Completed sending text to {e.RemoteEndPoint}");
        }
    }
}
