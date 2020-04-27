using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using UnityEngine.UI;
using System.Globalization;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Linq;
using System.IO;
using System.Text;
using System.Drawing;


namespace tk
{
    [RequireComponent(typeof(tk.JsonTcpClient))]

    public class TcpCarHandler : MonoBehaviour {

		public GameObject imagenObj;

        public GameObject carObj;
        public ICar car;

        public PathManager pm;
        public CameraSensor camSensor;
        private tk.JsonTcpClient client;
        float connectTimer = 1.0f;
        float timer = 0.0f;
        public Text ai_text;
        
        public float limitFPS = 0.5f;
        float timeSinceLastCapture = 0.0f;

        float ai_steering = 0.0f;
        float ai_throttle = 0.0f;
        float ai_brake = 0.0f;

        bool asynchronous = true;
        float time_step = 0.1f;
        bool bResetCar = false;

		/*Indica si vamos a enviar mensajes de GAN o mensajes para el NNSteering*/
		public bool ganMode = false;
		byte[] image64;
		bool InferenciaComenzada = false;

		//string dirEntrega = "C:/Users/david/Documents/Projects/drivingSimulator/src/ENTREGA/";
		//string dirEnvio = "C:/Users/david/Documents/Projects/drivingSimulator/src/ENVIO/";
		string dirEntrega = Path.Combine(Directory.GetCurrentDirectory(), "../src/ENTREGA/");
		string dirEnvio = Path.Combine(Directory.GetCurrentDirectory(), "../src/ENVIO/");

		bool recogiendo = false;
		bool enviando = false;
		bool mensajeEnviado = false;
		int contadorEnviadas = 0;
		int contadorRecogidas = 0;
		byte[] fileData;



		public enum State
        {
            UnConnected,
            SendTelemetry
        }        

        public State state = State.UnConnected;

        void Awake()
        {
			// Se obtiene el coche
            car = carObj.GetComponent<ICar>();
            client = GetComponent<tk.JsonTcpClient>();
		    pm = GameObject.FindObjectOfType<PathManager>();

            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            GameObject go = CarSpawner.getChildGameObject(canvas.gameObject, "AISteering");
            if (go != null)
                ai_text = go.GetComponent<Text>();
        }

        void Start()
        {
            Initcallbacks();
			if(ganMode == true)
			{
				createDirs();
			}
        }

		void createDirs()
		{
			DirectoryInfo dirInfo;
			if (Directory.Exists(dirEnvio) == false) {
				Directory.CreateDirectory(dirEnvio);
			} else {
				dirInfo = new DirectoryInfo(dirEnvio);
				foreach (FileInfo file in dirInfo.GetFiles())
				{
					file.Delete();
				}
			}

			if (Directory.Exists(dirEntrega) == false) {
				Directory.CreateDirectory(dirEntrega);
			} else {
				dirInfo = new DirectoryInfo(dirEntrega);
				foreach (FileInfo file in dirInfo.GetFiles())
				{
					file.Delete();
				}
			}
		}

        void Initcallbacks()
        {
			// Aquí se asignan los callbacks a cada uno de los eventos.
            client.dispatcher.Register("control", new tk.Delegates.OnMsgRecv(OnControlsRecv));
            client.dispatcher.Register("exit_scene", new tk.Delegates.OnMsgRecv(OnExitSceneRecv));
            client.dispatcher.Register("reset_car", new tk.Delegates.OnMsgRecv(OnResetCarRecv));
            client.dispatcher.Register("new_car", new tk.Delegates.OnMsgRecv(OnRequestNewCarRecv));
            client.dispatcher.Register("step_mode", new tk.Delegates.OnMsgRecv(OnStepModeRecv));
            client.dispatcher.Register("quit_app", new tk.Delegates.OnMsgRecv(OnQuitApp));
            client.dispatcher.Register("regen_road", new tk.Delegates.OnMsgRecv(OnRegenRoad));
			client.dispatcher.Register("GANResult", new tk.Delegates.OnMsgRecv(OnGANResult));
			client.dispatcher.Register("GANControl", new tk.Delegates.OnMsgRecv(OnGANControl));
		}

        bool Connect()
        {
            return client.Connect();
        }

        void Disconnect()
        {
            client.Disconnect();
        }

        void Reconnect()
        {
            Disconnect();
            Connect();
        }
		

		// Función encargada de enviar los datos correspondientes al servidor.
        void SendTelemetry()
        {
            JSONObject json = new JSONObject(JSONObject.Type.OBJECT);
            json.AddField("msg_type", "telemetry");

            json.AddField("steering_angle", (car.GetSteering() / car.GetMaxSteering()).ToString().Replace(',', '.'));
            json.AddField("throttle", car.GetThrottle());
            json.AddField("speed", car.GetVelocity().magnitude.ToString().Replace(',', '.'));
            json.AddField("image", System.Convert.ToBase64String(camSensor.GetImageBytes()));
            
            json.AddField("hit", car.GetLastCollision());
            car.ClearLastCollision();

            Transform tm = car.GetTransform();
            json.AddField("pos_x", tm.position.x.ToString().Replace(',', '.'));
            json.AddField("pos_y", tm.position.y.ToString().Replace(',', '.'));
            json.AddField("pos_z", tm.position.z.ToString().Replace(',', '.'));

            json.AddField("time", Time.timeSinceLevelLoad.ToString().Replace(',', '.'));
			//Debug.Log(car.GetVelocity().magnitude);

            if(pm != null)
            {
                float cte = 0.0f;
                if(pm.path.GetCrossTrackErr(tm.position, ref cte))
                {
                    json.AddField("cte", cte.ToString().Replace(',', '.'));
                }
                else
                {
                    pm.path.ResetActiveSpan();
                    json.AddField("cte", 0.0f);
                }
            }

			//print(json);

            client.SendMsg( json );
		}

		void comenzarEnvio()
		{
			enviando = true;
		}

		void OnGANResult(JSONObject json)
		{
			Debug.Log("ONGANRESULT");
			/*if (imagenObj.activeSelf == false)
			{
				imagenObj.SetActive(true);
				Debug.Log("Activando imagenes");
			}*/

			//Comenzamos a recoger imagenes
			recogiendo = true;
		}

		void OnGANControl(JSONObject json)
		{
			try
			{
				ai_steering = float.Parse(json["steering"].str, CultureInfo.InvariantCulture.NumberFormat) * car.GetMaxSteering();
				ai_throttle = float.Parse(json["throttle"].str, CultureInfo.InvariantCulture.NumberFormat);
				ai_brake = float.Parse(json["brake"].str, CultureInfo.InvariantCulture.NumberFormat);

				//Debug.Log("STEERING: " + ai_steering);

				car.RequestSteering(ai_steering);
				car.RequestThrottle(25);
				//car.RequestFootBrake(ai_brake);
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
			}
		}

		void SendCarLoaded()
        {
            JSONObject json = new JSONObject(JSONObject.Type.OBJECT);
            json.AddField("msg_type", "car_loaded");
            client.SendMsg( json );
        }

        void OnControlsRecv(JSONObject json)
        {
            try
            {
                ai_steering = float.Parse(json["steering"].str, CultureInfo.InvariantCulture.NumberFormat) * car.GetMaxSteering();
                ai_throttle = float.Parse(json["throttle"].str, CultureInfo.InvariantCulture.NumberFormat);
                ai_brake = float.Parse(json["brake"].str, CultureInfo.InvariantCulture.NumberFormat);

                car.RequestSteering(ai_steering);
                car.RequestThrottle(ai_throttle);
                car.RequestFootBrake(ai_brake);
            }
            catch(Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        void OnExitSceneRecv(JSONObject json)
        {
            SceneManager.LoadSceneAsync(0);
        }

        void OnResetCarRecv(JSONObject json)
        {
            bResetCar = true;            
        }

        void OnRequestNewCarRecv(JSONObject json)
        {
            string host = json.GetField("host").str;
            string port = json.GetField("port").str;

            //We get this callback in a worker thread, but need to make mainthread calls.
            //so use this handy utility dispatcher from
            // https://github.com/PimDeWitte/UnityMainThreadDispatcher
            UnityMainThreadDispatcher.Instance().Enqueue(SpawnNewCar(host, port));
        }

        IEnumerator SpawnNewCar(string host, string port)
        {
            CarSpawner spawner = GameObject.FindObjectOfType<CarSpawner>();

            if(spawner != null)
            {
                spawner.Spawn(Vector3.right * -4.0f, host, port);
            }

            yield return null;
        }

        void OnRegenRoad(JSONObject json)
        {
            //This causes the track to be regenerated with the given settings.
            //This only works in scenes that have random track generation enabled.
            //For now that is only in scene road_generator.
            //An index into our track options. 5 in scene RoadGenerator.
            int road_style = int.Parse(json.GetField("road_style").str);
            int rand_seed = int.Parse(json.GetField("rand_seed").str);
            float turn_increment = float.Parse(json.GetField("turn_increment").str);

            //We get this callback in a worker thread, but need to make mainthread calls.
            //so use this handy utility dispatcher from
            // https://github.com/PimDeWitte/UnityMainThreadDispatcher
            UnityMainThreadDispatcher.Instance().Enqueue(RegenRoad(road_style, rand_seed, turn_increment));
        }

        IEnumerator RegenRoad(int road_style, int rand_seed, float turn_increment)
        {
            TrainingManager train_mgr = GameObject.FindObjectOfType<TrainingManager>();
            PathManager path_mgr = GameObject.FindObjectOfType<PathManager>();

            if(train_mgr != null)
            {
                if (turn_increment != 0.0 && path_mgr != null)
                {
                    path_mgr.turnInc = turn_increment;
                }

                UnityEngine.Random.InitState(rand_seed);
                train_mgr.SetRoadStyle(road_style);
                train_mgr.OnMenuRegenTrack();
            }

            yield return null;
        }

        void OnStepModeRecv(JSONObject json)
        {
            string step_mode = json.GetField("step_mode").str;
            float _time_step = float.Parse(json.GetField("time_step").str);

            Debug.Log("got settings");

            if(step_mode == "synchronous")
            {
                Debug.Log("setting mode to synchronous");
                asynchronous = false;
                this.time_step = _time_step;
                Time.timeScale = 0.0f;
            }
            else
            {
                Debug.Log("setting mode to asynchronous");
                asynchronous = true;
            }
        }
    
        void OnQuitApp(JSONObject json)
        {
            Application.Quit();
        }

		void OnDestroy()
		{
			Shutdown();
		}

		void OnDisable()
		{
			Shutdown();
		}

		void Shutdown()
		{
			DirectoryInfo dirEntregas = new DirectoryInfo(dirEnvio);
			dirEntregas.Delete(true);
			DirectoryInfo dirEnvios = new DirectoryInfo(dirEntrega);
			dirEnvios.Delete(true);
		}

		void recogerImagenes()
		{
			DirectoryInfo dir = new DirectoryInfo(dirEntrega);
			if (dir.GetFiles().Length > 1)
			{
				//string firstFileName = dir.GetFiles().Select(fi => fi.Name).FirstOrDefault();
				string firstFileName = "ENTREGA_" + contadorRecogidas + ".jpg";

				Texture2D newImg = new Texture2D(1, 1);
				fileData = File.ReadAllBytes(Path.Combine(dirEntrega, firstFileName));
				File.Delete(Path.Combine(dirEntrega, firstFileName));

				newImg.LoadImage(fileData);
				newImg.Apply();

				Sprite newSprite = Sprite.Create(newImg, new Rect(0, 0, newImg.width, newImg.height), new Vector2(0.5f, 0.5f));
				imagenObj.GetComponent<Image>().sprite = newSprite;

				/*if (!imagenObj.activeSelf)
					imagenObj.SetActive(true);*/

				if(contadorRecogidas == 0)
				{
					contadorRecogidas = 1;
				} else {
					contadorRecogidas = 0;
				}
			}
		}

		// Update is called once per frame
		void Update () 
        {
			Debug.Log(Directory.GetCurrentDirectory());

			// Si no está conectado, se conecta.
            if(state == State.UnConnected)
            {
                timer += Time.deltaTime;

                if(timer > connectTimer)
                {
                    timer = 0.0f;

                    if(Connect())
                    {
                        SendCarLoaded();
                        state = State.SendTelemetry;
                    }
                }
            }
			// Una vez conectado, se pone en estado Telemetry y comienza a enviar mensajes.
            else if(state == State.SendTelemetry)
            {
				// No entiendo muy bien esto, pero se supone que resetea el coche,
				// así que no voy a tocarlo.
                if (bResetCar)
                {
                    car.RestorePosRot();
                    pm.path.ResetActiveSpan();
                    bResetCar = false;
                }

                timeSinceLastCapture += Time.deltaTime;
				/*Con esta condición se limita el número de FPS*/
				if (timeSinceLastCapture > 1.0f / limitFPS)
				{
					/*Dependiendo del modo en el que estemos, hacemos unas cosas u otras.*/
					if (ganMode == false)
					{
						timeSinceLastCapture -= (1.0f / limitFPS);
						SendTelemetry();

						if (ai_text != null)
							ai_text.text = string.Format("NN: {0} : {1}", ai_steering, ai_throttle);
					}
					else
					{
						timeSinceLastCapture -= (1.0f / limitFPS);
						image64 = camSensor.GetImageBytes();

						if (recogiendo == true)
						{
							recogiendo = false;
							InvokeRepeating("recogerImagenes", 0, 0.09f);
						}

						if (enviando == true)
						{
							DirectoryInfo dir = new DirectoryInfo(dirEnvio);
							if (dir.GetFiles().Length < 2) {
								Debug.Log(Path.Combine(dirEnvio, "ENVIO_" + contadorEnviadas + ".png"));
								try
								{
									File.WriteAllBytes(Path.Combine(dirEnvio, "ENVIO_" + contadorEnviadas + ".png"), image64);

								}
								catch
								{
									Debug.Log("Fallo al guardar las imagenes");
								}

								//Thread.Sleep(1000);
								if (contadorEnviadas == 0){
									contadorEnviadas = 1;
								} else {
									contadorEnviadas = 0;
								}
							}

							if (mensajeEnviado == false)
							{
								mensajeEnviado = true;
								/*AVISAMOS DE QUE VAMOS A COMENZAR A ENVIAR IMAGENES ENVIANDO UN MENSAJE*/
								JSONObject json = new JSONObject(JSONObject.Type.OBJECT);
								// Escribimos qué tipo de mensaje es para que sea identificado por el server.
								json.AddField("msg_type", "telemetryGAN");

								// Enviamos el mensaje.
								client.SendMsg(json);
							}
						}

						if (InferenciaComenzada == false)
						{
							Debug.Log("Comenzamos inferencia.");
							InferenciaComenzada = true;
							comenzarEnvio();
						}

					}
				}
                    
            }
        }
    }
}
