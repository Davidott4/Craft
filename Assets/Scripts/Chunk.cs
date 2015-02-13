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

		CalculateMapFromScratch ();
		StartCoroutine(CreateVisualMesh());
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public static byte GetTheoreticalByte(Vector3 pos)
	{
		Random.seed = World.currentWorld.seed;
		Vector3 grain0Offset = new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000);
		Vector3 grain1Offset = new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000);
		Vector3 grain2Offset = new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000);

		return GetTheoreticalByte (pos, grain0Offset, grain1Offset, grain2Offset);
	}

	public static byte GetTheoreticalByte(Vector3 pos, Vector3 grain0Offset, Vector3 grain1Offset, Vector3 grain2Offset)
	{

		byte brick = 1;

		float heightBase = 10;
		float maxHeight = height - 10;
		float heightSwing = height - heightBase;

		float mountainValue = CalculateNoiseValue(pos, grain1Offset,0.009f);
		mountainValue *= heightSwing;
		mountainValue +=heightBase;
		
		mountainValue += (CalculateNoiseValue(pos, grain0Offset,0.05f) *10)-5;
		mountainValue += CalculateNoiseValue(pos, grain2Offset,0.03f);
		if (mountainValue > y)
			return 1;
		
		//cant fall through
		if (y <=1)
			return 1;
		return 0;
	}


	public virtual void CalculateMapFromScratch()
	{
		Random.seed = World.currentWorld.seed;
		Vector3 grain0Offset = new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000);
		Vector3 grain1Offset = new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000);
		Vector3 grain2Offset = new Vector3(Random.value * 10000, Random.value * 10000, Random.value * 10000);
		



		map = new byte[width, width, height];
		
		for (int x=0; x < width; x++)
		{
			for (int y=0; y < height; y++)
			{
				for (int z=0; z < width; z++)
				{
					map[x,y,z] = GetTheoreticalByte(new Vector3(x,y,z) + transform.position);
				}
			}
		}
	}

	public virtual float CalculateNoiseValue(Vector3 pos, Vector3 offset, float scale)
	{
		float noiseX = Mathf.Abs ((float)(pos.x + offset.x) * scale);
		float noiseY = Mathf.Abs ((float)(pos.y + offset.y) * scale);
		float noiseZ = Mathf.Abs ((float)(pos.z + offset.z) * scale);

		return Mathf.Max(0,Noise.Generate (noiseX, noiseY, noiseZ));

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

		//Vector2 uvWidth = new Vector2 (0.25f, 0.25f);
		//Vector2 uvCorner = new Vector2 (0, 0.75f);

		uvs.Add (new Vector2(0,0));
		uvs.Add (new Vector2(0,1));
		uvs.Add (new Vector2(1,0));
		uvs.Add (new Vector2(1,1));


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
		if (y < 0)
			return true;
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
		if ((y < 0) || (y >= height))
			return 0;
		if ((x < 0) || (y < 0) || (z < 0) || (y >= height) || (x >= width) || (z >= width)) 
		{
			Vector3 worldPos = new Vector3(x,y,z) + transform.position;
			Chunk chunk = Chunk.FindChunk(worldPos);
			if (chunk == this)
				return 0;
			if (chunk == null) 
				return GetTheoreticalByte(worldPos);

			return chunk.GetByte(worldPos);

		}
		return map[x,y,z];
	}

	public virtual byte GetByte(Vector3 worldPos)
	{
		worldPos -= transform.position;
		int posX = Mathf.FloorToInt (worldPos.x);
		int posY = Mathf.FloorToInt (worldPos.y);
		int posZ = Mathf.FloorToInt (worldPos.z);
		return GetByte (posX, posY, posZ);
	}



	public static Chunk FindChunk(Vector3 pos)
	{
		for (int i=0; i <chunks.Count; i++)
		{
			Vector3 cpos = chunks[i].transform.position;

			if((pos.x < cpos.x) || (pos.z < cpos.z) || (pos.x >= cpos.x + width) || (pos.z >= cpos.z + width))
				continue;
			return chunks[i];
		}
		return null;
	}
}