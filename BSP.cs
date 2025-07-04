using System.Numerics;

namespace BSP;

enum VertexClassification
{
    FRONT = 1,
    BACK = -1,
    ON = 0,
}

class BSPTree
{
    static private float SignedDistance(Vector3 p, Vector3 n, float d)
    {
        return Vector3.Dot(n, p) + d;
    }

    static private int ClassifyVertex(Vector3 p, Vector3 n, float d, float eps = 1e-6f)
    {
        float dist = SignedDistance(p, n, d);

        if (dist > eps)
            return (int)VertexClassification.FRONT;
        if (dist < -eps)
            return (int)VertexClassification.BACK;
        return (int)VertexClassification.ON;
    }

    static private List<Vector3> ClipPolygon(List<Vector3> poly, Vector3 n, float d, bool keepFront)
    {
        var outPoly = new List<Vector3>();
        int count = poly.Count;

        for (int i = 0; i < count; i++)
        {
            Vector3 A = poly[i];
            Vector3 B = poly[(i + 1) % count];

            float da = SignedDistance(A, n, d);
            float db = SignedDistance(B, n, d);

            bool aIn = (keepFront ? (da >= 0) : (da <= 0));
            bool bIn = (keepFront ? (db >= 0) : (db <= 0));

            if (aIn)
                outPoly.Add(A);

            // AB crosses the plane
            if (aIn ^ bIn)  // fucking xor
            {
                float t = da / (da - db);
                Vector3 I = A + t * (B - A);

                outPoly.Add(I);
            }
        }

        return outPoly;
    }

    private static List<Triangle> TriangulatePoly(List<Vector3> poly, Vector3 originalNormal)
    {
        var tris = new List<Triangle>();

        for (int i = 1; i + 1 < poly.Count; i++)
        {
            tris.Add(new Triangle
            {
                v1 = poly[0],
                v2 = poly[i],
                v3 = poly[i + 1],
                normal = originalNormal
            });
        }

        return tris;
    }
    
    private static void ClipTriangle(
        Triangle t,
        Vector3 planeN,
        float planeD,
        ref List<Triangle> outFront,
        ref List<Triangle> outBack,
        ref List<Triangle> outCoplanar)
    {
        int c1 = ClassifyVertex(t.v1, planeN, planeD);
        int c2 = ClassifyVertex(t.v2, planeN, planeD);
        int c3 = ClassifyVertex(t.v3, planeN, planeD);

        // all on plane
        if (c1 == 0 && c2 == 0 && c3 == 0)
        {
            outCoplanar.Add(t);
            return;
        }

        // front or on
        if (c1 >= 0 && c2 >= 0 && c3 >= 0)
        {
            outFront.Add(t);
            return;
        }

        // back or on
        if (c1 <= 0 && c2 <= 0 && c3 <= 0)
        {
            outBack.Add(t);
            return;
        }

        // otherwise split
        var triPoly = new List<Vector3> { t.v1, t.v2, t.v3 };
        var frontPoly = ClipPolygon(triPoly, planeN, planeD, keepFront: true);
        var backPoly = ClipPolygon(triPoly, planeN, planeD, keepFront: false);

        // and triangulate
        foreach (var ft in TriangulatePoly(frontPoly, t.normal))
            outFront.Add(ft);
        foreach (var bt in TriangulatePoly(backPoly, t.normal))
            outBack.Add(bt);
    }
    
    public static BSPNode BuildBSP(List<Triangle> triangles, ref int depth, ref Random rnd)
    {
        var node = new BSPNode();
        
        if (triangles.Count == 0) return node;
        if (triangles.Count == 1)
        {
            node.coplanar.Add(triangles[0]);
            return node;
        }

        int pivotIndex = rnd.Next(triangles.Count);
        var pivotTri = triangles[pivotIndex];

        node.planeN = pivotTri.normal;
        node.planeD = -Vector3.Dot(pivotTri.normal, pivotTri.v1);

        node.coplanar.Add(pivotTri);

        // distribute all other triangles
        for (int i = 0; i < triangles.Count; i++)
        {
            if (i == pivotIndex) continue;
            ClipTriangle(
                triangles[i],
                node.planeN, node.planeD,
                ref node.front, ref node.back, ref node.coplanar
            );
        }

        depth++;

        node.frontNode = BuildBSP(node.front, ref depth, ref rnd);
        node.backNode  = BuildBSP(node.back, ref depth, ref rnd);
        return node;
    }
}

class BSPNode
{
    public bool isLeaf { get { return frontNode == null && backNode == null; } }

    public Vector3 planeN;
    public float planeD;

    public BSPNode? frontNode = null;
    public BSPNode? backNode = null;

    public List<Triangle> front = new();
    public List<Triangle> back = new();
    public List<Triangle> coplanar = new();

}