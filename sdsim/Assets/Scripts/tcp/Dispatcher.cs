using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using tk;

// El dispatcher es el encargado de ver si existen eventos o mensajes 
// y registrarlos en un diccionario.

namespace tk
{
    public class Dispatcher {
        //Name to Message client handling.

        private Dictionary <string, Delegates> eventDictionary;

        public void Init ()
        {
            if (eventDictionary == null)
            {
                eventDictionary = new Dictionary<string, Delegates>();
            }
        }

        public void Register(string msgType, Delegates.OnMsgRecv regCallback)
        {
            Delegates Delegates = null;

			//Comprueba que hay un evento con la clave msgType.
			// Si existe, se introduce en Delgates
			if (eventDictionary.TryGetValue (msgType, out Delegates))
            {
				// Se le suma al evento obtenido el Callback
                Delegates.onMsgCb += regCallback;
            }
            else
            {
				// Si no existe el evento, se crea uno nuevo
                Delegates newDel = new Delegates();

				// Se le añade el Callback
                newDel.onMsgCb += regCallback;

				// Se añade el nuevo mensaje al diccionario
                eventDictionary.Add(msgType, newDel);
            }
        }

        public void Dipatch(string msgType, JSONObject msgPayload)
        {
            Delegates delegates = null;
        
            if (eventDictionary.TryGetValue (msgType, out delegates))
            {
                delegates.onMsgCb.Invoke(msgPayload);
            }
        }
    }

}