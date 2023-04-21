﻿using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json;
using System.Xml;
using static rt004.SceneConfig;

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
        public float[] Clr { get; set; }
        public Vector3d Color { get { return ColorTools.ArrToV3d(Clr); } }
        public float Scale { get; set; }
        public string Mat {get;set;}
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
    }

	public class Node
	{
        public Attribs? Attributes { get; set; }
        public Node? Parent { get; set; }
        public List<Node>? Children { get; set; }
        public string? Obj { get; set; }
        public string? Transformation { get; set; }
        public SceneObject? Object { 
            get {
                SceneObject? priv_obj=null;
                if (Obj != null)
                {
                    priv_obj = Activator.CreateInstance(Type.GetType(Globals.ASSEMBLY_NAME + Obj)) as SceneObject;
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
                        float[] values_arr = values.Split(",").Select(float.Parse).ToArray();
                        //Initially just make translations
                        switch (transform)
                        {
                            case "translate":
                                Vector3d values_vec = ColorTools.ArrToV3d(values_arr);
                                FinalTransform *=Matrix4d.Transpose(Matrix4d.CreateTranslation(values_vec));
                                break;
                            //TODO: Add rotation, both around the world, and the object's coordinate system.
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

    private List<SceneObject> traverse(Node node,Matrix4d transform,Attribs? attribs)
    {
        List<SceneObject> sceneObjects = new List<SceneObject>();
        Matrix4d current_transform =node.Transform*transform;
        Attribs? attributes = node.Attributes != null ? node.Attributes : attribs;
        
        if (node.Children != null)
        {
            foreach (Node child in node.Children)
            {
                sceneObjects=sceneObjects.Concat(traverse(child, current_transform, attributes)).ToList();
            }
        }
        else if(node.Object!=null)
        {
            SceneObject scene_object = node.Object;
            scene_object.Transform(current_transform);
            if(scene_object is Plane)
            {
                Console.WriteLine(scene_object);
            }
            if (attributes != null)
            {
                scene_object.Color = attributes.Color;
                scene_object.Material = attributes.Material;
                scene_object.Scale = attributes.Scale;
            }
            else
            {
                scene_object.Color = Vector3d.One;
                scene_object.Material = new Phong1();
                scene_object.Scale = 1;
            }
            sceneObjects.Add(scene_object);
        }
        return sceneObjects;
    }

    /// <summary>
    /// Traverses the tree, and returns a list of the objects stored in it.
    /// </summary>
    public List<SceneObject> RetrieveObjects()
    {
        //Console.WriteLine(Root.Attributes.Material+" "+Root.Attributes.Material);
        List<SceneObject> objects = traverse(Root,Root.Transform,Root.Attributes);
        return objects;
    }
}
