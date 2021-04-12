using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace UDP_Asynchronous_Chat
{
    public class UDPAsynchronousChatClient : UDPChatGeneral
    {
        Socket mSockBroadCastSender;
        IPEndPoint mIPEPBroadcast;
        IPEndPoint mIPEPLocal;
        private EndPoint mChatServerEP;

        public UDPAsynchronousChatClient(int _localPort, int _remotePort)
        {
            mIPEPBroadcast = new IPEndPoint(IPAddress.Broadcast, _remotePort);
            mIPEPLocal = new IPEndPoint(IPAddress.Any, _localPort);

            mSockBroadCastSender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            mSockBroadCastSender.EnableBroadcast = true;
        }

        public void SendBroadcast(string strDataForBroadcast)
        {
            if (string.IsNullOrEmpty(strDataForBroadcast))
            {
                return;
            }
            try
            {
                if (!mSockBroadCastSender.IsBound)
                {
                    mSockBroadCastSender.Bind(mIPEPLocal);
                }
                SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
                var dataBytes = Encoding.ASCII.GetBytes(strDataForBroadcast);
                saea.SetBuffer(dataBytes, 0, dataBytes.Length);
                saea.RemoteEndPoint = mIPEPBroadcast;

                saea.Completed += SendCompletedCallBack;


                mSockBroadCastSender.SendToAsync(saea);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        private void SendCompletedCallBack(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine($"Data send succesfully to: {e.RemoteEndPoint}");
            if (Encoding.ASCII.GetString(e.Buffer).Equals("<DISCOVER>"))
            {
                ReceiveTextFromServer(expectedValue: "<CONFIRM>", IPEPReceiverLocal: mIPEPLocal);
            }
        }

        private void ReceiveTextFromServer(string expectedValue, IPEndPoint IPEPReceiverLocal)
        {
            if (IPEPReceiverLocal == null)
            {
                Console.WriteLine("No IPEndpoint specified");
            }

            SocketAsyncEventArgs saeaSendConfirmation = new SocketAsyncEventArgs();
            saeaSendConfirmation.SetBuffer(new byte[1024], 0, 1024);
            saeaSendConfirmation.RemoteEndPoint = IPEPReceiverLocal;

            saeaSendConfirmation.UserToken = expectedValue;
            saeaSendConfirmation.Completed += ReceiveconfirmationCompleted;

            mSockBroadCastSender.ReceiveFromAsync(saeaSendConfirmation);

        }

        private void ReceiveconfirmationCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0)
            {
                Debug.WriteLine($"Zero bytes transferred, socket error: {e.SocketError}");
                return;
            }

            var receivedText = Encoding.ASCII.GetString(e.Buffer, 0, e.BytesTransferred);
            var expectedText = Convert.ToString(e.UserToken);

            if (receivedText.Equals(expectedText))
            {
                Console.WriteLine($"Received confirmation from server. {e.RemoteEndPoint}");
                OnRaisePrintStringEvent(new PrintStringEventArgs($"Received confirmation from server. {e.RemoteEndPoint}"));
                mChatServerEP = e.RemoteEndPoint;
                ReceiveTextFromServer(string.Empty, mChatServerEP as IPEndPoint);
            }
            else if(string.IsNullOrEmpty(expectedText) && !string.IsNullOrEmpty(receivedText))
            {
                Console.WriteLine($"Text received: {receivedText}");
                OnRaisePrintStringEvent(new PrintStringEventArgs(($"Text received: {receivedText}")));
                ReceiveTextFromServer(string.Empty, mChatServerEP as IPEndPoint);
            }
            else if (!string.IsNullOrEmpty(expectedText) && !receivedText.Equals(expectedText))
            {
                Console.WriteLine($"Expected token not returned by the server");
                OnRaisePrintStringEvent(new PrintStringEventArgs(($"Expected token not returned by the server")));
            }
        }


        public void SendMessageToKnownServer(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                var bytesToSend = Encoding.ASCII.GetBytes(message);

                SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
                saea.SetBuffer(bytesToSend, 0, bytesToSend.Length);

                saea.RemoteEndPoint = mChatServerEP;

                saea.UserToken = message;

                saea.Completed += SendMessageToKnownServerCompletedCallback;
                mSockBroadCastSender.SendToAsync(saea);

            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendMessageToKnownServerCompletedCallback(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine($"Sent: {e.UserToken}{Environment.NewLine}Server: {e.RemoteEndPoint}");
            OnRaisePrintStringEvent(new PrintStringEventArgs(($"Sent: {e.UserToken}{Environment.NewLine}Server: {e.RemoteEndPoint}")));


        }
    }

  
}
