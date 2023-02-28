using System;
using System.Numerics;
using OpenTK.Mathematics;

namespace rt004;

public class Scene
{
	
	public Vector3d WorldUpDirection;
	public List<Camera> Camera;
	public List<SceneObject> Objects;

	/// <summary>
	/// Set up basic properties of the scene, like Initialize the camera.
	/// </summary>
	public Scene(Config config)
	{
		//Set up scene and camera configs, and create one Camera object.
		Camera.Add(new Camera(config.CameraConfig));
		float[] v = config.SceneConfig.WorldUpDirection;
		WorldUpDirection = new Vector3d(v[0], v[1], v[2]);
	}

	public void AddObject()
	{

	}

	/// <summary>
	/// Maybe add support for multiple cameras
	/// </summary>
	/// <param name="Cam"></param>
	public void SwapCamera(int Cam)
	{

	}

}

public abstract class SceneEntity
{
	public abstract void Translate(Vector3d translation);
	public abstract void Rotate(Vector3d rotation);
	
}

public interface Projection
{
	public void Project();
}


public class Camera:SceneEntity
{
	public Vector3d Position;
	
	/// <summary>
	/// Init the camera orientation.
	/// </summary>
	/// <param name="Config"></param>
	public Camera(CameraConfig Config)
	{
	}

	/// <summary>
	/// Moves the camera in the 3D world.
	/// </summary>
	/// <param name="Direction"></param>
	public override void Translate(Vector3d translation) 
	{ 

	}
	
	/// <summary>
	/// Rotates the camera around the 3 axes.
	/// </summary>
	/// <param name="Angles"></param>
	public override void Rotate(Vector3d rotation)
	{ 
	
	}

	/// <summary>
	/// Moves the plane along the camera moving direction, so the viewplane
	/// cuts out a larger area from the scene.
	/// </summary>
	/// <param name="Distance"></param>
	public void SetFOV(float Distance)
	{ 
	
	}

	/// <summary>
	/// ???
	/// </summary>
	public void CastRay()
	{

	}

	
}

public class PerspectiveCamera:Camera,Projection
{
    public PerspectiveCamera(CameraConfig Camera): base(Camera)
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


    
}

public class FisheyeLense :Camera,Projection
{
	public FisheyeLense(CameraConfig Camera):base(Camera)
	{

	}

    public void Project()
    {
        throw new NotImplementedException();
    }
}



/// <summary>
/// Represents an SceneObject in the scene
/// </summary>
public class SceneObject:SceneEntity
{
    
    public override void Translate(Vector3d translation)
    {

    }

  
    public override void Rotate(Vector3d rotation)
    {

    }
}
