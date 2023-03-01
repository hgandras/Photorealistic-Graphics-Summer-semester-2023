using System;
using System.ComponentModel;
using System.Numerics;
using OpenTK.Mathematics;

namespace rt004;



/// <summary>
/// The basic instance should be a camera
/// The concrete products should be the different types of cameras
/// 
/// Define abstract creator
/// Override them to create the concrete creators for all of the different cameras.
/// 
/// Call the camera creators in the constructor of the Scene, and in the add camera 
/// method.
/// 
/// TODO: Implement the creation of the particular cameras from the config files.
/// </summary>


public class Scene
{
	
	public Vector3d WorldUpDirection;
	public List<Camera> Camera;
	public Camera Cam;
	public List<SceneObject> Objects;
	public Vector3d LightSourcePosition; //Maybe different config can be created for the light source later

	/// <summary>
	/// Set up basic properties of the scene, like Initialize the camera.
	/// </summary>
	public Scene(Config config)
	{
		// TODO: Add a perspective camera as the default camera
		InitPerspectiveCamera camcreator = new InitPerspectiveCamera();
        Camera.Add(camcreator.Create(config.CameraConfig));
		Cam = Camera[0];
        float[] v = config.SceneConfig.WorldUpDirection;
		float[] l = config.SceneConfig.LightSourcePosition;
		WorldUpDirection = new Vector3d(v[0], v[1], v[2]);
		LightSourcePosition = new Vector3d(l[0], l[1], l[2]);
	}

	public void AddObject()
	{

	}

	/// <summary>
	/// Might add support for multiple cameras
	/// </summary>
	/// <param name="Cam"></param>
	public void SwapCamera(int cam_number)
	{
		Cam = Camera[cam_number];
	}


	/// <summary>
	/// Should call the Camera's RayCaster function, and generate a value for 
	/// all of the pixels.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public void GenerateImge(int width, int height)
	{
		
	}
}

/// <summary>
/// Represents the base class for onjects that can be in the scene, like cameras, objects...
/// </summary>
public interface SceneEntity
{
	public void Translate(Vector3d translation);
	public void Rotate(Vector3d rotation);
    public Vector3d Position { get; set; }


}



public interface Camera:SceneEntity
{
	/// <summary>
	/// Moves the plane along the camera moving direction, so the viewplane
	/// cuts out a larger area from the scene.
	/// </summary>
	/// <param name="Distance"></param>
	public void SetFOV(float Distance){ }
	public Vector3d ViewDirection { get; set; }
	public void Project()
	{ 
	}
}

public class PerspectiveCamera:Camera
{
	public Vector3d Position { get; set; }
	public Vector3d ViewDirection { get; set; }

    public PerspectiveCamera(CameraConfig Camera)
	{
	
	}

	/// <summary>
	/// The projection should return the generated image. The image can also be a 
	/// property of the whole scene.
	/// </summary>
	/// <exception cref="NotImplementedException"></exception>
	public void Project()
	{
		throw new NotImplementedException();
	}

	public void Translate(Vector3d Translation)
	{

	}

	public void Rotate(Vector3d Rotation)
	{

	}


    
}

public class FisheyeLense :Camera
{
	public Vector3d Position { get; set; }
	public Vector3d ViewDirection { get; set; }
	public FisheyeLense(CameraConfig Camera)
	{

	}

    public void Project()
    {
        throw new NotImplementedException();
    }

	public void Translate(Vector3d Translation)
	{

	}

	public void Rotate(Vector3d Rotation)
	{

	}
}


//Creator classes for cameras
public abstract class CamCreator
{
	public abstract Camera Create(CameraConfig config); 
}

public class InitPerspectiveCamera:CamCreator
{
    public override Camera Create(CameraConfig config)
    {
		return new PerspectiveCamera(config);
    }
}

public class InitFisheyeLenseCamera:CamCreator
{
    public override Camera Create(CameraConfig config)
    {
		return new FisheyeLense(config);
    }
}


/// <summary>
/// Represents an SceneObject in the scene. Going to be a tree structure, that stores the vertices, edges, planes etc...
/// </summary>
public class SceneObject:SceneEntity
{
	public Vector3d Position { get; set; }
    
    public void Translate(Vector3d translation)
    {

    }

  
    public void Rotate(Vector3d rotation)
    {

    }
}
