using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;
using System.Text.Json;

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

    public class Attribs
    {
        public double[] Clr { get; set; }
        public Vector3d Color { get { return ColorTools.ArrToV3d(Clr); } }
        public double Scale { get; set; }
        public string Mat {get;set;}
        public string Txt { get; set; }
        public Material? Material { 
            get
            {
                Material? priv_mat = null;
                if (Mat != null)
                {
                    priv_mat = Activator.CreateInstance(Type.GetType(Globals.ASSEMBLY_NAME + Mat)) as Material;
                }
                return priv_mat;
 
            }
        }

        public Texture? Texture {
            get
            {
                Texture? priv_mat = null;
                if (Txt != null)
                {
                    priv_mat = Activator.CreateInstance(Type.GetType(Globals.ASSEMBLY_NAME + Txt)) as Texture;
                }
                return priv_mat;
            }
        }
    }

	public class Node
	{
        public Attribs? Attributes { get; set; }
        public Node? Parent { get; set; }
        public List<Node>? Children { get; set; }
        public string? Obj { get; set; }
        public string? Transformation { get; set; }
        public Shape? Object { 
            get {
                Shape? priv_obj=null;
                if (Obj != null)
                {
                    priv_obj = Activator.CreateInstance(Type.GetType(Globals.ASSEMBLY_NAME + Obj)) as Shape;
                }
                return priv_obj;
            } 
        }
        public Matrix4d Transform { 
            get 
            {
                Matrix4d FinalTransform = Matrix4d.Identity;
                if (Transformation != null)
                {
                    string[] commands = Transformation.Split(" ");
                    foreach (string command in commands)
                    {
                        int left_bracket = command.IndexOf('(');
                        string transform = command.Substring(0, left_bracket);
                        string values = command.Substring(left_bracket);
                        values = values.Trim('(', ')');
                        double[] values_arr = values.Split(",").Select(double.Parse).ToArray();
                        switch (transform)
                        {
                            case "translate":
                                Vector3d values_vec = ColorTools.ArrToV3d(values_arr);
                                FinalTransform =Matrix4d.Transpose(Matrix4d.CreateTranslation(values_vec))*FinalTransform;    
                                break;
                            case "rotateX":
                                Matrix4d rotX = Matrix4d.CreateRotationX(MathTools.Deg2Rad(values_arr[0]));
                                FinalTransform = rotX*FinalTransform;
                                break;
                            case "rotateY":
                                double rad = MathTools.Deg2Rad(values_arr[0]);
                                Matrix4d rotY = Matrix4d.CreateRotationY(rad);
                                FinalTransform = rotY * FinalTransform;
                                break;
                            case "rotateZ":
                                Matrix4d rotZ = Matrix4d.CreateRotationZ(MathTools.Deg2Rad(values_arr[0]));
                                FinalTransform = rotZ * FinalTransform;
                                break;
                        }
                    } 
                }
                return FinalTransform;
            } 
        }
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
        return; 
    }

    private List<Shape> traverse(Node node,Matrix4d transform,Attribs? attribs)
    {
        List<Shape> sceneObjects = new List<Shape>();
        Matrix4d current_transform =node.Transform*transform;
        Attribs? attributes = node.Attributes != null ? node.Attributes : attribs;
        
        if(node.Children != null)
        {
            foreach (Node child in node.Children)
            {
                sceneObjects=sceneObjects.Concat(traverse(child, current_transform, attributes)).ToList();
            }
        }
        else if(node.Object!=null)
        {
            Shape scene_object = node.Object;
            scene_object.Transform(current_transform);
            
            scene_object.Color = attributes.Color;
            scene_object.Material = attributes.Material;
            scene_object.Scale = attributes.Scale;
            scene_object.Texture = attributes.Texture;
            
          
            sceneObjects.Add(scene_object);
        }
        return sceneObjects;
    }

    /// <summary>
    /// Traverses the tree, and returns a list of the objects stored in it.
    /// </summary>
    public List<Shape> RetrieveObjects()
    {
        List<Shape> objects = traverse(Root,Root.Transform,Root.Attributes);
        return objects;
    }
}
