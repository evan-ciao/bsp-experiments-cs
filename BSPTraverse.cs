using System.Numerics;

namespace BSP;

class BSPTraverse
{
    public static BSPLeafState TraversePoint(BSPNode root, Vector3 point, out Vector3 correction)
    {
        BSPNode node = (BSPNode)root.Clone();
        Vector3 _bestNormal = Vector3.Zero;
        float _bestDepth = -100;

        while (!node.isLeaf)
        {
            float d = BSPTree.PlaneSignedDistance(point, node.planeN, node.planeD);

            if (d >= 0)
                node = node.frontNode;
            else
            {
                if (d > _bestDepth)
                {
                    _bestNormal = node.planeN;
                    _bestDepth = d;
                }

                node = node.backNode;
            }
        }

        correction = _bestNormal * -(_bestDepth);

        return node.state;
    }

    public static void CollidePoint(BSPNode root, ref Vector3 point)
    {
        Vector3 correction;
        const int MAX_ITERATIONS = 8;
        int i = 0;
        while (TraversePoint(root, point, out correction) == BSPLeafState.SOLID)
        {
            point += correction;
            i++;

            if (i > MAX_ITERATIONS)
                break;
        }
    }

    public static bool RecursiveLineTrace(BSPNode node, Vector3 p1, Vector3 p2, out Vector3 intersection)
    {
        // handle leaves
        if (node.isLeaf)
        {
            if (node.state == BSPLeafState.SOLID)
            {
                intersection = p1;
                return true;
            }
            intersection = Vector3.NaN;
            return false;
        }
        
        // distances
        float t1 = BSPTree.PlaneSignedDistance(p1, node.planeN, node.planeD);
        float t2 = BSPTree.PlaneSignedDistance(p2, node.planeN, node.planeD);

        // the line lies entirely within one of the two subspaces
        if (t1 >= 0 && t2 >= 0)
            return RecursiveLineTrace(node.frontNode, p1, p2, out intersection);
        if (t1 < 0 && t2 < 0)
            return RecursiveLineTrace(node.backNode, p1, p2, out intersection);

        // straddle the plane
        float denom = (t1 - t2);
        if (Math.Abs(denom) < 1e-6f)
        {
            // parallel. bail on front side
            return RecursiveLineTrace(node.frontNode, p1, p2, out intersection);
        }

        // line crosses different nodes
        float frac = t1 / denom;    // intersection with the split plane
        frac = Math.Clamp(frac, 0, 1);

        Vector3 mid = p1 + frac * (p2 - p1);
        bool keepBack = t1 < 0;

        // split the problem
        if (RecursiveLineTrace(keepBack ? node.backNode : node.frontNode, p1, mid, out intersection))
            return true;
        return RecursiveLineTrace(keepBack ? node.frontNode : node.backNode, mid, p2, out intersection);
    }
}