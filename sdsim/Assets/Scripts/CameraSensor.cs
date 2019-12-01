using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSensor : MonoBehaviour {

	public Camera sensorCam;
	public int width = 256;
	public int height = 256;

	Texture2D tex;
	RenderTexture ren;

	void Awake()
	{
		// Esta linea genera una especie de "grid" en el que estará
		// contenida la imagen. Es parecido a un fig de plt.
		tex = new Texture2D(width, height, TextureFormat.RGB24, false);
		ren = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
		sensorCam.targetTexture = ren;
	}

	Texture2D RTImage(Camera cam) 
	{
		RenderTexture currentRT = RenderTexture.active;
		RenderTexture.active = cam.targetTexture;
		cam.Render();
		// Con esta función, se lee la imagen.
		// El primer parametro indica la sección a la que hacerle la foto
		// Los otros dos parámetros son cuánto de pantalla se quiere obtener
		// en la imagen.
		tex.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
		tex.Apply();
		RenderTexture.active = currentRT;


		return tex;
	}

	public Texture2D GetImage()
	{
		return RTImage(sensorCam);
	}

	// El único punto en el que se envía esta función
	// es en la clase TcpCarHandler que se encarga de enviar
	// mensajes al servidor del autor con el objetivo de inferir
	// en una red de neuronas y obtener respuestas
	public byte[] GetImageBytes()
	{
		return GetImage().EncodeToJPG();
	}
}
