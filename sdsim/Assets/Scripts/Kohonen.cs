using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;public static class Kohonen{	public static Vector3 car1InitialPos = new Vector3(50, 0.5f, 50);	public static int rangePoints = 300;	public static Texture2D cityColor = (Texture2D)Resources.Load("city");	public static Vector3[] genRandomPoints(int numElem)
	{
		Vector3[] randPoints = new Vector3[numElem];

		for(int i = 0; i < randPoints.Length; i++)
		{
			randPoints[i] = randomPosition();
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			/*sphere.transform.position = randPoints[i];
			sphere.transform.localScale = new Vector3(10, 0.6f, 10);
			sphere.GetComponent<Renderer>().material.mainTexture = cityColor;*/
		}
		return randPoints;
	}	public static Vector3[] generate_network(int size)
	{
		Vector3[] network = new Vector3[size];
		for (int i = 0; i < network.Length; i++)
		{
			network[i] = randomPosition();
		}

		return network;
	}	public static Vector3[] KohonenMain(int points)
	{
		Vector3[] problem = genRandomPoints(points);

		// Aquí se introducen las iteraciones
		return SOM(problem, 4000);
	}	public static Vector3[] SOM(Vector3[] problem, int iterations, double learning_rate = 0.8)
	{
		// El numero de neuronas es 8 veces el numero de puntos que haya en el mapa
		int nNeuronas = problem.Length * 8;

		// Genera la red
		Vector3[] network = generate_network(nNeuronas);

		for(int i = 0; i < network.Length; i++)
		{
			//Debug.Log(network[i]);
			/*GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.position = network[i];
			sphere.transform.localScale = new Vector3(10, 0.1f, 10);*/
		}
		Debug.Log("Red de " + network.Length + " neuronas creada.");

		double vecindario = (double)nNeuronas;

		for(int i = 0; i < iterations; i++)
		{
			if (i != 0 && iterations % i == 0)
			{
				Debug.Log("\t> Iteration " + i + "/" + iterations);
			}
			// Itero por todas las ciudades
			for (int ciudadIndex = 0; ciudadIndex < problem.Length; ciudadIndex++)
			{
				//Elige una ciudad aleatoria
				Vector3 city = problem[ciudadIndex];
				//Debug.Log("Ciudad ganadora: " + city);

				int winnerInd = distance.select_closest(network, city);
				/*Debug.Log("Winner index: " + winnerInd);
				Debug.Log("Winner position: " + network[winnerInd]);*/

				// Genera la gaussiana que se aplica a todas las neuronas cercanas al ganador
				double[] gaussian = get_neighborhood(winnerInd, vecindario / 10, network.Length);
				/*for (int p = 0; p < gaussian.Length; p++)
				{
					Debug.Log("Gaussian: " + gaussian[p]);
				}
				Debug.Log("Winner: " + winnerInd);*/
				Vector3 distanciaXZ;
				// Paralelizable
				for (int n = 0; n < network.Length; n++)
				{
					distanciaXZ = city - network[n];
					network[n] += distanciaXZ * (float)gaussian[n] * (float)learning_rate;
				}
			}

			learning_rate = learning_rate * 0.6;
			//No se si esto es asi.
			vecindario = vecindario * 0.75;

			if(vecindario < 1)
			{
				Debug.Log("El radio ha caido completamente. Finalizando ejecucion. ");
				break;
			}

			if(learning_rate < 0.001)
			{
				Debug.Log("La razon de aprendizaje ha caido por completo. Finalizando ejecucion. ");
				break;
			}
		}

		return network;
	}	public static double[] get_neighborhood(int center, double radix, int domain)
	{
		if (radix < 1){
			radix = 1;
		}


		/* Calcula la gaussiana elemento a elemento.
		 * Esto se podra paralelizar en un futuro.
		 */
		int[] deltas = new int[domain];
		int[] distances = new int[domain];
		double[] gaussiana = new double[domain];

		for (int i = 0; i < domain; i++)
		{
			deltas[i] = Math.Abs(i - center);

			distances[i] = Math.Min(deltas[i], domain - deltas[i]);

			gaussiana[i] = Math.Exp(-(distances[i] * distances[i]) / (2 * radix * radix));
		}

		return gaussiana;
	}	public static Vector3 randomPosition()
	{
		Vector3 randPosition = new Vector3();

		// Posicion aleatoria
		randPosition[0] = UnityEngine.Random.Range(car1InitialPos[0] - rangePoints, car1InitialPos[0] + rangePoints);
		randPosition[1] = car1InitialPos[1];
		randPosition[2] = UnityEngine.Random.Range(car1InitialPos[2] - rangePoints, car1InitialPos[2] + rangePoints);

		return randPosition;
	}}public static class distance
{
	public static int select_closest(Vector3[] candidates, Vector3 point)
	{
		float min = Vector3.Distance(candidates[0], point);
		int indexMin = 0;
		float newMin;

		//Recorre todos los candidatos en busca del mas cercano al punto
		for (int i = 1; i < candidates.Length; i++)
		{
			newMin = Vector3.Distance(candidates[i], point);
			if (newMin < min)
			{
				indexMin = i;
				min = newMin;
			}
		}

		return indexMin;
	}
}