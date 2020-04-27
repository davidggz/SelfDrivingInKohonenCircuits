using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;

// Parece que esta es la clase con la mayor parte de la lógica del servidor.
// Por lo menos del lado cliente.

namespace tk
{
public class TcpClient : MonoBehaviour {

	/* La inicialización del socket es lo que determina qué va a poder hacer el servidor.
	 * InterNetwork solo hace referencia al tipo de IP que se va a utilizar.
	 * Stream tiene que ver con qué tipo de mensajes se pueden enviar (puede ser importante)
	 * El protocolo también es importante, puede que interese usar UDP.*/
	private Socket _clientSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

	/* Buffer en el que se van recibiendo los datos.*/
	private byte[] _recieveBuffer = new byte[8142];

	public delegate void OnDataRecv(byte[] data);

	public OnDataRecv onDataRecvCB;

	public bool Connect(string ip, int port)
	{
		if(_clientSocket.Connected)
			return false;
		
		try
		{
			IPAddress address = IPAddress.Parse(ip);
			_clientSocket.Connect(new IPEndPoint(address, port));
		}
		catch(SocketException ex)
		{
			Debug.Log(ex.Message);
			return false;
		}

		_clientSocket.BeginReceive(_recieveBuffer,0,_recieveBuffer.Length,SocketFlags.None,new AsyncCallback(ReceiveCallback),null);

		return true;
	}

	public void Disconnect()
	{
		if(_clientSocket.Connected)
			_clientSocket.Disconnect(true);
	}

	public void Close()
	{
		Disconnect();

		_clientSocket.Close();
	}

	void OnDestroy()
	{
		Close();
	}

	private void ReceiveCallback(IAsyncResult AR)
	{
		//Check how much bytes are recieved and call EndRecieve to finalize handshake
		int recieved = _clientSocket.EndReceive(AR);

		if(recieved <= 0)
			return;

		//Copy the recieved data into new buffer , to avoid null bytes
		byte[] recData = new byte[recieved];
		Buffer.BlockCopy(_recieveBuffer,0,recData,0,recieved);

		//Process data here the way you want , all your bytes will be stored in recData
		if(onDataRecvCB != null)
			onDataRecvCB.Invoke(recData);

		//Start receiving again
		_clientSocket.BeginReceive(_recieveBuffer,0,_recieveBuffer.Length,SocketFlags.None,new AsyncCallback(ReceiveCallback),null);
	}

	public bool SendData(byte[] data)
	{
		if(!_clientSocket.Connected)
			return false;
		
		SocketAsyncEventArgs socketAsyncData = new SocketAsyncEventArgs();
		socketAsyncData.SetBuffer(data,0,data.Length);
		_clientSocket.SendAsync(socketAsyncData);
		return true;
	}
}

} //end namepace tk
