using System.Numerics;

namespace BSP;

class BSPTraverse
{
    private static Vector3 ClosestPointOnTriangle(Triangle t, Vector3 p)
    {
        Vector3 ab = t.v2 - t.v1;
        Vector3 ac = t.v3 - t.v1;
        Vector3 ap = p - t.v1;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0)
            return t.v1;

        Vector3 bp = p - t.v2;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3)
            return t.v2;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            float v = d1 / (d1 - d3);
            return t.v1 + v * ab;
        }

        Vector3 cp = p - t.v3;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6)
            return t.v3;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            float v = d2 / (d2 - d6);
            return t.v1 + v * ac;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
        {
            float v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return t.v2 + v * (t.v3 - t.v2);
        }

        // outside face
        float d = 1 / (va + vb + vc);
        float w = vb * d;
        float k = vc * d;
        return t.v1 + ab * w + ac * k;
    }

    private static bool SphereIntersectsTriangles(Vector3 c, float r, List<Triangle> triangles)
    {
        float r2 = MathF.Pow(r, 2);

        foreach (var t in triangles)
        {
            Vector3 p = ClosestPointOnTriangle(t, c);
            Vector3 d = c - p;

            if (d.LengthSquared() <= r2)
                return true;
        }

        return false;
    }

    public static bool SphereTest(BSPNode root, ref BSPNode debugNode, Vector3 position, float radius, ref Vector3 collisionN, ref float penetration)
    {
        debugNode = root;

        float mind = radius;
        BSPNode closestNode = null;

        while (debugNode != null && !debugNode.isLeaf)
        {
            float d = BSPTree.PlaneSignedDistance(position, debugNode.planeN, debugNode.planeD);

            // found a colliding node
            if (MathF.Abs(d) < mind)
            {
                mind = d;
                closestNode = debugNode;
            }

            // otherwise
            if (d >= 0)
            {
                // move to front
                debugNode = debugNode.frontNode;
            }
            else
            {
                // move to back
                debugNode = debugNode.backNode;
            }

        }

        // test collision
        if (closestNode != null && SphereIntersectsTriangles(position, radius, closestNode.coplanar))
        {
            collisionN = closestNode.planeN;
            penetration = MathF.Abs(radius - mind);
            return true;
        }

        // no collision
        return false;
    }
    /*
    public static BSPNodeState TraversePoint(BSPNode root, Vector3 point)
    {
        BSPNode node = root;

        while (!node.isLeaf)
        {
            float d = BSPTree.PlaneSignedDistance(point, node.planeN, node.planeD);

            if (d >= 0)
                node = node.frontNode;
            else
                node = node.backNode;
        }

        return node.state;
    }
    */
}