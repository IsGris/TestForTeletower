using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelZoneData", menuName = "ScriptableObjects/LevelZoneData")]
public class LevelZoneData : ScriptableObject
{
    [Tooltip("All zone points in level that makes polygon")] 
    public List<Vector3> ZonePoints = new List<Vector3>();

    // Generates zone polygon to triangles and pick random point from random triangle
    public Vector3 GetRandomPointInZone()
    {
        if (ZonePoints == null || ZonePoints.Count < 3)
        {
            return Vector3.zero;
        }

        List<Vector2> points2D = new List<Vector2>();
        foreach (var point in ZonePoints)
        {
            points2D.Add(new Vector2(point.x, point.z));
        }

        List<Triangle> triangles = Triangulate(points2D);
        if (triangles == null || triangles.Count == 0)
        {
            return Vector3.zero;
        }

        float totalArea = 0;
        foreach (var triangle in triangles)
        {
            totalArea += triangle.Area;
        }

        float randomValue = Random.Range(0, totalArea);
        Triangle selectedTriangle = null;

        foreach (var triangle in triangles)
        {
            if (randomValue <= triangle.Area)
            {
                selectedTriangle = triangle;
                break;
            }
            randomValue -= triangle.Area;
        }

        if (selectedTriangle == null && triangles.Count > 0)
        {
            selectedTriangle = triangles[0];
        }


        Vector2 randomPoint2D = GetRandomPointInTriangle(selectedTriangle.A, selectedTriangle.B, selectedTriangle.C);

        return new Vector3(randomPoint2D.x, ZonePoints[0].y, randomPoint2D.y);
    }

    private class Triangle
    {
        public readonly Vector2 A, B, C;
        public readonly float Area;

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;
            Area = Mathf.Abs(A.x * (B.y - C.y) + B.x * (C.y - A.y) + C.x * (A.y - B.y)) * 0.5f;
        }
    }

    private List<Triangle> Triangulate(List<Vector2> points)
    {
        List<Triangle> triangles = new List<Triangle>();
        List<int> indices = new List<int>();
        for (int i = 0; i < points.Count; i++)
        {
            indices.Add(i);
        }

        float signedArea = 0;
        for (int i = 0; i < points.Count; i++)
        {
            signedArea += (points[i].x * points[(i + 1) % points.Count].y) - (points[(i + 1) % points.Count].x * points[i].y);
        }
        bool isClockwise = signedArea < 0;

        int attempts = 0;
        while (indices.Count > 3 && attempts < points.Count * points.Count)
        {
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i + indices.Count - 1) % indices.Count];
                int current = indices[i];
                int next = indices[(i + 1) % indices.Count];

                Vector2 p_prev = points[prev];
                Vector2 p_curr = points[current];
                Vector2 p_next = points[next];

                if (IsEar(p_prev, p_curr, p_next, points, isClockwise))
                {
                    triangles.Add(new Triangle(p_prev, p_curr, p_next));
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound) attempts++;
        }

        if (indices.Count == 3)
        {
            triangles.Add(new Triangle(points[indices[0]], points[indices[1]], points[indices[2]]));
        }

        return triangles;
    }

    private bool IsEar(Vector2 a, Vector2 b, Vector2 c, List<Vector2> polygonPoints, bool isClockwise)
    {
        float crossProduct = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (isClockwise ? crossProduct > 0 : crossProduct < 0)
        {
            return false;
        }

        foreach (var p in polygonPoints)
        {
            if (p == a || p == b || p == c) continue;
            if (IsPointInTriangle(p, a, b, c))
            {
                return false;
            }
        }
        return true;
    }

    private Vector2 GetRandomPointInTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        float r1 = Random.value;
        float r2 = Random.value;

        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }

        Vector2 ab = b - a;
        Vector2 ac = c - a;

        return a + r1 * ab + r2 * ac;
    }

    private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(has_neg && has_pos);
    }

    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
