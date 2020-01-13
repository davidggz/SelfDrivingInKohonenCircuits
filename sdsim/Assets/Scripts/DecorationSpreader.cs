using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorationSpreader : MonoBehaviour
{
	public GameObject[] itemsToPickFrom;
	public int numItemsToSpread = 1000;

	// Posicion inicial del segundo coche
	public Vector3 car2InitialPos = new Vector3(3000, 0, 50);
	public int range = 500;

	public float fromXSpread;
	public float toXSpread;
	public float itemYSpread;
	public float fromZSpread;
	public float toZSpread;

	public Boolean scaleUniformly = true;

	public float uniformScaleMax = 5f;
	public float uniformScaleMin = 1f;

	public float xScaleMax = 3f;
	public float xScaleMin = .1f;
	public float yScaleMax = 3f;
	public float yScaleMin = .1f;
	public float zScaleMax = 3f;
	public float zScaleMin = .1f;

	public float globalScaleMultiplier = 1f;

	void Start() {
		//SpreadItems();
    }

	int PickRandom()
	{
		int randomIndex = UnityEngine.Random.Range(0, itemsToPickFrom.Length);
		return randomIndex;
	}

	void SpreadItem (CarPath path, float RoadWidth)
	{
		// Posicion aleatoria
		Vector3 randPosition = randomPosition(path, RoadWidth);
		// Rotacion aleatoria
		Quaternion randYRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
		// Tamaño aleatorio
		Vector3 randSize = randomSize();
		// Objeto aleatorio
		int randomIndex = PickRandom();
		// Objeto que vamos a introducir
		GameObject clone = Instantiate(itemsToPickFrom[randomIndex], randPosition, randYRotation);
		clone.transform.localScale = randSize;
		clone.transform.parent = transform;
	}

	Vector3 randomSize()
	{
		Vector3 randScale = Vector3.one;

		if (scaleUniformly) {
			float uniformScale = UnityEngine.Random.Range(uniformScaleMin, uniformScaleMax);
			randScale = new Vector3(uniformScale, uniformScale, uniformScale);
		} else {
			randScale = new Vector3(UnityEngine.Random.Range(xScaleMin, xScaleMax), UnityEngine.Random.Range(yScaleMin, yScaleMax), UnityEngine.Random.Range(zScaleMin, zScaleMax));
		}

		return randScale * globalScaleMultiplier;
		
	}

	Vector3 randomPosition(CarPath path, float RoadWidth)
	{
		Vector3 randPosition = new Vector3();
		// Se cogen posiciones aleatorias hasta que se encuentra una que 
		// no está dentro de la carretera.
		do
		{
			// Posicion aleatoria
			randPosition[0] = UnityEngine.Random.Range(fromXSpread, toXSpread);
			randPosition[1] = itemYSpread;
			randPosition[2] = UnityEngine.Random.Range(fromZSpread, toZSpread);
		} while (checkPosition(randPosition, path, RoadWidth) == true);

		return randPosition;
	}

	bool checkPosition (Vector3 pos, CarPath path, float RoadWidth)
	{
		foreach (PathNode node in path.nodes)
		{
			// La comprobación se hace sumando una cantidad debido a que el tamaño del objeto
			// puede hacer que se siga montando en la carretera.
			if (Vector3.Distance(node.pos, pos) < RoadWidth + 5)
			{
				//Debug.Log("TRUE");
				return true;
			}
		}
		//Debug.Log("FALSE");
		return false;
	}

	public void SpreadItems (CarPath path, float RoadWidth)
	{
		// Con este código comentado, se generan unos circulos encima de la carretera
		// que tienen un radio igual a lo que se supone que es el ancho de la carretera.
		/*foreach (PathNode node in path.nodes)
		{
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.position = node.pos;
			sphere.transform.localScale = new Vector3(RoadWidth, 0.1f, RoadWidth);
		}*/
		fromXSpread = car2InitialPos[0] - range;
		toXSpread = car2InitialPos[0] + range;
		itemYSpread = 0;
		fromZSpread = car2InitialPos[2] - range;
		toZSpread = car2InitialPos[2] + range;

		for (int i = 0; i < numItemsToSpread; i++)
		{
			SpreadItem(path, RoadWidth);
		}
	}

	public void DestroyDecoration()
	{
		foreach(Transform child in transform)
		{
			Destroy(child.gameObject);
		}
	}
}
