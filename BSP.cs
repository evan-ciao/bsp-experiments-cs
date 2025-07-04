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
    static public float PlaneSignedDistance(Vector3 p, Vector3 n, float d)
    {
        return Vector3.Dot(n, p) + d;
    }

    static private int ClassifyVertex(Vector3 p, Vector3 n, float d, float eps = 1e-6f)
    {
        float dist = PlaneSignedDistance(p, n, d);

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

            float da = PlaneSignedDistance(A, n, d);
            float db = PlaneSignedDistance(B, n, d);

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
        /*
        if (triangles.Count == 1)
        {
            node.coplanar.Add(triangles[0]);
            node.planeN = triangles[0].normal;
            return node;
        }*/

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
        node.backNode = BuildBSP(node.back, ref depth, ref rnd);
        return node;
    }

    /// <summary>
    /// Gathers all the triangles in a BSPNode and finds the smallest enclosing AABB.
    /// </summary>
    /// <param name="node">Target node.</param>
    /// <returns>The vertices of the AABB.</returns>
    private static List<Vector3> GetAABBFromBSPNode(BSPNode node)
    {
        List<Vector3> aabb = new List<Vector3>(8);
        List<Triangle> triangles = [.. node.front.Concat(node.back).Concat(node.coplanar)];
        List<Vector3> soup = new();

        foreach (var triangle in triangles)
        {
            soup.Add(triangle.v1);
            soup.Add(triangle.v2);
            soup.Add(triangle.v3);
        }

        if (soup.Count < 1)
            return aabb;

        Vector3 min = Vector3.Zero;
        Vector3 max = Vector3.Zero;
        foreach (var vertex in soup)
        {
            if (vertex.X < min.X)
                min.X = vertex.X;
            if (vertex.Y < min.Y)
                min.Y = vertex.Y;
            if (vertex.Z < min.Z)
                min.Z = vertex.Z;

            if (vertex.X > max.X)
                max.X = vertex.X;
            if (vertex.Y > max.Y)
                max.Y = vertex.Y;
            if (vertex.Z > max.Z)
                max.Z = vertex.Z;
        }

        // lower
        aabb.Add(min);
        aabb.Add(new Vector3(max.X, min.Y, min.Z));
        aabb.Add(new Vector3(max.X, min.Y, max.Z));
        aabb.Add(new Vector3(min.X, min.Y, max.Z));
        // upper
        aabb.Add(new Vector3(min.X, max.Y, min.Z));
        aabb.Add(new Vector3(max.X, max.Y, min.Z));
        aabb.Add(max);
        aabb.Add(new Vector3(min.X, max.Y, max.Z));

        return aabb;
    }

    public static void BuildBSPLeaves(ref BSPNode root)
    {
        // cache aabb from root
        List<Vector3> boundingAABB = GetAABBFromBSPNode(root);

        List<HalfSpace> recursiveHalfSpaces = new();
        PreOrderTraversal(ref root, recursiveHalfSpaces, boundingAABB);
    }

    /// <summary>
    /// Recursively clips the bounding AABB with the leaf's half-spaces, then calculates the centroid.
    /// </summary>
    /// <param name="leaf">The target leaf.</param>
    /// <param name="boundingAABB">The tree bounding AABB. This value is cloned before clipping.</param>
    /// <returns>The centroid.</returns>
    private static Vector3 GetBSPLeafCentroid(BSPLeaf leaf, List<Vector3> boundingAABB)
    {
        Vector3 centroid = Vector3.Zero;

        if (leaf.halfSpaces == null)
            return centroid;

        List<Vector3> aabb = [.. boundingAABB]; // copy this crap

        // ok so now we should just be able to slice through the aabb like a cake and pray ir works
        foreach (var halfSpace in leaf.halfSpaces)
        {
            aabb = ClipPolygon(aabb, halfSpace.planeN, halfSpace.planeD, halfSpace.keepFront);
        }

        // sum and average
        foreach (var vertex in aabb)
            centroid += vertex;
        centroid /= aabb.Count;

        return centroid;
    }

    /// <summary>
    /// Recursive function call. Traverses the tree from node until a leaf is found. The next branch is informed with the last iteration data.
    /// </summary>
    /// <param name="node">Recursive call on next binary nodes in the chain.</param>
    /// <param name="branchHalfSpaces">Half-spaces that led up to this node.</param>
    /// <param name="boundingAABB">Bounding AABB volume used by leaves for centroid calculation.</param>

    private static void PreOrderTraversal(ref BSPNode node, List<HalfSpace> branchHalfSpaces, List<Vector3> boundingAABB)
    {
        if (node == null)
            return;

        if (node.isLeaf)
        {
            node.leaf = new();
            node.leaf.halfSpaces = branchHalfSpaces;
            node.leaf.centroid = GetBSPLeafCentroid(node.leaf, boundingAABB);

            if (node.leaf.centroid != Vector3.NaN)
                Program.centroids.Add(node.leaf.centroid);

            Console.WriteLine($"[PreOrderTraversal] Reached a leaf with {branchHalfSpaces.Count} half spaces. Centroid {node.leaf.centroid}");
            return;
        }

        var frontHalfSpace = new HalfSpace { planeN = node.planeN, planeD = node.planeD, keepFront = true };
        var backHalfSpace = new HalfSpace { planeN = node.planeN, planeD = node.planeD, keepFront = false };

        // lists are passed by reference ffs
        if (node.frontNode != null)
            PreOrderTraversal(ref node.frontNode, new List<HalfSpace>(branchHalfSpaces) { frontHalfSpace }, boundingAABB);// continue down this branch keeping the front of the plane
        if (node.backNode != null)
            PreOrderTraversal(ref node.backNode, new List<HalfSpace>(branchHalfSpaces) { backHalfSpace }, boundingAABB);  // keep the back of the plane
    }
}

enum BSPNodeState
{
    UNASSIGNED,
    AIR,
    SOLID
}

struct HalfSpace
{
    public Vector3 planeN;
    public float planeD;
    public bool keepFront;
}

class BSPLeaf
{
    public List<HalfSpace>? halfSpaces;
    // defining a centroid within all of the half spaces?
    public Vector3 centroid = Vector3.Zero;
    public BSPNodeState state = BSPNodeState.UNASSIGNED;
}

class BSPNode
{
    public bool isLeaf { get { return frontNode == null && backNode == null; } }
    public BSPLeaf? leaf = null;

    public Vector3 planeN;
    public float planeD;

    public BSPNode? frontNode = null;
    public BSPNode? backNode = null;

    public List<Triangle> front = new();
    public List<Triangle> back = new();
    public List<Triangle> coplanar = new();

}