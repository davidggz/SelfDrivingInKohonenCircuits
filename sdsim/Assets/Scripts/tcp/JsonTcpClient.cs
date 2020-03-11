using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using tk;

namespace tk
{

    [Serializable]
    public class NetPacket
    {
        public NetPacket(string m, string data)
        {
            msg = m;
            payload = data;
        }

        public string msg;
        public string payload;
    }

    
    //Wrap a tcpclient and dispatcher to handle network events over a tcp connection.
    //We create a NetPacket header to wrap all sends and recv. Should be pretty portable
    //over languages.
    [RequireComponent(typeof(tk.TcpClient))]
    public class JsonTcpClient : MonoBehaviour {

        public string nnIPAddress = "127.0.0.1";
        public int nnPort = 9090;
        private tk.TcpClient client;

        public tk.Dispatcher dispatcher;

        public bool dispatchInMainThread = false;

        private List<string> recv_packets;

        readonly object _locker = new object();

        void Awake()
        {
            recv_packets = new List<string>();
			// Se inicializa el dispatcher
            dispatcher = new tk.Dispatcher();
            dispatcher.Init();

			// Se utiliza la clase TcpClient
            client = GetComponent<tk.TcpClient>();
            
			// Se llama a la función Initcallbacks para inicializar el callback que se produce
			// al recibir un mensaje en la clase TcpClient
            Initcallbacks();
        }

        void Initcallbacks()
        {
			// Se le pone al delegate de la clase TcpClient, la función 
			// OnDataRecv de esta clase.
            client.onDataRecvCB += new TcpClient.OnDataRecv(OnDataRecv);
        }

        public bool Connect()
        {
            return client.Connect(nnIPAddress, nnPort);
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public void Reconnect()
        {
            Disconnect();
            Connect();
        }

        public void SendMsg(JSONObject msg)
        {
            string packet = msg.ToString() + "\n";

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(packet);

            client.SendData( bytes );
        }

        void OnDataRecv(byte[] bytes)
        {
			// Se coge un string a partir de los bytes que se reciben
            string str = System.Text.Encoding.UTF8.GetString(bytes);

			lock (_locker)
            {
				// Se guarda el string en la lista recv_packets
                recv_packets.Add(str);
            }

            if(!dispatchInMainThread)
            {
				// Se llama a la función Dispatch de esta misma clase
                Dispatch();
            }
        }

        void Dispatch()
        {
            lock(_locker)
            {
				// Se itera dentro de cada paquete dentro de recv_packets
                foreach(string str in recv_packets)
                {
                    try
                    {
						// Se transforma en JSON
                        JSONObject j = new JSONObject(str);

						// Se coge el tipo de mensaje
                        string msg_type = j["msg_type"].str;

						// Se hace el dipatch dentro de la funcion dispatcher
						// teniendo en cuenta el tipo de mensaje y enviando el JSON.
						// En el dispatcher se invoca la función correspondiente del diccionario
						// de TCP Handler.
                        dispatcher.Dipatch(msg_type, j);

                    }
                    catch(Exception e)
                    {
                        Debug.Log(e.ToString());
                    }
                }

                recv_packets.Clear();
            }

        }

        void Update()
        {
            if (dispatchInMainThread)
            {
                Dispatch();
            }
        }
    }
}