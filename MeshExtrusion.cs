using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// http://stackoverflow.com/questions/3848923/how-to-extrude-a-flat-2d-mesh-giving-it-depth

struct IntPair
{
	public int a, b;
}

public class MeshExtrusion
{
	public static Mesh ExtrudeZ(Mesh flatMesh, float depth)
	{
		Mesh extrudedMesh = new Mesh();
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		
		int n = flatMesh.vertices.Length;
		int i, a, b;
		
		foreach (Vector3 v in flatMesh.vertices)
		{
			vertices.Add( new Vector3(v.x, v.y, -depth * 0.5f) );
		}
		foreach (Vector3 v in flatMesh.vertices)
		{
			vertices.Add( new Vector3(v.x, v.y, depth * 0.5f) );
		}
		
		List<IntPair> edgeList = new List<IntPair>();
		bool oppositeFound;
		for (i = 0; i < flatMesh.triangles.Length - 1; i++)
		{
			a = i;
			if (a % 3 == 2)
				b = i - 2;
			else
				b = i + 1;
			
			IntPair ab = new IntPair();
			ab.a = flatMesh.triangles[a];
			ab.b = flatMesh.triangles[b];
			
			oppositeFound = false;
			foreach (IntPair edge in edgeList)
			{
				if (ab.a == edge.b && ab.b == edge.a)
				{
					edgeList.Remove(edge);
					oppositeFound = true;
					break;
				}
			}
			if (!oppositeFound)
			{
				edgeList.Add(ab);
			}
		}
		
		foreach (IntPair edge in edgeList)
		{
			a = edge.a;
			b = edge.b;
			triangles.Add( b );
			triangles.Add( a );
			triangles.Add( b + n );
			
			triangles.Add( a + n );
			triangles.Add( b + n );
			triangles.Add( a );
		}
		
		extrudedMesh.vertices = vertices.ToArray();
		extrudedMesh.triangles = triangles.ToArray();
		extrudedMesh.RecalculateNormals();
		extrudedMesh.RecalculateBounds();
		
		return extrudedMesh;
	}

}
