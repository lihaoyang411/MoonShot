using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

// Fix Me: some sort of "packet duplication" occurs with multiple clients. Might be a localhost issue.
namespace TeamBlack.MoonShot.Networking 
{

    public abstract class AbstractClient : IDisposable
    {
        protected TcpClient _client;
        protected byte[] _buffer = new byte[Constants.Networking.READ_BUFF_SIZE];  
        
        protected Action<byte[], int> _recieveBytesCallback;
        protected int _sizeOfPacket = 0;
        protected int _currentRead = 0; 

        public NetworkStream Stream => _client.GetStream();

        public AbstractClient(TcpClient client, 
        Action<byte[], int> recieveBytesCallback = null) 
        {
            _client = client;
            _recieveBytesCallback = recieveBytesCallback;
        }

        public void SetCallback(Action<byte[], int> recieveBytesCallback)
        {
            _recieveBytesCallback = recieveBytesCallback;
        }

        protected void BeginRead(IAsyncResult ar) 
        {
            try
            {
                int length = Stream.EndRead(ar);
                int packetSize = BitConverter.ToInt32(_buffer, 0);

                _sizeOfPacket = packetSize;
                _currentRead = 0;

                //Debug.Log($"Read header of packet size {packetSize}!");
                if (_sizeOfPacket >= Constants.Networking.READ_BUFF_SIZE || _sizeOfPacket <= 0)
                {
                    Debug.Log($"!!!! SIZE OF PACKET ERROR: " + _sizeOfPacket);
                }

                Stream.BeginRead(_buffer, 0, packetSize, EndRead, null);
            }
            catch (Exception e)
            {
                Debug.Log("YARR! NET WRAPPER EXCEPTION: BeginRead() Failed! \n" + e);
            }
        }

        protected void EndRead(IAsyncResult ar)
        {
            try
            {
                int length = Stream.EndRead(ar);
                _currentRead += length;

                // Continue reading this packets
                if (_currentRead < _sizeOfPacket)
                    Stream.BeginRead(_buffer, _currentRead, _sizeOfPacket - _currentRead, EndRead, null);
                // Process packet then begin read on next packet
                else
                {
                    if (_sizeOfPacket == 0)
                        Debug.Log("ERROR: WTF size is 0?!");
                    else
                        _recieveBytesCallback(_buffer, _sizeOfPacket);
                    Stream.BeginRead(_buffer, 0, sizeof(int), BeginRead, null);
                }
            }
            catch (Exception e)
            {
                Debug.Log("YARR! NET WRAPPER EXCEPTION: EndRead() Failed! \n" + e);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                // Find and write packet size header
                int packetSize = data.Length;
                byte[] packetSizeHeader = BitConverter.GetBytes(packetSize);

                //Debug.Log($"Sending size {packetSize} packet");

                MemoryStream memStream = new MemoryStream();
                memStream.Write(packetSizeHeader, 0, sizeof(int));
                memStream.Write(data, 0, data.Length);

                //Stream.Write(packetSizeHeader, 0, sizeof(int));
                byte[] final = memStream.ToArray();

                // Write main body of packet
                Stream.Write(final, 0, final.Length);
            }
            catch (Exception e)
            {
                Debug.Log("YARR! NET WRAPPER EXCEPTION: Send() Failed! \n" + e);
            }
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
    public class ConnectedClient : AbstractClient
    {
        public ConnectedClient(TcpClient client, 
        Action<byte[], int> recieveBytesCallback = null): 
        base(client, recieveBytesCallback)
        {
            // Begin read loop
            // Assume the first byte chunk is a size header
            Stream.BeginRead(_buffer, 0, sizeof(int), BeginRead, null);
        }
    }
    
    public class Client : AbstractClient 
    {
        #region fields
        private IPAddress _ip;
        #endregion
        // temp

        public Client() : base (null, null)
        {
            _client = new TcpClient();
        }

        public void Connect(string ip) 
        {
            Connect(Dns.GetHostEntry(ip).AddressList[0]);
        }

        public void Connect(IPAddress ip) 
        {
            _ip = ip;

            _client.BeginConnect(_ip, Constants.Networking.TCP_PORT, 
                new AsyncCallback(EndConnection), null
            );
        }


        #region callbacks
        internal void EndConnection(IAsyncResult ar) 
        {
            _client.EndConnect(ar);
            Debug.Log("YARRR, I've boarded the ship!");
            // Begin read loop
            // Assume the first byte chunk is a size header
            Stream.BeginRead(_buffer, 0, sizeof(int), BeginRead, null);
        }
        #endregion
    }
    public class Server : IDisposable
    {
        #region fields
        private TcpListener _listener;
        private List<ConnectedClient> _clients;
        protected Action<byte[], int> _recieveBytesCallback;
        protected Action<ConnectedClient> _connectCallback;

        private byte[] buffer = new byte[Constants.Networking.READ_BUFF_SIZE];

        #endregion

        // NOTE: MUST EXPLICITLY START THE SERVER
        public Server() 
        {
            _clients = new List<ConnectedClient>();
        }

        public void Start(Action<byte[], int> recieveBytesCallback, Action<ConnectedClient> connectCallback) 
        {
            Debug.Log("Starting Server");

            _recieveBytesCallback = recieveBytesCallback;
            _connectCallback = connectCallback;

            _listener = new TcpListener(IPAddress.Any, Constants.Networking.TCP_PORT);
            _listener.Start();
            
            _listener.BeginAcceptTcpClient(
                new AsyncCallback(AcceptClients),
                null
            );
        }

        public void Dispose()
        {
            _listener.Stop();
            //for (int i = 0; i < _clients.Count; i++)
            //{
            //    _clients[i].Dispose();
            //}
        }

        #region callbacks

        void AcceptClients(IAsyncResult ar) 
        {
            TcpClient newClient = _listener.EndAcceptTcpClient(ar);
            Debug.Log("YARRR, a crew member has boarded me ship!");

            ConnectedClient newConnection = new ConnectedClient(newClient, _recieveBytesCallback);
            _clients.Add(newConnection);
            
            // accept more clients
            _listener.BeginAcceptTcpClient(
                new AsyncCallback(AcceptClients),
                null
            );          

            _connectCallback(newConnection);
        }

        #endregion

        #region API
        public void Send(ConnectedClient client, byte[] message) 
        {
            if (!_clients.Contains(client))
                Debug.Log("INVALID CLIENT");
            else 
                client.Send(message);
        }

        public void Broadcast(byte[] message) 
        {
            foreach(var client in _clients) Send(client, message);
        }
        #endregion
    }
}   