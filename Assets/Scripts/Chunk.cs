using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimplexNoise;

[RequireComponent (typeof(MeshRenderer))]
[RequireComponent (typeof(MeshCollider))]
[RequireComponent (typeof(MeshFilter))]

public class Chunk : MonoBehaviour 
{
	public static List<Chunk> chunks = new List<Chunk>();
	public static int width
	{
		get { return World.currentWorld.chunkWidth;}
	}
	public static int height
	{
		get { return World.currentWorld.chunkHeight;}
	}
	public byte[,,] map;


	//Meshes
	public Mesh visualMesh;
	protected MeshRenderer meshRenderer;
	protected MeshCollider meshCollider;
	protected MeshFilter meshFilter;

	// Use this for initialization
	void Start () {

		chunks.Add(this);
		meshRenderer = GetComponent<MeshRenderer>();
		meshCollider = GetComponent<MeshCollider>();
		meshFilter = GetComponent<MeshFilter>();

		
		map = new byte[width, width, height];

		for (int x=0; x < width; x++)
		{
			float noiseX = (float) x/20;
			for (int y=0; y < height; y++)
			{
				float noiseY = (float) y/20;
				for (int z=0; z < width; z++)
				{
					float noiseZ = (float) z/20;
					float noiseValue = Noise.Generate(noiseX,noiseY,noiseZ);
					noiseValue += (10f-(float)y) /10;
					noiseValue /= (float) y/5;

					if (noiseValue > 0.2f)
						map[x,y,z] = 1;

				}
			}
		}
		StartCoroutine(CreateVisualMesh());
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public virtual IEnumerator CreateVisualMesh()
	{
		visualMesh = new Mesh ();
		List<Vector3> verts = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> tris = new List<int>();

		//3d loop
		for (int x=0; x < width; x++)
		{
			for (int y=0; y < height; y++)
			{
				for (int z=0; z < width; z++)
				{
					if (map[x,y,z] == 0) continue;
					byte brick = map[x,y,z];
					//left wall
					if (!IsTransparent(x-1,y,z))
						BuildFace (brick, new Vector3(x,y,z), Vector3.up, Vector3.forward, false, verts, uvs, tris);
					//right
					if (!IsTransparent(x+1,y,z))
						BuildFace (brick, new Vector3(x +1 ,y,z), Vector3.up, Vector3.forward, true, verts, uvs, tris);

					//bottom
					if (!IsTransparent(x,y-1,z))
						BuildFace (brick, new Vector3(x,y,z), Vector3.forward, Vector3.right, false, verts, uvs, tris);
					//top
					if (!IsTransparent(x,y+1,z))
						BuildFace (brick, new Vector3(x ,y+1,z), Vector3.forward, Vector3.right, true, verts, uvs, tris);

					//back
					if (!IsTransparent(x,y,z-1))
						BuildFace (brick, new Vector3(x,y,z), Vector3.up, Vector3.right, true, verts, uvs, tris);
					//front
					if (!IsTransparent(x,y,z+1))
						BuildFace (brick, new Vector3(x ,y,z +1), Vector3.up, Vector3.right, false, verts, uvs, tris);
				}
			}
		}

		visualMesh.vertices = verts.ToArray ();
		visualMesh.uv = uvs.ToArray ();
		visualMesh.triangles = tris.ToArray ();
		visualMesh.RecalculateBounds ();
		visualMesh.RecalculateNormals ();

		meshFilter.mesh = visualMesh;
		meshCollider.sharedMesh = visualMesh;
		
		yield return 0;
	}

	public virtual void BuildFace(byte brick, Vector3 corner, Vector3 up, Vector3 right, bool reversed, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
	{
		int index = verts.Count;

		verts.Add (corner);
		verts.Add (corner + up);
		verts.Add (corner + up + right);
		verts.Add (corner + right);

		uvs.Add (new Vector2 (0, 0));
		uvs.Add (new Vector2 (0, 1));
		uvs.Add (new Vector2 (1, 0));
		uvs.Add (new Vector2 (1, 1));

		if (reversed) 
		{
			tris.Add (index + 0);
			tris.Add (index + 1);
			tris.Add (index + 2);

			tris.Add (index + 2);
			tris.Add (index + 3);
			tris.Add (index + 0);
		} 
		else
		{
			tris.Add (index + 1);
			tris.Add (index + 0);
			tris.Add (index + 2);

			tris.Add (index + 3);
			tris.Add (index + 2);
			tris.Add (index + 0);
		}
	}

	public bool IsTransparent (int x, int y, int z)
	{
		byte brick = GetByte (x, y, z);
		switch(brick)
		{
		case 0: return false;
		case 1: return true;
		default: return true;
		}
	}

	public virtual byte GetByte( int x, int y, int z)
	{
		if ((x < 0) || (y < 0) || (z < 0) || (y >= height) || (x >= width) || (z >= width)) 
			return 0;
		return map[x,y,z];
	}

	public static Chunk FindChunk(Vector3 pos)
	{
		for (int i=0; i <chunks.Count; i++)
		{
			Vector3 cpos = chunks[i].transform.position;

			if((pos.x < cpos.x) || (pos.z < cpos.z) || (pos.x > cpos.x + width) || (pos.z > cpos.z + width))
				continue;
			return chunks[i];
		}
		return null;
	}
}