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
        int depth = 0;
        Random rnd = new();

        BSPNode root = BSPTree.BuildBSP(triangles, ref depth, ref rnd);
        BSPNode tree = root;

        // sphere test
        bool sphereTest = false;
        bool sphereCollision = false;
        Vector3 spherePosition = new Vector3(0, 3, 0);
        float sphereRadius = 1;

        Vector3 collisionN = Vector3.Zero;
        float penetration = 0;

        BSPNode debugNode = tree;
        
        while (!Raylib.WindowShouldClose())
        {
            float delta = Raylib.GetFrameTime();

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
                Raylib.UpdateCamera(ref camera, CameraMode.Free);

            if (sphereTest)
            {
                spherePosition.Y -= 9.8f * delta;

                Vector2 movement = Vector2.Zero;
                if (Raylib.IsKeyDown(KeyboardKey.Up))
                    movement.Y = 1;
                if (Raylib.IsKeyDown(KeyboardKey.Down))
                    movement.Y = -1;
                if (Raylib.IsKeyDown(KeyboardKey.Right))
                    movement.X = 1;
                if (Raylib.IsKeyDown(KeyboardKey.Left))
                    movement.X = -1;

                spherePosition.Z -= movement.Y * 3 * delta;
                spherePosition.X += movement.X * 3 * delta;

                sphereCollision = BSPTraverse.SphereTest(root, ref debugNode, spherePosition, sphereRadius, ref collisionN, ref penetration);

                // simple resolve
                spherePosition += collisionN * penetration;

                tree = debugNode;
            }
            else
            {
                tree = root;
            }

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

            Raylib.DrawSphereWires(spherePosition, sphereRadius, 6, 6, sphereCollision ? Color.Red : Color.Yellow);

            Raylib.EndMode3D();

            rlImGui.Begin();
            ImGui.Begin("navigator 3000");

            if (ImGui.Button("new bsp"))
            {
                depth = 0;
                root = BSPTree.BuildBSP(triangles, ref depth, ref rnd);
                tree = root;
            }

            ImGui.Spacing();
            ImGui.Text($"depth {depth}");

            if (tree.frontNode != null && !tree.frontNode.isLeaf)
            {
                if (ImGui.Button("go to front"))
                {
                    tree = tree.frontNode;
                }
            }
            if (tree.backNode != null && !tree.backNode.isLeaf)
            {
                if (ImGui.Button("go to back"))
                {
                    tree = tree.backNode;
                }
            }
            if (tree != root)
            {
                if (ImGui.Button("go back to root"))
                {
                    tree = root;
                }
            }

            ImGui.Spacing();
            ImGui.Checkbox("sphere test", ref sphereTest);

            if (sphereTest)
            {
                ImGui.SliderFloat("radius", ref sphereRadius, 0.1f, 2f);
                ImGui.Text("use arrow keys to move around");
            }

            ImGui.End();

            rlImGui.End();

            Raylib.EndDrawing();
        }

        Raylib.UnloadModel(model);

        Raylib.CloseWindow();
    }
}