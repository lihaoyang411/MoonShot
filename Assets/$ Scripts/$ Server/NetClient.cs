using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using TeamBlack.MoonShot.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;


namespace TeamBlack.MoonShot
{
    public class NetClient : MonoBehaviour
    {
        private Client _client;
        public Client Client => _client;

        private ConcurrentQueue<byte[]> _packetQueue 
            = new ConcurrentQueue<byte[]>();

        public Dictionary<NewPackets.PacketType, Action<byte[]>> _packetCallbacks 
            = new Dictionary<NewPackets.PacketType, Action<byte[]>>();

        public string ServerIP = "";

        #region UnityCallbacks
  
        public void Initialize(string IP) 
        {
            _client = new Client();
            _client.SetCallback(Queuer);

            ServerIP = IP;
            IPAddress ip;
            if (!IPAddress.TryParse(ServerIP, out ip)) ip = Constants.Networking.LOCAL_HOST;

            // TODO: ask client for an IP! :O
            _client.Connect(ip);
        }

        int frameCounter = 0;
        int byteCounter = 0;
        private void FixedUpdate() 
        {
            frameCounter++;

            // Process all newly recieved packets
            byte[] toProcess;
            while (_packetQueue.TryDequeue(out toProcess))
            {
                byteCounter += toProcess.Length;    
                PacketSwitch(toProcess);
            } 
            if (_packetQueue.Count != 0) Debug.Log("Some Packets not proccessed");

            if(frameCounter > 80)
            {
                //Debug.Log($"PROCESSED [{byteCounter}] BYTES!");
                byteCounter = 0;
                frameCounter = 0;
            }
        }
        #endregion

         public void AddCallback(NewPackets.PacketType type, Action<byte[]> callback) 
        {
            if (!_packetCallbacks.ContainsKey(type)) 
                _packetCallbacks.Add(type, callback);
            else 
                _packetCallbacks[type] += callback;
        }

        public void Send(byte[] packet, NewPackets.PacketType type, byte from)
        {
            packet = NewPackets.AppendByteHeader(from, packet);
            packet = NewPackets.AppendTypeHeader(type, packet);
            Client.Send(packet);
        }

        #region PacketCallbacks
        private void Queuer(byte[] packetBuffer, int bytesToRead) 
        {
            //print("GOT PACKET: " + bytesToRead); 

            if (packetBuffer != null && bytesToRead > 0)
            {
                byte[] packet = new byte[bytesToRead];
                Array.Copy(packetBuffer, packet, bytesToRead);
                _packetQueue.Enqueue(packet);
            }
        }
        private void PacketSwitch(byte[] packet)
        {

            if (packet == null)
            {
                //Debug.Log("ERROR: client recieved NULL packet!!");
                return;
            }
            if (packet.Length == 0)
            {
                Debug.LogWarning("ERROR: client recieved EMPTY packet!!");
                return;
            }

            // Get type to switch on
            NewPackets.PacketType type;
            packet = NewPackets.PopTypeHeader(packet, out type);

            //print($"Client recieved packet of type {type}");

            if (_packetCallbacks.ContainsKey(type))
                _packetCallbacks[type](packet);
            else
                Debug.LogWarning("Net Client: ERROR! Packet callback not added for that type!");
        }
        #endregion
    }
}