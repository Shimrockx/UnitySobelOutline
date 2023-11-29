using Pathfinding.ClipperLib;
using Pathfinding.Poly2Tri;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshBool2d : MonoBehaviour 
{
    #region Serialize Variables
    private enum ShapeType
    {
        Multipoints,
        Circle
    }
    [SerializeField] private ShapeType shapeType = ShapeType.Multipoints;
    [SerializeField] private List<Vector2> vectors = new List<Vector2>();
    [SerializeField] private float circleRadius = 1;
    [SerializeField] private int circleVerticesCount = 30;
    #endregion

    private const float precision = 100000;
	private Mesh mesh;
	public List<List<IntPoint>> polys = new List<List<IntPoint>>();

    public readonly PolyTree polyTree = new PolyTree();

	private void Start()
    {
        List<IntPoint> list = new List<IntPoint>();
        if (shapeType == ShapeType.Multipoints)
		{
			foreach (Vector2 vector2 in vectors)
			{
				list.Add(Convert(vector2.x, vector2.y));
			}
		}
		else if (shapeType == ShapeType.Circle)
		{
            float delta = Mathf.PI * 2 / 128;
            for (int i = 0; i < 128; i++)
            {
                float x = Mathf.Cos(delta * i) * 10;
                float y = Mathf.Sin(delta * i) * 10;
                list.Add(Convert(x, y));
            }
        }

        polys = new List<List<IntPoint>>()
		{
            list
        };

        mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

        InitMesh();
	}

	private void Update()
    {

    }

	private void InitMesh()
    {
		Clipper clipper = new Clipper(Clipper.ioStrictlySimple);
		clipper.AddPolygons(polys, PolyType.ptSubject);
		clipper.Execute(ClipType.ctDifference, polyTree);
		Clipper.SimplifyPolygons(polys);
		List<DelaunayTriangle> triangles = GenerateTriangles();
		GenerateMesh(triangles);
    }

	public List<DelaunayTriangle> GenerateTriangles()
    {
		List<DelaunayTriangle> triangles = new List<DelaunayTriangle>();

		Dictionary<PolyNode, Polygon> dict = new Dictionary<PolyNode, Polygon>();
		PolyNode curt = polyTree.GetFirst();
		while(curt != null)
        {
			var polygon = Convert(curt.Contour);
			dict.Add(curt, polygon);

			if (curt.IsHole && curt.Parent != null)
				dict[curt.Parent].AddHole(polygon);

			curt = curt.GetNext();
        }

		foreach(var pair in dict)
        {
			var node = pair.Key;
			var poly = pair.Value;

			if (node.IsHole == false)
            {
				P2T.Triangulate(poly);
				triangles.AddRange(poly.Triangles);
			}
        }

		return triangles;
	}

    public virtual void GenerateMesh(List<DelaunayTriangle> triangles)
    {
        mesh.Clear();

        Vector3[] vertices = new Vector3[triangles.Count * 3];
        Vector2[] uv = new Vector2[triangles.Count * 3]; // Create an array for UV coordinates

        for (int i = 0, index = 0; i < triangles.Count; i++)
        {
            vertices[index] = Convert(triangles[i].Points._0);
            uv[index] = new Vector2(vertices[index].x, vertices[index].z); // Calculate UV based on X and Z coordinates
            index++;

            vertices[index] = Convert(triangles[i].Points._2);
            uv[index] = new Vector2(vertices[index].x, vertices[index].z);
            index++;

            vertices[index] = Convert(triangles[i].Points._1);
            uv[index] = new Vector2(vertices[index].x, vertices[index].z);
            index++;
        }

        mesh.vertices = vertices;
        mesh.uv = uv; // Assign UV coordinates to the mesh

        int[] triIndices = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            triIndices[i] = i;
        }
        mesh.triangles = triIndices;

        mesh.RecalculateNormals();
    }

    public static IntPoint Convert(float x, float y)
    {
		return new IntPoint(x * precision, y * precision);
    }

	public static Polygon Convert(List<IntPoint> list)
	{
		List<PolygonPoint> result = new List<PolygonPoint>();

		Clipper.SimplifyPolygon(list);
		for (int i = 0; i < list.Count; i++)
			result.Add(new PolygonPoint(list[i].X, list[i].Y));

		return new Polygon(result);
	}

	public static Vector2 Convert(IntPoint p)
	{
		return new Vector2(p.X / precision, p.Y / precision);
	}

	public static Vector2 Convert(TriangulationPoint p)
    {
		return new Vector2(p.Xf / precision, p.Yf / precision);
    }
}
