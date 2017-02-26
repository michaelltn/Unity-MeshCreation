using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct TrackPiece
{
	public Transform nodeA, nodeB;
	public Vector3[] vertices;
	public Vector2[] uvs;
	public int[,] triangles;
	
	// count only specifies the geometry for the rectangle
	// size includes the extra triangle to fill in the gap
	public int vertCount, vertSize;
	public int triCount, triSize;
}

public class TrackCreator : MonoBehaviour
{
	public Material chunkMaterial;
	public Vector2 uvBottomLeft, uvSize;
	public float trackWidth = 0.1f;
	private float tileHeight, tileWidth;
	public float drawDepth = 0.5f;
	
	public bool stretchPieces = false;
	
	public bool addMeshCollider = true;
	
	public bool flattenNodesToLocalZPlane = true;
	public bool deactivateNodesOnStart = true;
	public bool addNodesFromChildren = false;
	public Transform[] nodes;
	
	private static TrackPiece[] TrackData;
	private static List<Vector3> Vertices;
	private static List<Vector2> UVs;
	private static List<int> Triangles;
	
	static TrackCreator()
	{
		TrackData = null;
		Vertices = new List<Vector3>();
		UVs = new List<Vector2>();
		Triangles = new List<int>();
	}
	
	void Start ()
	{
		// check for invalid stuff and issue errors.
		// require:
		//   nodes.Length > 1
		//   no adjacent nodes overlap
		//   trackWidth > 0
		//   uvSize.x > 0 and ufSize.y > 0
		
    if (nodes.Length< 2) return;

		// tileHeight and tileWidth are in world units.
		tileHeight = trackWidth;
		tileWidth = trackWidth * uvSize.x / uvSize.y;
		
		// build the data for each track piece
		TrackData = new TrackPiece[nodes.Length-1];
		Vertices.Clear();
		UVs.Clear();
		Triangles.Clear();
	
		int i, t;
		Vector3 a, b, ab, v1, v2, n;
		Vector3 previousAB = Vector3.zero;
		
		Vector3 previousTopVert = Vector3.zero, previousBottomVert = Vector3.zero;
		Vector3 firstTopVert = Vector3.zero, firstBottomVert = Vector3.zero;
		Vector2 previousTopUV = Vector2.zero, previousBottomUV = Vector2.zero;
		Vector2 firstTopUV = Vector2.zero, firstBottomUV = Vector2.zero;
		
		float distanceRemaining = 0;
		float uvleft = 0, uvright = 0;
		int firstVert;
		for (t = 0; t < TrackData.Length; t++)
		{
			a = nodes[t].transform.localPosition;
			a.z = drawDepth;
			b = nodes[t+1].transform.localPosition;
			b.z = drawDepth;
			ab = b-a;
			
			v1 = a;
			n = Vector3.Cross(Vector3.forward, ab).normalized;
			i = 0;
			
			// add the wedge triangle
			if (t > 0)
			{
				if (stretchPieces)
					uvleft = 0;
				else if (distanceRemaining > 0)
					uvleft = 1f - (distanceRemaining/tileWidth);
				else
					uvleft = 0;
				firstBottomVert = v1 - (tileHeight*0.5f) * n;
				firstTopVert = v1 + (tileHeight*0.5f) * n;
				firstBottomUV = new Vector2(uvBottomLeft.x + (uvleft * uvSize.x), uvBottomLeft.y);
				firstTopUV = new Vector2(uvBottomLeft.x + (uvleft * uvSize.x), uvBottomLeft.y + uvSize.y);
				
				firstVert = Vertices.Count;
				
				if (Vector3.Cross(-previousAB, ab).z > 0)
				{
					// top wedge
					Vertices.Add(a);
					Vertices.Add(firstTopVert);
					Vertices.Add(previousTopVert);
					
					UVs.Add(firstTopUV - Vector2.up * uvSize.y * 0.5f);
					UVs.Add(firstTopUV);
					UVs.Add(previousTopUV);
				}
				else
				{
					// bottom wedge
					Vertices.Add(a);
					Vertices.Add(previousBottomVert);
					Vertices.Add(firstBottomVert);
					
					UVs.Add(firstBottomUV + Vector2.up * uvSize.y * 0.5f);
					UVs.Add(previousBottomUV);
					UVs.Add(firstBottomUV);
				}
					
				Triangles.Add(firstVert + 0);
				Triangles.Add(firstVert + 1);
				Triangles.Add(firstVert + 2);
			}
			
			bool endOfSection = false;
			while (!endOfSection)
			{
				if (stretchPieces)
				{
					v2 = b;
					uvleft = 0;
				}
				else if (distanceRemaining > 0)
				{
					v2 = v1 + ab.normalized * distanceRemaining;
					uvleft = 1f - (distanceRemaining/tileWidth);
				}
				else
				{
					v2 = v1 + ab.normalized * tileWidth;
					uvleft = 0;
				}
				
				if (stretchPieces)
				{
					previousBottomVert = v2 - (tileHeight*0.5f) * n;
					previousTopVert = v2 + (tileHeight*0.5f) * n;
					
					uvright = 1f;
					endOfSection = true;
				}
				else if (Vector3.Distance(a, v2) >= ab.magnitude)
				{
					distanceRemaining = Vector3.Distance(v2, b);
					v2 = b;
					uvright = 1f - (distanceRemaining/tileWidth);
					
					previousBottomVert = v2 - (tileHeight*0.5f) * n;
					previousTopVert = v2 + (tileHeight*0.5f) * n;
					previousBottomUV = new Vector2(uvBottomLeft.x + (uvright * uvSize.y), uvBottomLeft.y);
					previousTopUV = new Vector2(uvBottomLeft.x + (uvleft * uvSize.y), uvBottomLeft.y + uvSize.y);
					endOfSection = true;
				}
				else
				{
					distanceRemaining = 0;
					uvright = 1f;
				}
				
				firstVert = Vertices.Count;
				
				Vertices.Add(v1 - (tileHeight*0.5f) * n);
				Vertices.Add(v1 + (tileHeight*0.5f) * n);
				Vertices.Add(v2 - (tileHeight*0.5f) * n);
				Vertices.Add(v2 + (tileHeight*0.5f) * n);
				
				UVs.Add(new Vector2(uvBottomLeft.x + (uvleft * uvSize.x), uvBottomLeft.y));
				UVs.Add(new Vector2(uvBottomLeft.x + (uvleft * uvSize.x), uvBottomLeft.y + uvSize.y));
				UVs.Add(new Vector2(uvBottomLeft.x + (uvright * uvSize.x), uvBottomLeft.y));
				UVs.Add(new Vector2(uvBottomLeft.x + (uvright * uvSize.x), uvBottomLeft.y + uvSize.y));
				
				Triangles.Add(firstVert + 0);
				Triangles.Add(firstVert + 1);
				Triangles.Add(firstVert + 3);
				Triangles.Add(firstVert + 0);
				Triangles.Add(firstVert + 3);
				Triangles.Add(firstVert + 2);
				
				i += 4;
				v1 = v2;
			}
			
			previousAB = ab;
		}
		
		// build the mesh
		Mesh msh = new Mesh();
		msh.vertices = Vertices.ToArray();
		msh.triangles = Triangles.ToArray();
		msh.uv = UVs.ToArray();
		msh.RecalculateNormals();
		msh.RecalculateBounds();
		
		this.gameObject.AddComponent(typeof(MeshRenderer));
		MeshFilter filter = this.gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
		filter.mesh = msh;
		
		this.renderer.material = chunkMaterial;
		
		if (addMeshCollider)
		{
			//(this.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider).sharedMesh = msh;
			(this.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider).sharedMesh = MeshExtrusion.ExtrudeZ(msh, 1f);
		}
		
		if (deactivateNodesOnStart)
			foreach (Transform node in nodes)
				node.gameObject.SetActive(false);
		
	}
	
#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		if (addNodesFromChildren)
		{
			nodes = this.gameObject.GetComponentsInChildren<Transform>();
			addNodesFromChildren = false;
		}
		
		foreach (Transform t in UnityEditor.Selection.transforms)
			if (this.transform == t) return;
		
		if (nodes != null && nodes.Length > 1)
		{
			Gizmos.color = Color.magenta;
			for (int n = 0; n < nodes.Length-1; n++)
			{
				if (nodes[n] && nodes[n+1])
				{
					Gizmos.DrawLine(nodes[n].transform.position, nodes[n+1].transform.position);
				}
			}
		}
	}
	
	void OnDrawGizmosSelected()
	{
		if (nodes != null && nodes.Length > 1)
		{
			Gizmos.color = Color.green;
			for (int n = 0; n < nodes.Length-1; n++)
			{
				if (nodes[n] && nodes[n+1])
				{
					Gizmos.DrawLine(nodes[n].transform.position, nodes[n+1].transform.position);
				}
			}
		}
	}
#endif
}
