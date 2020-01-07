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
		Vector3 randPosition = new Vector3();
		do {
			// Posicion aleatoria
			randPosition[0] = UnityEngine.Random.Range(fromXSpread, toXSpread);
			randPosition[1] = itemYSpread;
			randPosition[2] = UnityEngine.Random.Range(fromZSpread, toZSpread);
		} while (checkPosition(randPosition, path, RoadWidth) == true);

		// Rotacion aleatoria
		Quaternion randYRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
		// Objeto aleatorio
		int randomIndex = PickRandom();
		GameObject clone = Instantiate(itemsToPickFrom[randomIndex], randPosition, randYRotation);
		clone.transform.parent = transform;
	}
	bool checkPosition (Vector3 pos, CarPath path, float RoadWidth)
	{
		foreach (PathNode node in path.nodes)
		{
			if (Vector3.Distance(node.pos, pos) < RoadWidth)
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
