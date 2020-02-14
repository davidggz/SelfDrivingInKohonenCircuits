using UnityEngine;
using System.Collections;

public class PathManager : MonoBehaviour {

	public CarPath path;
	public CarPath path2;

	public GameObject prefab;

	public Transform startPos;
	public Transform startPos2;

	Vector3 span = Vector3.zero;

	public float spanDist = 5f;

	public int numSpans = 100;

	public float turnInc = 1f;

	public bool sameRandomPath = true;

	public int randSeed = 2;

	public bool doMakeRandomPath = true;

	public bool doDavidPath = false;

	public bool doLoadScriptPath = false;

	public bool doLoadPointPath = false;

	public bool doBuildRoad = false;

	public bool doChangeLanes = false;

	public int smoothPathIter = 0;

	public bool doShowPath = false;

    public string pathToLoad = "none";

	public RoadBuilder roadBuilder;
	public RoadBuilder semanticSegRoadBuilder;

	public LaneChangeTrainer laneChTrainer;

	void Awake () 
	{
		if(sameRandomPath)
			Random.InitState(randSeed);

		// Nada mas iniciar el juego, se comienza con una carretera.
		InitNewRoad();			
	}

	public void InitNewRoad()
	{
		// Con estas llamadas a funciones se genera el path
		// y se almacena en la variable path que es una instancia
		// de CarPath
		if(doMakeRandomPath)
		{
			MakeRandomPath();
		}
		else if (doLoadScriptPath)
		{
			MakeScriptedPath();
		}
		else if(doLoadPointPath)
		{
			MakePointPath();
		}
		else if (doDavidPath)
		{
			MakeDavidPath();
		}

		// Codigo introducido por mi.
		// Copio el path producido por una de las funciones anteriores y les
		// sumo un offset a todos los puntos para generar una carretera
		// en otro lugar del mundo.
		path2 = new CarPath();
		Vector3 s = startPos2.position;
		Vector3 diferenciaStartPos = startPos2.position - startPos.position;
		for (int i = 0; i < path.nodes.Count; i++)
		{
			Vector3 posicion = path.nodes[i].pos;
			PathNode p = new PathNode();
			p.pos = posicion + diferenciaStartPos;
			path2.nodes.Add(p);
		}
		// Fin de codigo introducido por mi

		if(smoothPathIter > 0)
			SmoothPath();

		// Should we build a road mesh along the path?
		// Una vez tenemos el path creado, tenemos que inicializar
		// la carretera mediante roadBuilder
		if (doBuildRoad && roadBuilder != null)
		{
			roadBuilder.InitRoad(path2, true);
			roadBuilder.InitRoad(path);
		}

		if (doBuildRoad && semanticSegRoadBuilder != null)
		{
			semanticSegRoadBuilder.InitRoad(path);
			semanticSegRoadBuilder.InitRoad(path);
		}

		if (laneChTrainer != null && doChangeLanes)
		{
			laneChTrainer.ModifyPath(ref path);
			laneChTrainer.ModifyPath(ref path2);
		}

		// No cambio esto porque doShowPath esta puesto en false por defecto
		if(doShowPath)
		{
			for(int iN = 0; iN < path.nodes.Count; iN++)
			{
				Vector3 np = path.nodes[iN].pos;
				GameObject go = Instantiate(prefab, np, Quaternion.identity) as GameObject;
				go.tag = "pathNode";
				go.transform.parent = this.transform;
			}
		}
	}

	public void DestroyRoad()
	{
		GameObject[] prev = GameObject.FindGameObjectsWithTag("pathNode");

		foreach(GameObject g in prev)
			Destroy(g);

		if(roadBuilder != null)
			roadBuilder.DestroyRoad();
	}

    public Vector3 GetPathStart()
    {
        return startPos.position;
    }

    public Vector3 GetPathEnd()
    {
        int iN = path.nodes.Count - 1;

        if(iN < 0)
            return GetPathStart();

        return path.nodes[iN].pos;
    }

	void SmoothPath()
	{
		while(smoothPathIter > 0)
		{
			path.SmoothPath();
			smoothPathIter--;
		}
	}

	void MakeDavidPath()
	{
		path = new CarPath();
		int numPoints = 20;
		Vector3[] kohonen = Kohonen.KohonenMain(numPoints);
		for (int i = 0; i < numPoints; i++)
		{
			PathNode p = new PathNode();
			p.pos = kohonen[i];
			path.nodes.Add(p);

			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.position = kohonen[i];
			sphere.transform.localScale = new Vector3(10, 0.1f, 10);

			Debug.Log(kohonen[i]);
		}


	}

	void MakePointPath()
	{
		string filename = "thunder_path";

		TextAsset bindata = Resources.Load(filename) as TextAsset;

		if(bindata == null)
			return;

		string[] lines = bindata.text.Split('\n');

		Debug.Log(string.Format("found {0} path points. to load", lines.Length));

		path = new CarPath();

		Vector3 np = Vector3.zero;

		float offsetY = -0.1f;

		foreach(string line in lines)
		{
			string[] tokens = line.Split(',');

			if (tokens.Length != 3)
				continue;
			np.x = float.Parse(tokens[0]);
			np.y = float.Parse(tokens[1]) + offsetY;
			np.z = float.Parse(tokens[2]);
			PathNode p = new PathNode();
			p.pos = np;
			path.nodes.Add(p);
		}
			
	}

	void MakeScriptedPath()
	{
		TrackScript script = new TrackScript();

		if(script.Read(pathToLoad))
		{
			path = new CarPath();
			TrackParams tparams = new TrackParams();
			tparams.numToSet = 0;
			tparams.rotCur = Quaternion.identity;
			tparams.lastPos = startPos.position;

			float dY = 0.0f;
			float turn = 0f;

			Vector3 s = startPos.position;
			s.y = 0.5f;
			span.x = 0f;
			span.y = 0f;
			span.z = spanDist;
			float turnVal = 10.0f;

			foreach(TrackScriptElem se in script.track)
			{
				if(se.state == TrackParams.State.AngleDY)
				{
					turnVal = se.value;
				}
				else if(se.state == TrackParams.State.CurveY)
				{
					turn = 0.0f;
					dY = se.value * turnVal;
				}
				else
				{
					dY = 0.0f;
					turn = 0.0f;
				}

				for(int i = 0; i < se.numToSet; i++)
				{

					Vector3 np = s;
					PathNode p = new PathNode();
					p.pos = np;
					path.nodes.Add(p);

					turn = dY;

					Quaternion rot = Quaternion.Euler(0.0f, turn, 0f);
					span = rot * span.normalized;
					span *= spanDist;
					s = s + span;
				}
					
			}
		}
	}

	void MakeRandomPath()
	{
		path = new CarPath();

		//Vector3 s = startPos.position;
		// Este vector es la nueva posicion inicial. Esta puesto asi
		// para que no ocurra el bug de que el coche no se encuentre
		// en el inicio de la carretera al regenerarla

		Vector3 s = new Vector3(50, 0.633f, 50);
		Debug.Log(startPos.position);
		float turn = 0f;
		s.y = 0.5f;

		span.x = 0f;
		span.y = 0f;
		span.z = spanDist;
		// Esto marca el inicio de la carretera, tiene que ser
		// la posicion del coche, que justamente es la que tengo puesta.
		// Vector3 david = new Vector3(50, 0.5f, 50);

		for (int iS = 0; iS < numSpans; iS++)
		{
			Vector3 np = s;
			PathNode p = new PathNode();
			p.pos = np;
			// p.pos = david
			//david[0] = david[0] + 1M
			// // Con este código se puede poner una esfera en todos puntos
			// // que componen la carretera.
			//GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			//sphere.transform.position = np;

			// El array en el que debemos poner los puntos que generen la carretera
			// es path.
			path.nodes.Add(p);

			float t = Random.Range(-1.0f * turnInc, turnInc);

			turn += t;

			Quaternion rot = Quaternion.Euler(0.0f, turn, 0f);
			span = rot * span.normalized;

			if(SegmentCrossesPath( np + (span.normalized * 100.0f), 90.0f ))
			{
				//turn in the opposite direction if we think we are going to run over the path
				turn *= -0.5f;
				rot = Quaternion.Euler(0.0f, turn, 0f);
				span = rot * span.normalized;
			}

			span *= spanDist;

			s = s + span;
		}
	}

	public bool SegmentCrossesPath(Vector3 posA, float rad)
	{
		foreach(PathNode pn in path.nodes)
		{
			float d = (posA - pn.pos).magnitude;

			if(d < rad)
				return true;
		}

		return false;
	}

	public void SetPath(CarPath p)
	{
		path = p;

		GameObject[] prev = GameObject.FindGameObjectsWithTag("pathNode");

		Debug.Log(string.Format("Cleaning up {0} old nodes. {1} new ones.", prev.Length, p.nodes.Count));

		DestroyRoad();

		foreach(PathNode pn in path.nodes)
		{
			GameObject go = Instantiate(prefab, pn.pos, Quaternion.identity) as GameObject;
			go.tag = "pathNode";
		}
	}
}
