using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using System;
using System.Text.Json;
using System.Xml;

namespace rt004;


//TODO: Add attributes to be inherited to the tree. Inner nodes contain transformations, and attributes. They are
//read in string form, and those are stored in strings. Getters take care of calculating the actual matrices,
//color values etc. The leaf nodes contain objects, which are created in the node constructor, based on the 
//read integer.

/// <summary>
/// Directed tree data strcture to store scene elements
/// </summary>
public class SceneGraph
{
	public Node Root { get; set; }
    private ILogger logger = Logging.CreateLogger<SceneGraph>();

    public SceneGraph(string filePath) 
    {
        string jsonStr = File.ReadAllText(filePath);
        var obj=JsonDocument.Parse(jsonStr);

        try
        {
            Root = JsonSerializer.Deserialize<Node>(obj);
            SetUpParams(Root);
        }
        catch(Exception ex)
        {
            if (ex is JsonException)
                logger.LogWarning("SceneGraph.json is not set up properly, scene is empty!");
            else if (ex is NullReferenceException)
                logger.LogWarning("Root initialized as null, scene is empty!");
        } 
    }

	public class Node
	{
        public Node() 
        {
            //Create scene object
            if (Obj != null)
                priv_obj = Activator.CreateInstance(Type.GetType(Globals.ASSEMBLY_NAME + Obj)) as SceneObject;
            else
                priv_obj = null;

            //Create transformation matrix based on the input
            if(Transformation != null)
            {
                string[] commands = Transformation.Split(" ");
                Matrix4d FinalTransform = Matrix4d.Identity;
                foreach (string command in commands)
                {
                    int left_bracket = command.IndexOf('(');
                    string transform = command.Substring(0, left_bracket);
                    string values = command.Substring(left_bracket);
                    values = values.Trim('(', ')');
                    float[] values_arr = values.Split(",").Select(float.Parse).ToArray();

                    switch (transform)
                    {
                        case "translate":
                            Vector3d values_vec = ColorTools.ArrToV3d(values_arr);
                            FinalTransform *= Matrix4d.CreateTranslation(values_vec);
                            break;
                        case "rotateX":
                            FinalTransform *= Matrix4d.CreateRotationX(values_arr[0]);
                            break;
                        case "rotateY":
                            FinalTransform *= Matrix4d.CreateRotationY(values_arr[0]);
                            break;
                        case "rotateZ":
                            FinalTransform *= Matrix4d.CreateRotationZ(values_arr[0]);
                            break;
                    }
                }
                priv_transform = FinalTransform;
            }
            else
                priv_transform = null;
        }

        public Node? Parent { get; set; }
        public List<Node>? Children { get; set; }
        private string? Obj { get; set; }
        private string? Transformation { get; set; }
        private SceneObject? priv_obj;
        private Matrix4d? priv_transform;
        public SceneObject? Object { get {return priv_obj;} }
        public Matrix4d? Transform { get {return priv_transform;} }
	}

    /// <summary>
    /// Traverses the tree, and sets up the parent nodes.
    /// </summary>
    private void SetUpParams(Node node)
    {
        //Set up parents
        if (node.Children != null)
        {
            foreach (Node child in node.Children)
            {
                child.Parent = node;
                SetUpParams(child);
            }
        }
        else
            return;
        
    }
}
