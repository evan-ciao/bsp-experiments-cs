using System.Numerics;
using Raylib_cs;

using BSP;
using rlImGui_cs;
using ImGuiNET;
using HeaderExporter;

public struct Triangle
{
    public Vector3 v1, v2, v3;
    public Vector3 normal;
}

class Program
{
    public static void Main()
    {
        Raylib.InitWindow(1280, 720, "BSP baking");

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

        // load first mesh model
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

        // bsp
        int nodes = 0;
        int leaves = 0;

        BSPNode root = BSPTree.BuildBSP(triangles, ref nodes, ref leaves);
        BSPNode tree = root;

        HPPExporter.TestExport(root);

        // point test traversal
        Vector3 point = Vector3.Zero;
        BSPLeafState pointState = BSPLeafState.UNASSIGNED;

        while (!Raylib.WindowShouldClose())
        {
            float delta = Raylib.GetFrameTime();

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
                Raylib.UpdateCamera(ref camera, CameraMode.Free);

            // traverse point
            pointState = BSPTraverse.TraversePoint(root, point);
            if (Raylib.IsKeyDown(KeyboardKey.Up))
                point.Z -= 2 * delta;
            if (Raylib.IsKeyDown(KeyboardKey.Down))
                point.Z += 2 * delta;
            if (Raylib.IsKeyDown(KeyboardKey.Right))
                point.X += 2 * delta;
            if (Raylib.IsKeyDown(KeyboardKey.Left))
                point.X -= 2 * delta;
            if (Raylib.IsKeyDown(KeyboardKey.PageUp))
                point.Y += 2 * delta;
            if (Raylib.IsKeyDown(KeyboardKey.PageDown))
                point.Y -= 2 * delta;

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            Raylib.BeginMode3D(camera);

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

            Raylib.DrawSphere(point, 0.05f, pointState == BSPLeafState.SOLID ? Color.Red : Color.Green);

            Raylib.EndMode3D();

            rlImGui.Begin();
            ImGui.Begin("navigator 3000");

            ImGui.Spacing();
            ImGui.Text($"triangles {triangles.Count}");
            ImGui.Text($"nodes {nodes} leaves {leaves}");

            if (tree.frontNode != null)
            {
                if (ImGui.Button("go to front"))
                {
                    tree = tree.frontNode;
                }
            }
            if (tree.backNode != null)
            {
                if (ImGui.Button("go to back"))
                {
                    tree = tree.backNode;
                }
            }
            if (tree.isLeaf)
            {
                ImGui.Text($"reached a leaf");
                ImGui.Text($"state {tree.state}");
                ImGui.Text($"leaf id {tree.nodeID}");
            }
            if (tree != root)
            {
                ImGui.Spacing();
                if (ImGui.Button("go back to root"))
                {
                    tree = root;
                }
            }

            ImGui.Spacing();
            ImGui.SliderFloat3("point", ref point, -10, 10);
            ImGui.Text($"point is in {pointState}");

            ImGui.End();

            rlImGui.End();

            Raylib.EndDrawing();
        }

        Raylib.UnloadModel(model);

        Raylib.CloseWindow();
    }
}