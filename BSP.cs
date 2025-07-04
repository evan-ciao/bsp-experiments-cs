using System.Numerics;

namespace BSP;

enum BSPState
{
    UNASSIGNED,
    SOLID,
    AIR
}

class BSPTree
{
    static float DistanceFromPlane(Vector3 p, Vector3 normal, float d)
    {
        return Vector3.Dot(normal, p) + d;
    }

    enum IntersectionType
    {
        FRONT,
        BACK,
        COPLANAR,
        CROSSING
    }
    static IntersectionType SegmentPlaneIntersection(Vector3 p1, Vector3 p2, ref List<Vector3> i, Vector3 normal, float d)
    {
        float d1 = DistanceFromPlane(p1, normal, d);
        float d2 = DistanceFromPlane(p2, normal, d);

        const double eps = 1e-9;
        bool p1OnPlane = MathF.Abs(d1) < eps;
        bool p2OnPlane = MathF.Abs(d2) < eps;

        if (p1OnPlane && p2OnPlane)
            return IntersectionType.COPLANAR;
        if (d1 < 0 && d2 < 0 || p1OnPlane && d2 < 0 || p2OnPlane && d1 < 0)
            return IntersectionType.BACK;
        if (d1 > 0 && d2 > 0 || p1OnPlane && d2 > 0 || p2OnPlane && d1 > 0)
            return IntersectionType.FRONT;

        float t = d1 / (d1 - d2);
        i.Add(p1 + t * (p2 - p1));

        return IntersectionType.CROSSING;
    }

    static IntersectionType ClipTriangleWithPlane(Triangle t, ref List<Triangle> splits, Vector3 normal, Vector3 v1)
    {
        float d = Vector3.Dot(-normal, v1);

        Vector3 p1 = t.v1;
        Vector3 p2 = t.v2;
        Vector3 p3 = t.v3;

        List<Vector3> intersections = new();
        IntersectionType a = SegmentPlaneIntersection(p1, p2, ref intersections, normal, d);
        IntersectionType b = SegmentPlaneIntersection(p1, p3, ref intersections, normal, d);
        IntersectionType c = SegmentPlaneIntersection(p2, p3, ref intersections, normal, d);

        if (intersections.Count == 2)
        {
            if (c == IntersectionType.FRONT || c == IntersectionType.BACK)
            {
                Triangle quadTriangle1 = new();
                Triangle quadTriangle2 = new();
                Triangle smallTriangle = new();
                quadTriangle1.v1 = intersections[0];
                quadTriangle1.v2 = p2;
                quadTriangle1.v3 = p3;

                quadTriangle2.v1 = intersections[1];
                quadTriangle2.v2 = intersections[0];
                quadTriangle2.v3 = p3;

                smallTriangle.v1 = p1;
                smallTriangle.v2 = intersections[0];
                smallTriangle.v3 = intersections[1];

                splits.Add(quadTriangle1);
                splits.Add(quadTriangle2);
                splits.Add(smallTriangle);
            }
            if (a == IntersectionType.FRONT || a == IntersectionType.BACK)
            {
                Triangle quadTriangle1 = new();
                Triangle quadTriangle2 = new();
                Triangle smallTriangle = new();
                quadTriangle1.v1 = intersections[0];
                quadTriangle1.v2 = p1;
                quadTriangle1.v3 = p2;

                quadTriangle2.v1 = p2;
                quadTriangle2.v2 = intersections[1];
                quadTriangle2.v3 = intersections[0];

                smallTriangle.v1 = p3;
                smallTriangle.v2 = intersections[0];
                smallTriangle.v3 = intersections[1];

                splits.Add(quadTriangle1);
                splits.Add(quadTriangle2);
                splits.Add(smallTriangle);
            }
            if (b == IntersectionType.FRONT || b == IntersectionType.BACK)
            {
                Triangle quadTriangle1 = new();
                Triangle quadTriangle2 = new();
                Triangle smallTriangle = new();
                quadTriangle1.v1 = p1;
                quadTriangle1.v2 = intersections[0];
                quadTriangle1.v3 = p3;

                quadTriangle2.v1 = intersections[0];
                quadTriangle2.v2 = intersections[1];
                quadTriangle2.v3 = p3;

                smallTriangle.v1 = intersections[0];
                smallTriangle.v2 = p2;
                smallTriangle.v3 = intersections[1];

                splits.Add(quadTriangle1);
                splits.Add(quadTriangle2);
                splits.Add(smallTriangle);
            }
            
            return IntersectionType.CROSSING;
        }

        if (a == IntersectionType.COPLANAR && b == a && b == c)
            return IntersectionType.COPLANAR;

        if (a == IntersectionType.FRONT || b == IntersectionType.FRONT || c == IntersectionType.FRONT)
            return IntersectionType.FRONT;
        else
            return IntersectionType.BACK;
    }

    static IntersectionType CheckTriangleFrontBackWithPlane(Triangle t, Vector3 normal, Vector3 v1)
    {
        float d = Vector3.Dot(-normal, v1);

        Vector3 p1 = t.v1;
        Vector3 p2 = t.v2;
        Vector3 p3 = t.v3;

        List<Vector3> intersections = new();
        IntersectionType a = SegmentPlaneIntersection(p1, p2, ref intersections, normal, d);
        IntersectionType b = SegmentPlaneIntersection(p1, p3, ref intersections, normal, d);
        IntersectionType c = SegmentPlaneIntersection(p2, p3, ref intersections, normal, d);

        if (a == IntersectionType.FRONT || b == IntersectionType.FRONT || c == IntersectionType.FRONT)
            return IntersectionType.FRONT;
        else
            return IntersectionType.BACK;
    }
    
    static public BSPNode CreateBSPTree(List<Triangle> triangles)
    {
        BSPNode node = new();
        if (triangles.Count < 2)
        {
            // leaf found
            return node;
        }

        // get random triangle from the list
        Random rnd = new Random();
        int i = rnd.Next(triangles.Count);

        // test iteration
        node.coplanar.Add(triangles[i]);

        Vector3 plane = triangles[i].normal;

        for (int j = 0; j < triangles.Count; j++)
        {
            if (i == j) // skip the current triangle
                continue;

            List<Triangle> splits = new();

            switch (ClipTriangleWithPlane(
                triangles[j],
                ref splits,
                plane,
                triangles[i].v1
            ))
            {
                case IntersectionType.FRONT:
                    node.front.Add(triangles[j]);
                    break;
                case IntersectionType.BACK:
                    node.back.Add(triangles[j]);
                    break;
                case IntersectionType.COPLANAR:
                    node.coplanar.Add(triangles[j]);
                    break;
                case IntersectionType.CROSSING:
                    for (int s = 0; s < splits.Count; s++)
                    {
                        Triangle split = splits[s];
                        split.normal = triangles[j].normal;
                        switch (CheckTriangleFrontBackWithPlane(split, plane, triangles[i].v1))
                        {
                            case IntersectionType.FRONT:
                                node.front.Add(split);
                                break;
                            case IntersectionType.BACK:
                                node.back.Add(split);
                                break;
                        }
                    }
                    break;
            }
        }

        // run the algorithm again on the front and back (?)
        node.frontNode = CreateBSPTree(node.front);
        node.backNode = CreateBSPTree(node.back);

        return node;
    }
}

class BSPNode
{
    public bool isLeaf { get { return frontNode == null && backNode == null; }}
    public BSPState state = BSPState.UNASSIGNED;

    public BSPNode? frontNode = null;
    public BSPNode? backNode = null;

    public List<Triangle> coplanar = new();
    public List<Triangle> crossing = new();
    public List<Triangle> front = new();
    public List<Triangle> back = new();

}