using System.Numerics;
using Raylib_cs;

using BSP;
using rlImGui_cs;
using ImGuiNET;

struct Triangle
{
    public Vector3 v1, v2, v3;
    public Vector3 normal;
}

class Program
{
    public static void Main()
    {
        Raylib.InitWindow(800, 480, "BSP baking");

        Camera3D camera = new(
            new Vector3(0, 2, 2),
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            50,
            CameraProjection.Perspective
        );

        // relative path won't work. great
        Model model = Raylib.LoadModel("/home/evan/Documents/bsp-experiments-cs/models/test-scene.obj");
        Console.WriteLine(model.MeshCount);
        
        if (model.MeshCount < 1)
            return;

        List<Triangle> triangles = new();
        unsafe
        {
            Mesh mesh = model.Meshes[0];

            for (int i = 0; i < mesh.VertexCount; i += 3)
            {
                Triangle triangle = new();

                triangle.v1 = new Vector3(
                    mesh.Vertices[3 * i],
                    mesh.Vertices[3 * i + 1],
                    mesh.Vertices[3 * i + 2]);
                triangle.v2 = new Vector3(
                    mesh.Vertices[3 * i + 3],
                    mesh.Vertices[3 * i + 4],
                    mesh.Vertices[3 * i + 5]);
                triangle.v3 = new Vector3(
                    mesh.Vertices[3 * i + 6],
                    mesh.Vertices[3 * i + 7],
                    mesh.Vertices[3 * i + 8]);

                triangle.normal = Vector3.Normalize(Vector3.Cross(triangle.v2 - triangle.v1, triangle.v3 - triangle.v1));

                triangles.Add(triangle);
            }
        }

        Console.WriteLine($"triangles count {triangles.Count}");

        // imgui
        rlImGui.Setup(true);

        // test BSP
        BSPNode tree = BSPTree.CreateBSPTree(triangles);

        Triangle triangleVisited = new();
        int triangleVisitedIndex = 0;
        
        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Right))
                Raylib.UpdateCamera(ref camera, CameraMode.Free);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            Raylib.BeginMode3D(camera);

            //Raylib.DrawGrid(10, 1);

            /*
            foreach (var triangle in triangles)
            {
                Raylib.DrawLine3D(triangle.v1, triangle.v2, Color.Black);
                Raylib.DrawLine3D(triangle.v3, triangle.v2, Color.Black);
                Raylib.DrawLine3D(triangle.v3, triangle.v1, Color.Black);

                Vector3 center = (triangle.v1 + triangle.v2 + triangle.v3) / 3;
                Raylib.DrawLine3D(
                    center,
                    center + triangle.normal,
                    Color.Red
                );
            }
            */

            foreach (var triangle in tree.front)
            {
                Raylib.DrawLine3D(triangle.v1, triangle.v2, Color.Blue);
                Raylib.DrawLine3D(triangle.v3, triangle.v2, Color.Blue);
                Raylib.DrawLine3D(triangle.v3, triangle.v1, Color.Blue);
            }
            foreach (var triangle in tree.back)
            {
                Raylib.DrawLine3D(triangle.v1, triangle.v2, Color.Red);
                Raylib.DrawLine3D(triangle.v3, triangle.v2, Color.Red);
                Raylib.DrawLine3D(triangle.v3, triangle.v1, Color.Red);
            }
            foreach (var triangle in tree.coplanar)
            {
                Raylib.DrawLine3D(triangle.v1, triangle.v2, Color.Magenta);
                Raylib.DrawLine3D(triangle.v3, triangle.v2, Color.Magenta);
                Raylib.DrawLine3D(triangle.v3, triangle.v1, Color.Magenta);

                Vector3 center = (triangle.v1 + triangle.v2 + triangle.v3) / 3;
                Raylib.DrawLine3D(
                    center,
                    center + triangle.normal,
                    Color.Red
                );
            }

            int bspTriangles = tree.front.Count + tree.back.Count + tree.coplanar.Count - 1;

            if (triangleVisitedIndex >= 0 && triangleVisitedIndex < tree.front.Count)
                triangleVisited = tree.front[triangleVisitedIndex];

            if (triangleVisitedIndex >= tree.front.Count && triangleVisitedIndex < tree.front.Count + tree.back.Count)
                triangleVisited = tree.back[triangleVisitedIndex - tree.front.Count];
            
            if (triangleVisitedIndex >= tree.front.Count + tree.back.Count && triangleVisitedIndex < bspTriangles)
                triangleVisited = tree.coplanar[triangleVisitedIndex - tree.front.Count - tree.back.Count];
            

            Raylib.DrawTriangle3D(triangleVisited.v1, triangleVisited.v2, triangleVisited.v3, Color.Red);
            Raylib.DrawTriangle3D(triangleVisited.v1, triangleVisited.v3, triangleVisited.v2, Color.Red);

            Raylib.EndMode3D();

            rlImGui.Begin();
            ImGui.Begin("navigator 3000");
            ImGui.Text($"bsp triangles {bspTriangles + 1}");
            ImGui.SliderInt("triangle visited", ref triangleVisitedIndex, 0, bspTriangles);

            if (tree.frontNode != null && !tree.frontNode.isLeaf)
            {
                if (ImGui.Button("go front nodes"))
                {
                    tree = tree.frontNode;
                }
            }
            if (tree.backNode != null && !tree.backNode.isLeaf)
            {
                if (ImGui.Button("go back node"))
                {
                    tree = tree.backNode;
                }
            }
                
            ImGui.End();

            rlImGui.End();

            Raylib.EndDrawing();
        }

        Raylib.UnloadModel(model);

        Raylib.CloseWindow();
    }
}