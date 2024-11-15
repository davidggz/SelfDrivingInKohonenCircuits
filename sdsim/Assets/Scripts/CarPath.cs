﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathNode
{
	public Vector3 pos;
	// CarModel es una clase que se instancia pero 
	// cuyas funciones no se llaman nunca. Asi que
	// no tiene mucha importancia.
	public CarModel cm;
	public string activity;
}

public class CarPath 
{

	public List<PathNode> nodes;
	public int iActiveSpan = 0;

	// Constructor por defecto de CarPath
	public CarPath()
	{
		// Genera una lista de nodos
		nodes = new List<PathNode>();

		// Resetea a 0 iActiveSpan
		ResetActiveSpan();
	}

	public void ResetActiveSpan()
	{
		iActiveSpan = 0;
	}


	public PathNode GetActiveNode()
	{
		if (iActiveSpan < nodes.Count)
			return nodes[iActiveSpan];

		return null;
	}

	// No sé como funciona pero parece que lo que hace es 
	// suavizar los caminos. He probado a poner un mensaje 
	// de debug y no he conseguido que ejecutase nada
	public void SmoothPath(float factor = 0.5f)
	{
		LineSeg3d.SegResult segRes = new LineSeg3d.SegResult();

		for(int iN = 1; iN < nodes.Count - 2; iN++)
		{
			PathNode p = nodes[iN - 1];
			PathNode c = nodes[iN];
			PathNode n = nodes[iN + 1];

			LineSeg3d seg = new LineSeg3d(ref p.pos, ref n.pos);
			Vector3 closestP = seg.ClosestPointOnSegmentTo(ref c.pos, ref segRes);
			Vector3 dIntersect = closestP - c.pos;
			c.pos += dIntersect.normalized * factor;
		}
	}

	// Comprueba si una posición está en una zona de carretera cruzada.
	public bool GetCrossTrackErr(Vector3 pos, ref float err)
	{
		if(iActiveSpan >= nodes.Count - 2)
			return false;

		PathNode a = nodes[iActiveSpan];
		PathNode b = nodes[iActiveSpan + 1];

		//2d path.
		pos.y = a.pos.y;

		// Se genera una línea desde la posición a a la posición b
		LineSeg3d pathSeg = new LineSeg3d(ref a.pos, ref b.pos);

		// Se dibuja una línea verde en el juego desde el inicio dicho anteriormente al siguiente
		pathSeg.Draw( Color.green);		

		// Se inicializa el enum SegResult teniendo en cuenta que
		// se está en el contexto de LineSeg3d (algo así)
		LineSeg3d.SegResult segRes = new LineSeg3d.SegResult();

		Vector3 closePt = pathSeg.ClosestPointOnSegmentTo(ref pos, ref segRes);  

		Debug.DrawLine(a.pos, closePt, Color.blue);

		if(segRes == LineSeg3d.SegResult.GreaterThanEnd)
		{
			iActiveSpan++;
		}
		else if(segRes == LineSeg3d.SegResult.LessThanOrigin)
		{
			if(iActiveSpan > 0)
				iActiveSpan--;
		}

		Vector3 errVec = pathSeg.ClosestVectorTo( ref pos );

		Debug.DrawRay(closePt, errVec, Color.white);

		float sign = 1.0f;

		Vector3 cp = Vector3.Cross(pathSeg.m_dir.normalized, errVec.normalized);

		if(cp.y > 0.0f)
			sign = -1f;

		err = errVec.magnitude * sign;
		return true;
	}

}