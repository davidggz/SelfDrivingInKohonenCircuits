using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System;
using UnityEngine.UI;

[Serializable]
public class MetaJson
{
    public string[] inputs;
    public string[] types;

    public void Init(string[] _inputs, string[] _types)
    {
        inputs = _inputs;
        types = _types;
    }
}

[Serializable]
public class DonkeyRecord
{
    public string cam_image_array;
    public float user_throttle;
    public float user_angle;
    public string user_mode; 
    public int track_lap;
    public int track_loc;

    public void Init(string image_name, float throttle, float angle, string mode, int lap, int loc)
    {
        cam_image_array = image_name;
        user_throttle = throttle;
        user_angle = angle;
        user_mode = mode;
        track_lap = lap;
        track_loc = loc;
    }

    public string AsString()
    {
        string json = JsonUtility.ToJson(this);

        //Can't name the variable names with a slash, so replace on output
        json = json.Replace("cam_image", "cam/image");
        json = json.Replace("user_throttle", "user/throttle");
        json = json.Replace("user_angle", "user/angle");
        json = json.Replace("user_mode", "user/mode");
        json = json.Replace("track_lap", "track/lap");
        json = json.Replace("track_lap", "track/lap");
        json = json.Replace("track_loc", "track/loc");

        return json;
    }
}
public class Logger : MonoBehaviour {

	public GameObject carObj;
	public ICar car;
	public CameraSensor camSensor;
	// Camara del segundo coche
	public CameraSensor camSensor2;
	public CameraSensor optionlB_CamSensor;
	public Lidar lidar;

	// Coger dos imagenes o solo una. Modo imagen normal/imagen modificada
	// Valores validos: 1 2
	public int nPhotos = 2;

	//what's the current frame index
    public int frameCounter = 0;

    //which lap
    public int lapCounter = 0;

	//is there an upper bound on the number of frames to log
	public int maxFramesToLog = 14000;

	//should we log when we are enabled
	public bool bDoLog = true;

    public int limitFPS = 30;

    float timeSinceLastCapture = 0.0f;

    //We can output our logs in the style that matched the output from the shark robot car platform - github/tawnkramer/shark
    public bool SharkStyle = true;

	//We can output our logs in the style that matched the output from the udacity simulator
	public bool UdacityStyle = false;

    //We can output our logs in the style that matched the output from the donkey robot car platform - donkeycar.com
    public bool DonkeyStyle = false;

    //Tub style as prefered by Donkey2
    public bool DonkeyStyle2 = false;

    public Text logDisplay;

	string outputFilename = "log_car_controls.txt";
	private StreamWriter writer;

	class ImageSaveJob {
		public string filename;
		public byte[] bytes;
	}
		
	List<ImageSaveJob> imagesToSave;

	Thread thread;

    string GetLogPath()
    {
        if(GlobalState.log_path != "default")
            return GlobalState.log_path + "/";

        return Application.dataPath + "/../log/";
    }

	void Awake()
	{
		if (File.Exists(GetLogPath() + "/IMG")){
			frameCounter = Directory.GetFiles(GetLogPath() + "/IMG").Length;
		}
		car = carObj.GetComponent<ICar>();

		if(bDoLog && car != null)
		{
			if(UdacityStyle)
			{
				outputFilename = "driving_log.csv";
			}

			string filename = GetLogPath() + outputFilename;


			// Se inicializa un writer que se encarga de escribir en el fichero
			// las etiquetas de las imagenes
			writer = new StreamWriter(filename);

			Debug.Log("Opening file for log at: " + filename);

			if(UdacityStyle)
			{
				writer.WriteLine("center,left,right,steering,throttle,brake,speed");
				// Generado por mi para poder guardar las imagenes en una carpeta.
				Directory.CreateDirectory(GetLogPath() + "IMG");

				// Si estamos en el modo de dos imagenes, creamos otra carpeta.
				if (nPhotos == 2)
				{
					// Generado por mi para poder guardar las imagenes en una carpeta.
					Directory.CreateDirectory(GetLogPath() + "IMG2");
				}
			}

            if(DonkeyStyle2)
            {
                MetaJson mjson = new MetaJson();
                string[] inputs = {"cam/image_array", "user/angle", "user/throttle", "user/mode", "track/lap", "track/loc"};
                string[] types = {"image_array", "float", "float", "str", "int", "int"};
                mjson.Init(inputs, types);
                string json = JsonUtility.ToJson(mjson);
				var f = File.CreateText(GetLogPath() + "meta.json");
				f.Write(json);
				f.Close();
            }
		}

        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        GameObject go = CarSpawner.getChildGameObject(canvas.gameObject, "LogCount");
        if (go != null)
            logDisplay = go.GetComponent<Text>();

		// Se crea la lista donde se guardarán las imágenes 
		// que se deben ir imprimiendo
        imagesToSave = new List<ImageSaveJob>();

		// Generación del thread que lee imagenes y las almacena
		thread = new Thread(SaverThread);
		thread.Start();
	}
		
	// Update is called once per frame
	void Update () 
	{
		if(!bDoLog)
			return;

        timeSinceLastCapture += Time.deltaTime;

        if (timeSinceLastCapture < 1.0f / limitFPS)
            return;

        timeSinceLastCapture -= (1.0f / limitFPS);

        string activity = car.GetActivity();

		if(writer != null)
		{
			if(UdacityStyle)
			{
				// Se le pide el nombre del fichero a la funcion
				string image_filename = GetUdacityStyleImageFilename();
				// Se calcula el porcentaje de steering que está haciendo el coche
				// teniendo en cuenta el maximo y el real.
				float steering = car.GetSteering() / car.GetMaxSteering();
				writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", image_filename, 
                    "none", "none", steering.ToString(), 
                    car.GetThrottle().ToString(), "0", "0", lapCounter));
			}
            else if(DonkeyStyle || SharkStyle)
            {
				// Aquí podría poner yo mi propio estilo
            }
            else if(DonkeyStyle2)
            {
                DonkeyRecord mjson = new DonkeyRecord();
                float steering = car.GetSteering() / car.GetMaxSteering();
                float throttle = car.GetThrottle() * 10.0f;
                int loc = 0;

                //training code like steering clamped between -1, 1
                steering = Mathf.Clamp(steering, -1.0f, 1.0f);

                mjson.Init(string.Format("{0}_cam-image_array_.jpg", frameCounter),
                    throttle, steering, "user", lapCounter, loc);

                string json = mjson.AsString();
                string filename = string.Format("record_{0}.json", frameCounter);
				var f = File.CreateText(GetLogPath() + filename);
				f.Write(json);
				f.Close();
            }
			else
			{
				writer.WriteLine(string.Format("{0},{1},{2},{3}", frameCounter.ToString(), activity, car.GetSteering().ToString(), car.GetThrottle().ToString()));
			}
		}

		if(lidar != null)
		{
			LidarPointArray pa = lidar.GetOutput();

			if(pa != null)
			{
				string json = JsonUtility.ToJson(pa);
				var filename = string.Format("lidar_{0}_{1}.txt", frameCounter.ToString(), activity);
				var f = File.CreateText(GetLogPath() + filename);
				f.Write(json);
				f.Close();
			}
		}

        if (optionlB_CamSensor != null)
        {
			SaveCamSensor(camSensor, camSensor2, activity, "_a");

            SaveCamSensor(optionlB_CamSensor, camSensor2, activity, "_b");
        }
        else
        {
			// Llama a una función que introduce una imagen en 
			// el array que introduce las imagenes
			SaveCamSensor(camSensor, camSensor2, activity, "");
        }

        if (maxFramesToLog != -1 && frameCounter >= maxFramesToLog)
        {
            Shutdown();
            this.gameObject.SetActive(false);
        }

        frameCounter = frameCounter + 1;

        if (logDisplay != null)
            logDisplay.text = "Log:" + frameCounter;
	}

	string GetUdacityStyleImageFilename(int mode = 1)
	{
		if (mode == 1)
		{
			// El nombre aquí es problemático ya que contiene la carpeta IMG. 
			// Para que funcione se debe crear esta carpeta.
			return GetLogPath() + string.Format("IMG/road_{0,8:D8}.png", frameCounter);
		} else {
			return GetLogPath() + string.Format("IMG2/road_{0,8:D8}.png", frameCounter);
		}
	}

    string GetDonkeyStyleImageFilename()
    {
        float steering = car.GetSteering() / car.GetMaxSteering();
        float throttle = car.GetThrottle();
        return GetLogPath() + string.Format("frame_{0,6:D6}_ttl_{1}_agl_{2}_mil_0.0.jpg", 
            frameCounter, throttle, steering);
    }

	string GetSharkStyleImageFilename()
    {
        int steering = (int)(car.GetSteering() / car.GetMaxSteering() * 32768.0f);
        int throttle = (int)(car.GetThrottle() * 32768.0f);
        return GetLogPath() + string.Format("frame_{0,6:D6}_st_{1}_th_{2}.jpg", 
            frameCounter, steering, throttle);
    }

    string GetDonkey2StyleImageFilename()
    {
        return GetLogPath() + string.Format("{0}_cam-image_array_.jpg", frameCounter);
    }

    //Save the camera sensor to an image. Use the suffix to distinguish between cameras.
    void SaveCamSensor(CameraSensor cs, CameraSensor cs2, string prefix, string suffix)
    {
        if (cs != null)
        {
            Texture2D image = cs.GetImage();
			Texture2D image2 = cs2.GetImage();
			ImageSaveJob ij = new ImageSaveJob();
			ImageSaveJob ij2 = new ImageSaveJob();

			// Tengo que haces la dos imagenes aunque no haga falta en todos los
			// casos porque si lo pongo en una condición me dan warnings.

			// Se comprueba que tipo de estilo se quiere para la imagen
			if (UdacityStyle)
			{
				// Se guarda el nombre que va a tener la imagen llamando a una función.
				ij.filename = GetUdacityStyleImageFilename();

				// Se codifica la imagen como JPG
				//ij.bytes = image.EncodeToJPG();
				// Se codifica la imagen como PNG
				ij.bytes = image.EncodeToPNG();

				if (nPhotos == 2)
				{
					ij2.filename = GetUdacityStyleImageFilename(2);

					// Se codifica la imagen como JPG
					//ij2.bytes = image2.EncodeToJPG();
					// Se codifica la imagen como PNG
					ij2.bytes = image2.EncodeToPNG();
				}
			}
            else if (DonkeyStyle)
            {
                ij.filename = GetDonkeyStyleImageFilename();

                ij.bytes = image.EncodeToJPG();
            }
            else if (DonkeyStyle2)
            {
                ij.filename = GetDonkey2StyleImageFilename();

                ij.bytes = image.EncodeToJPG();
            }
			else if(SharkStyle)
            {
                ij.filename = GetSharkStyleImageFilename();

                ij.bytes = image.EncodeToJPG();
            }
			else
			{
            	ij.filename = GetLogPath() + string.Format("{0}_{1,8:D8}{2}.png", prefix, frameCounter, suffix);

            	ij.bytes = image.EncodeToPNG();
			}

            lock (this)
            {
				// Se guarda la nueva imagen en imagesToSave
				// se hace en un lock para proteger la variable.
                imagesToSave.Add(ij);
				if (nPhotos == 2)
				{
					imagesToSave.Add(ij2);
				}
            }
        }
    }

	public void setPhotoMode(int mode)
	{
		nPhotos = mode;
	}

	public void SaverThread()
	{
		while(true)
		{
			int count = 0;

			// Mete en la variable count cuántas imagenes hay en
			// el array imagesToSave
			lock(this)
			{
				count = imagesToSave.Count; 
			}

			// Comprueba si hay más de una imagen
			if(count > 0)
			{
				ImageSaveJob ij = imagesToSave[0];

                Debug.Log("saving: " + ij.filename);

                File.WriteAllBytes(ij.filename, ij.bytes);

				lock(this)
				{
					imagesToSave.RemoveAt(0);
				}
			}
		}
	}

	public void Shutdown()
	{
		if(writer != null)
		{
			writer.Close();
			writer = null;
		}

		if(thread != null)
		{
			thread.Abort();
			thread = null;
		}

		bDoLog = false;
	}

	void OnDestroy()
	{
		Shutdown();
	}
}

