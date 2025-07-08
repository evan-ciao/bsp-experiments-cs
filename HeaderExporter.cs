namespace HeaderExporter;

using System.Text;
using BSP;

public static class HPPExporter
{
    private static void CreateBoilerplateStart(ref StringBuilder boilerplate)
    {
        boilerplate.Append("""
        #ifndef CMAP_TEST_HPP
        #define CMAP_TEST_HPP

        // auto generated collision map

        #include "BSP.hpp"

        class cmap_Test
        {
        BSPNode* root = nullptr;
        
        cmap_Test()
        {

        """);
    }

    private static void CreateBoilerplateEnd(ref StringBuilder boilerplate)
    {
        boilerplate.Append("""
        
        }
        };

        #endif
        """);
    }

    private static void DeclarationTraversal(BSPNode node, ref StringBuilder declaration)
    {
        if (node == null)
            return;

        // visit the current node
        declaration.Append($"BSPNode* {node.nodeID};\n");
        declaration.Append($"{node.nodeID} = new BSPNode();");
        if (node.isLeaf)
        {
            declaration.Append(" // leaf");
        }
        declaration.Append("\n");

        // visit front node
        DeclarationTraversal(node.frontNode, ref declaration);
        // visit back node
        DeclarationTraversal(node.backNode, ref declaration);
    }

    private static void InitializationTraversal(BSPNode node, ref StringBuilder initialization)
    {
        if (node == null)
            return;

        // visit the current node
        if (node.isLeaf)
        {
            // just change the state
            initialization.Append($"{node.nodeID}->state = {node.state};\n");
        }
        else
        {
            initialization.Append($"{node.nodeID}->planeN = vector3({FixedPoint.FloatToF32(node.planeN.X)}, {FixedPoint.FloatToF32(node.planeN.Y)}, {FixedPoint.FloatToF32(node.planeN.Z)});\n");
            initialization.Append($"{node.nodeID}->planeD = {FixedPoint.FloatToF32(node.planeD)};\n");
        }

        // visit front node
        InitializationTraversal(node.frontNode, ref initialization);
        // visit back node
        InitializationTraversal(node.backNode, ref initialization);
    }

    private static void LinkingTraversal(BSPNode node, ref StringBuilder linking)
    {
        if (node == null)
            return;

        // visit the current node
        if (!node.isLeaf)
        {
            linking.Append($"{node.nodeID}->frontNode = {node.frontNode.nodeID};\n");
            linking.Append($"{node.nodeID}->backNode = {node.backNode.nodeID};\n");
        }

        // visit front node
        LinkingTraversal(node.frontNode, ref linking);
        // visit back node
        LinkingTraversal(node.backNode, ref linking);
    }

    public static void TestExport(BSPNode root)
    {
        string testPath = @"/home/evan/Documents/bsp-experiments-cs/exported/test.txt";

        StringBuilder boilerplateStart = new();
        StringBuilder declarationCode = new();
        StringBuilder initializationCode = new();
        StringBuilder linkingCode = new();

        CreateBoilerplateStart(ref boilerplateStart);

        DeclarationTraversal(root, ref declarationCode);
        declarationCode.Append("\n");

        InitializationTraversal(root, ref initializationCode);
        initializationCode.Append("\n");

        LinkingTraversal(root, ref linkingCode);
        linkingCode.Append("\n");

        StringBuilder header = boilerplateStart.Append(declarationCode).Append(initializationCode).Append(linkingCode);

        // assign the root
        header.Append($"root = {root.nodeID};");

        CreateBoilerplateEnd(ref header);

        System.IO.File.WriteAllText(testPath, header.ToString());
    }
}