using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadBuilder : MonoBehaviour {

	public float roadWidth = 1.0f;
	public float roadHeightOffset = 0.0f;
	public float roadOffsetW = 0.0f;
	public bool doFlattenAtStart = true;
	public bool doErodeTerrain = true;
	public bool doGenerateTerrain = true;
	public bool doFlattenArroundRoad = true;
	public bool doLiftRoadToTerrain = false;

	public Terrain terrain;

	// No se exactamente cuando se inicializa este objeto
	// pero parece que se ha hecho en la interfaz grafica.
	// Un GameObject en la jerarquía que se llama WorldBuilder.
	// Dentro de su componente RoadBuilder sale un parametro
	// llamado RoadPrefabMesh que contiene el RoadPrefab a mano.
	public GameObject roadPrefabMesh;
	public GameObject roadPrefabMesh2;

	public TerrainToolkit terToolkit;

	// Este parametro es el indice que representa a cada 
	// tipo de carretera distinto (tipo visual)
	public int iRoadTexture = 0;
	public Texture2D[] roadTextures;
	public float[] roadOffsets;
	public float[] roadWidths;

	public Texture2D texturaSecundaria;
	public bool utilizarTexSecundaria;

	Texture2D customRoadTexure;

	GameObject createdRoad;

	public DecorationSpreader Deco;

	void Start()
	{
		if(terToolkit != null && doErodeTerrain)
		{
			//terToolkit.FastThermalErosion(20, 0.0f, 0.0f); //creates pits
			//terToolkit.FastHydraulicErosion(100, 1.0f, 0.0f); //creates washouts
			//terToolkit.FullHydraulicErosion(1, 10.0f, 1.0f, .3f, 2.0f);
			terToolkit.SmoothTerrain(10, 1.0f);
		}
	}

	public void DestroyRoad()
	{
		GameObject[] prev = GameObject.FindGameObjectsWithTag("road_mesh");

		foreach(GameObject g in prev)
			Destroy(g);

		//Destruye la decoracion que se haya podido introducir
		Deco.DestroyDecoration();

		//advance road index into texture list.
		iRoadTexture += 1;
	}

	public void SetNewRoadVariation(int iVariation)
	{
		if(roadTextures.Length > 0)		
			customRoadTexure = roadTextures[ iVariation % roadTextures.Length ];

		if(roadOffsets.Length > 0)
			roadOffsetW = roadOffsets[ iVariation % roadOffsets.Length ];

		if(roadWidths.Length > 0)
			roadWidth = roadWidths[ iVariation % roadWidths.Length ];
		
	}

	public void NegateYTiling()
	{
		//todo
		if(createdRoad == null)
			return;
		
		MeshRenderer mr = createdRoad.GetComponent<MeshRenderer>();
		Vector2 ms = mr.material.mainTextureScale;
		ms.y *= -1.0f;
		mr.material.mainTextureScale = ms;
	}

	public void InitRoad(CarPath path, bool generateDeco = false)
	{
		// La verdad es que no se que hace esto, no encuentró
		// la funcion de la API. Aquí no entra cuando genero el mapa.
		if(terToolkit != null && doFlattenAtStart)
		{
			terToolkit.Flatten();
		}

		// Aqui tampoco entra cuando genero el mapa.
		if(terToolkit != null && doGenerateTerrain)
		{
			terToolkit.PerlinGenerator(1, 0.1f, 10, 0.5f);
			//terToolkit.NormaliseTerrain(0.0f, 0.001f, 0.5f);
		}
		
		// Si estamos en la primera carretera de la lista 
		// se pone el road prefab más simple.
		GameObject go = GameObject.Instantiate(roadPrefabMesh);
		/*if(iRoadTexture == 0)
		{
			go = GameObject.Instantiate(roadPrefabMesh2);
		}*/
		// MeshFilter siempre contiene la red en sí.
		// MeshRenderer contiene como se comporta la red con respecto a la luz.
		MeshRenderer mr = go.GetComponent<MeshRenderer>();
		MeshFilter mf = go.GetComponent<MeshFilter>();
		// Se genera la red
		Mesh mesh = new Mesh();
		// Le asignamos la red al componente MeshFilter.
		mf.mesh = mesh;
		createdRoad = go;

		// A esto no se entra y tampoco se exactamente
		// para que sirve. Supongo que es para meter una
		// textura personalizada.
		if(customRoadTexure != null)
		{
			mr.material.mainTexture = customRoadTexure;
		}
		else if(roadTextures != null && iRoadTexture < roadTextures.Length)
		{
			// Se almacena en t la textura que se va a usar.
			Texture2D t = roadTextures[iRoadTexture];

			if(utilizarTexSecundaria == true && generateDeco == false)
			{
				t = texturaSecundaria;
			}

			if(mr != null && t != null)
			{
				// Se le pone esta textura al meshrenderer
				mr.material.mainTexture = t;
			}
		}



		// Comienza el algoritmo de la muerte de construir la carretera.
		go.tag = "road_mesh";

		// Un Quad es basicamente un cuadrado
		int numQuads = path.nodes.Count - 1;
		// Una fila de Quads generan un camino con (numQuads+1)*2 vertices
		// Por ejemplo, 2 quads seguidos, tienen 6 vertices.
		int numVerts = (numQuads + 1) * 2;
		// Cada Quad tiene dos triangulos.
		int numTris = numQuads * 2;

		Vector3[] vertices = new Vector3[numVerts];

		// No se refiere al numero total de indices, se refiere al número
		// total de indices necesarios para definir los numQuads*2 cuadrados
		int numTriIndecies = numTris * 3;

		// Array de enteros con tantas posiciones como numero hemos calculado previamente
		int[] tri = new int[numTriIndecies];

		int numNormals = numVerts;
		Vector3[] normals = new Vector3[numNormals];

		int numUvs = numVerts;
		Vector2[] uv = new Vector2[numUvs];
		
		// Se inicializa "normals" con un vector 0, 1, 0
		for(int iN = 0; iN < numNormals; iN++)
			normals[iN] = Vector3.up;

		// Los nodos hacen referencia a las "esferas" que conforman
		// el path que se ha generado con MakeRandom o lo que sea
		// y que se pasan por parametro.
		int iNode = 0;

		Vector3 posA = Vector3.zero;
		Vector3 posB = Vector3.zero;

		Vector3 vLength = Vector3.one;
		Vector3 vWidth = Vector3.one;

		for(int iVert = 0; iVert < numVerts; iVert += 2)
		{

			if(iNode + 1 < path.nodes.Count)
			{
				PathNode nodeA = path.nodes[iNode];
				PathNode nodeB = path.nodes[iNode + 1];
				posA = nodeA.pos;
				posB = nodeB.pos;

				vLength = posB - posA;
				vWidth = Vector3.Cross(vLength, Vector3.up);

				// Estas dos condiciones hacen referencia a algunos parametros que estan 
				// en WorldBuilder pero que parece ser que son utiles cuando se introduce un Terrain
				// Este terrain tiene que ser introducido por el usuario.
				if(terToolkit != null && doFlattenArroundRoad  && (iVert % 10) == 0)
				{
					terToolkit.FlattenArround(posA + vWidth.normalized * roadOffsetW, 10.0f, 30.0f);
				}

				if(doLiftRoadToTerrain)
				{
					posA.y = terrain.SampleHeight(posA) + 1.0f;
				}

				posA.y += roadHeightOffset;
			}
			else
			{
				PathNode nodeA = path.nodes[iNode];
				posA = nodeA.pos;
				posA.y += roadHeightOffset;
			}

			Vector3 leftPos = posA + vWidth.normalized * roadWidth + vWidth.normalized * roadOffsetW;
			Vector3 rightPos = posA - vWidth.normalized * roadWidth + vWidth.normalized * roadOffsetW;

			vertices[iVert] = leftPos;
			vertices[iVert + 1] = rightPos;

			uv[iVert] = new Vector2(0.2f * iNode, 0.0f);
			uv[iVert + 1] = new Vector2(0.2f * iNode, 1.0f);

			iNode++;
		}

		if(generateDeco == true)
		{
			Deco.SpreadItems(path, roadWidth);
		}

		int iVertOffset = 0;
		int iTriOffset = 0;

		for(int iQuad = 0; iQuad < numQuads; iQuad++)
		{
			tri[0 + iTriOffset] = 0 + iVertOffset;
			tri[1 + iTriOffset] = 2 + iVertOffset;
			tri[2 + iTriOffset] = 1 + iVertOffset;

			tri[3 + iTriOffset] = 2 + iVertOffset;
			tri[4 + iTriOffset] = 3 + iVertOffset;
			tri[5 + iTriOffset] = 1 + iVertOffset;

			iVertOffset += 2;
			iTriOffset += 6;
		}


		mesh.vertices = vertices;
		mesh.triangles = tri;
		mesh.normals = normals;
		mesh.uv = uv;

		mesh.RecalculateBounds();



		if(terToolkit != null && doErodeTerrain)
		{
			//terToolkit.FastThermalErosion(20, 0.0f, 0.0f); //creates pits
			//terToolkit.FastHydraulicErosion(100, 1.0f, 0.0f); //creates washouts
			//terToolkit.FullHydraulicErosion(1, 10.0f, 1.0f, .3f, 2.0f);
			terToolkit.SmoothTerrain(10, 1.0f);

			if(doFlattenArroundRoad)
			{
				foreach(PathNode n in path.nodes)
				{
					terToolkit.FlattenArround(n.pos, 8.0f, 10.0f);
				}
			}

			float[] slopeStops = new float[2];
			float[] heightStops = new float[2];

			slopeStops[0] = 1.0f;
			slopeStops[1] = 2.0f;

			heightStops[0] = 4.0f;
			heightStops[1] = 10.0f;

			//terToolkit.TextureTerrain(slopeStops, heightStops, textures);
		}
	}
}
