using System;
using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Util;

namespace rt004;



/// <summary>
/// The idea is that the scene supports multiple objects and cameras. There is always a current camera,
/// and a current object, and you can switch between them. Operations can only be done on the current ones.
/// 
/// During projection, everything is going to be calculated in the camera's coordinate system, so objects and light
/// source coordinates all will be transformed during ray calulations.
/// </summary>

/*
 * TODO: Create empty camera type constructors
 * 
 * COMMENT: When we add a camera to the Scene, it should be bounded, and the camera should have the world up vector as its field, so
 * the X,Y,Z basis vectors in world coordinates can be accessed as a property. A camera can also be removed, so its world up Vector3d 
 * can be set to null. Or a camera just simply has the scene it is contained in as a field, so it can acces its properties.
 * 
 * TODO: Modify config file, so light sources can be added from it
 * 
 * TODO: Implement other shapes, light source, and camera types if I have time. Maybe implement another shading method.
 */

public class Scene
{
	//Some scene properties
	public Vector3d WorldUpDirection;
    public Vector3d AmbientLighting;
    public Plane Plane;
    public int PlaneWidthPx;
    public int PlaneHeightPx;
    public int ObjectCount { get { return Objects.Count(); } }
    public int CamCount { get { return Cameras.Count(); } }

    //Current scene entities
    public LightSource LightSource;
    public Camera Cam;
    public SceneObject Object;

	//Private lists for storin all scene entities
	private List<SceneObject> Objects;
	private List<Camera> Cameras;
	private List<LightSource> LightSources;

	/// <summary>
	/// Empty constructor, so Scene can be initialized without config.
	/// TODO: Either implement this, or delete.
	/// </summary>
	public Scene()
	{
		Objects = new List<SceneObject>();
		Cameras = new List<Camera>();
	}

	/// <summary>
	/// Constructor for the scene, sets it up based on the config file.
	/// </summary>
	public Scene(Config config)
	{ 

		//Initializing some properties
		Objects= new List<SceneObject>();
		LightSources = new List<LightSource>();
        Cameras = new List<Camera> //Add perspective camera by default
		{
			new PerspectiveCamera(config.CameraConfig,this)
		};
        Cam = Cameras[0];
		WorldUpDirection = ColorTools.ArrToV3d(config.SceneConfig.WorldUpDirection); 
		AmbientLighting =ColorTools.ArrToV3d(config.SceneConfig.AmbientLighting);
		Plane = new Plane(config.PlaneConfig);

		//Add scene elements based on properties
		try
		{
			for(int i=0;i<config.SceneConfig.Objects.Count();i++)
			{
				AddObject(config.SceneConfig.Objects[i]);
				Object = Objects[i];
				Object.Position =ColorTools.ArrToV3d( config.SceneConfig.ObjectPositions[i]);
				Object.Color = ColorTools.ArrToV3d(config.SceneConfig.ObjectColors[i]);
				Object.Material = new Phong(0.2f,0.8f,0.2f,150); //TODO: This should be added to config file.
			}
			for(int i=0;i<config.SceneConfig.PointLightPositions.Count();i++)
			{
				PointLight new_light=new PointLight();
				new_light.Position = ColorTools.ArrToV3d(config.SceneConfig.PointLightPositions[i]);
				LightSources.Add(new_light);
			}
            for (int i = 0; i < config.SceneConfig.DirectionalLightDirections.Count(); i++)
            {
                DirectionalLight new_light = new DirectionalLight();
                new_light.Direction = ColorTools.ArrToV3d(config.SceneConfig.DirectionalLightDirections[i]);
                LightSources.Add(new_light);
            }
			Console.WriteLine("Scene Initialized");
        }
		catch(IndexOutOfRangeException)
		{
			//TODO: If not all positions, colors are specified in the config file, then initialize the leftover scene entities with default parameters
			Console.WriteLine("Not all scene objects defined in config file has properties, exiting.");
			Environment.Exit(1);
		}
		
	}

	/// <summary>
	/// Adds an object with the default parameters provided in the object type's class definition.
	/// </summary>
	/// <param name="ObjPosition">The position of the object</param>
	/// <param name="shape">String that represents the object type. Possible values: Sphere</param>
	public void AddObject(string shape)
	{
		SceneObject new_object;
		switch(shape)
		{
			case "Sphere":
				new_object = new Sphere();
                Objects.Add(new_object);
                break;

			case "Box":
				new_object = new Box();
				Objects.Add(new_object);
				break;	
        }
    }

	public void AddLightSource(string type)
	{
		LightSource new_light_source;
		switch(type)
		{
			case "Point":
				new_light_source = new PointLight();
				LightSources.Add(new_light_source);
				break;

			case "Directional":
                new_light_source = new DirectionalLight();
                LightSources.Add(new_light_source);
                break;
        }
	}

	/// <summary>
	///TODO: Not yet implemnted, because I think I want an ID system for the objects, lightsources, and cameras.
	/// </summary>
	public void RemoveObject()
	{
		
	}

	/// <summary>
	/// Changes between the cameras.
	/// </summary>
	/// <param name="cam_ID"></param>
	public void SwapCamera(int cam_ID)
	{
		Cam = Cameras[cam_ID];
	}

	/// <summary>
	/// Changes the object.
	/// </summary>
	/// <param name="obj_ID"></param>
	public void SwapObject(int obj_ID)
	{
		Object = Objects[obj_ID];
	}

	public void SwapLightSource(int ls_ID)
	{
		LightSource = LightSources[ls_ID];
	}	
	/// <summary>
	/// For each pixel in the plane get generate the ray direction vectors. After that, for all rays, calculate 
	/// the intersection of all objects in the scene, and based on that the reflection, and the pixel color value
	/// in float. 
	/// TODO: This part can be parallelized.
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	public FloatImage SynthesizeImage()
	{
		FloatImage image = new FloatImage(Plane.PxWidth,Plane.PxHeight,3);

        //Transform all object's positions to the camera's system for intersection calculation
        List<Vector3d> obj_positions = new List<Vector3d>();
		foreach (SceneObject obj in Objects)
		{
			obj_positions.Add(Cam.CameraTransform(obj.Position));
			Console.WriteLine(obj_positions[obj_positions.Count - 1]);
		}
		//Iterate through the pixels 
		for(int h=0;h<Plane.PxHeight;h++)
		{
			for(int w=0;w<Plane.PxWidth;w++)
			{
				//Cast the rays for each h,w pixel
				Ray ray=Cam.CastRay(Plane, w, h);
				for(int i=0;i<Objects.Count();i++)
				{
					//If the ray intersects the object, set pixel color value to the object's value, else black.
					float[]? intersections = Objects[i].Intersection(ray, obj_positions[i]);
					if (intersections != null)
					{
                        ray.Intersections.Add(Objects[i], intersections);
					}
				}
				SceneObject? sO = ray.FirstIntersectedObject;
				if (sO == null)
					image.PutPixel(w, h, new float[] { 0.1f,0.2f,0.3f});
				else
				{
                    //Transform ray to world coordinates
                    //Console.WriteLine(ray.Origin + " " + " " + ray.Direction + " " + Cam.Position);
                    ray.Origin = Cam.Position;

					//Normalize is probebly unnecessary, because ray direction vectors are normalized in previous calculations.
                    ray.Direction = Vector3d.Normalize((Matrix4d.Invert(Cam.CameraTransformMatrix)*new Vector4d(ray.Direction,1)).Xyz-ray.Origin);
					//Console.WriteLine(ray.Origin+" "+" "+ray.Direction+" "+Cam.Position);

					Vector3d pixel_color = sO.Material.GetReflectedColor(ray, AmbientLighting, LightSources[0], LightSources[1]);
					float[] fin_color = ColorTools.V3dToArr(pixel_color/256);
					image.PutPixel(w,h,fin_color);
				}
            }
		}
		return image;
	}
}

/// <summary>
/// Represents movable objects in the scene like objects, cameras. Light sources do not inherit from this class, since they behave slightly differently.
/// </summary>
public class SceneEntity
{
    protected Vector4d homogeneous_pos;
	protected float[] pos;
    public Vector3d Position
    {
        get {  return homogeneous_pos.Xyz; }
        set
        {
            pos[0] = (float)value.X;
            pos[1] = (float)value.Y;
            pos[2] = (float)value.Z;

            homogeneous_pos = new Vector4d(pos[0], pos[1], pos[2], 1);
        }
    }

	public SceneEntity()
	{
		pos=new float[3];
	}

	/// <summary>
	/// Translates the object.
	/// </summary>
	/// <param name="Translation"></param>
    public void Translate(Vector3d Translation)
    {
		Matrix4d translation_matrix = Matrix4d.CreateTranslation(Translation);
		translation_matrix.Transpose();
        homogeneous_pos = translation_matrix * homogeneous_pos;
    }

    /// <summary>
    /// Rotatation around world system X axis
    /// </summary>
    /// <param name="Rotation"></param>
    private void RotateX(Vector3d Rotation)
    {
        Matrix4d rotationX = Matrix4d.CreateRotationX(Rotation.X);
        homogeneous_pos = rotationX * homogeneous_pos;
    }
    /// <summary>
    /// Rotatation around world system Y axis
    /// </summary>
    /// <param name="Rotation"></param>
    public void RotateY(Vector3d Rotation)
    {
        Matrix4d rotationY = Matrix4d.CreateRotationY(Rotation.Y);
		homogeneous_pos = rotationY * homogeneous_pos;
    }
    /// <summary>
    /// Rotatation around world system Z axis
    /// </summary>
    /// <param name="Rotation"></param>
    public void RotateZ(Vector3d Rotation)
    {
        Matrix4d rotationZ = Matrix4d.CreateRotationZ(Rotation.Z);
		homogeneous_pos = rotationZ * homogeneous_pos;
    }
}

/// <summary>
/// Contains plane properties, these are right now are the height/width in pixel.
/// TODO: Decide whether the plane properties will just be part of the camera class, because most of its 
/// parameters are projection specific.
/// </summary>
public class Plane
{
	public int PxWidth;
	public int PxHeight;

	public Plane(PlaneConfig config)
	{
		PxHeight = config.Height;
		PxWidth = config.Width;
	}
}

/// <summary>
/// Class that represents a ray in the scene. Has an origin, direction, and intersection data(only after image was generated).
/// </summary>
public class Ray
{
	/// <summary>
	/// Used when the intersection function is called. It stores the intersections of each 
	/// encountered object, and based on this it creates the image. After the image is generated, 
	/// it contains only those objects as keys, that were intersected, so there are no null values.
	/// </summary>
	public Dictionary<SceneObject, float[]> Intersections=new Dictionary<SceneObject, float[]>();
	/// <summary>
	/// Returns the first object that the ray encountered, based on the t value intervals in the Intersections property.
	/// </summary>
	public SceneObject? FirstIntersectedObject { 
		get {
			Dictionary<SceneObject,float> first_intersection= Intersections.ToDictionary(k=>k.Key,v=>v.Value.Min());
			SceneObject? fi =first_intersection.Values.Count()==0 ? null: first_intersection.MinBy(kvp => kvp.Value).Key;
			return fi; 
			} }
	
	/// <summary>
	/// Returns the parameter t, which represents the ray intersection.
	/// </summary>
	public float FirstIntersection { 
		get { 
			return FirstIntersectedObject==null?float.PositiveInfinity:Intersections[FirstIntersectedObject].Min(); 
			} }

	public Vector3d Origin;

	public Vector3d Direction;

	

	/// <summary>
	/// Origing, and direction. The constructor normalizes them before assigning it to its fields.
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="direction"></param>
	public Ray(Vector3d origin, Vector3d direction)
	{
		
		Origin =origin==Vector3d.Zero? origin:Vector3d.Normalize(origin);
		Direction = direction==Vector3d.Zero?direction:Vector3d.Normalize(direction);
	}

	/// <summary>
	/// Returns a point on the ray, given parameter value t. (R=O+t*D , where O is the ray origin, and 
	/// D is the  ray direction).
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
    public Vector3d GetRayPoint(float t)
    {
		return Origin+t*Direction;
    }
}



public class Camera : SceneEntity
{
	private Scene? Scene;

	//TODO: Angle getters.
	public float ElevationAngle{get;set;}
	public float AzimuthAngle { get; set; }

	/// <summary>
	/// The point the camera's -Z axis is going through.
	/// </summary>
	public Vector3d Target {
		get { return new Vector3d(t[0], t[1], t[2]); }
		set
		{
			t[0] = (float)value.X;
			t[1] = (float)value.Y;
			t[2] = (float)value.Z;
		}
	}
	private float[] t = new float[3];

	public Camera(CameraConfig config)
	{
		t = config.Target;
		pos = config.Position;
		homogeneous_pos = new Vector4d(pos[0], pos[1], pos[2], 1);
	}

	public Camera(CameraConfig config,Scene scene):this(config)
	{
		Scene = scene;
	}

	/// <summary>
	/// A 3x3 matrix which's rows contain the camera coordinate system axes in the world coordinate system.
	/// </summary>
	public Matrix3d CameraSystem 
	{
		get 
		{
			if (Scene != null)
			{
				Vector3d Z = Vector3d.Normalize(Position - Target); //Target is in -Z
				Vector3d X = Vector3d.Normalize(Vector3d.Cross(Scene.WorldUpDirection, Z));
				Vector3d Y = Vector3d.Normalize(Vector3d.Cross(Z,X));
				return new Matrix3d(X, Y, Z);
			}
			else
			{
				Console.WriteLine("WARNING: Scene is not set for camera.");//Create logging for this 
				return new Matrix3d();
			}
		}
	}
	
	public Matrix4d CameraTransformMatrix
	{
		get 
		{
            Matrix4d camSystem = new Matrix4d(new Vector4d(CamX, 0), new Vector4d(CamY, 0), new Vector4d(CamZ, 0), new Vector4d(0, 0, 0, 1));
            Matrix4d translation = Matrix4d.CreateTranslation(-Position);
            translation.Transpose();

			return camSystem * translation;
        }
	}

	/// <summary>
	/// Camera's X axis in the world coordinate system
	/// </summary>
	public Vector3d CamX { get { return CameraSystem.Row0; } }
	/// <summary>
	/// Camera's Y axis in the world coordinate system
	/// </summary>
	public Vector3d CamY { get { return CameraSystem.Row1; } }
	/// <summary>
	/// Camera's Z axis in the world coordinate system
	/// </summary>
	public Vector3d CamZ { get { return CameraSystem.Row2; } }

	/// <summary>
	/// This lets the camera to associate it with a scene, if it was not set in the constructor.
	/// </summary>
	/// <param name="scene"></param>
	public void BindScene(Scene scene)
	{
		Scene=scene;
	}

	/// <summary>
	/// Returns the coordinates of a point in the camera system.
	/// </summary>
	/// <param name="point">Coordinates of a point in world system.</param>
	/// <returns></returns>
	public Vector3d CameraTransform(Vector3d point)
	{
        Matrix4d camSystem = new Matrix4d(new Vector4d(CamX, 0), new Vector4d(CamY, 0), new Vector4d(CamZ, 0), new Vector4d(0, 0, 0, 1));
        Matrix4d translation = Matrix4d.CreateTranslation(-Position);
        translation.Transpose(); //OpenTK puts the translation to the last ROW, not the last column ???

        Vector4d lookat_cam_system = new Vector4d(point, 1);

        return (camSystem * translation * lookat_cam_system).Xyz;
    }


	/// <summary>
	/// Sets the camera to look at the specified point, and transforms that point's position to 
	/// the camera's coordinate system, and returns it(I)
	/// </summary>
	/// <param name="target">The object to be transformed in world coordinates</param>
	/// <param name="cam_position">The position of the camera in the world system</param>
	public Vector3d LookAt(Vector3d target)
	{
		Target=target;
		return CameraTransform(target);
	}

	public Vector3d LookAt(int objID)
	{
		Scene.SwapObject(objID);
		return LookAt(Scene.Object.Position);
	}

    /// <summary>
    /// Sets the camera to look at the specified object's location, and transforms that point's position to 
    /// the camera's coordinate system.
    /// </summary>
    /// <param name="obj"></param>
    public Vector3d LookAt(SceneObject obj)
	{
		return LookAt(obj.Position);
	}

    /// <summary>
    /// Returns the object's position in the camera world space, if the camera is position in the origin.
    /// </summary>
    /// <param name="sceneEntity"></param>
    public Vector3d TransformToCameraSystem(SceneEntity sceneEntity)
	{
		return CameraSystem * sceneEntity.Position;
	}
	public virtual Ray CastRay(Plane plane, int px_x, int px_y)
	{
		return new Ray(new Vector3d(),new Vector3d());
	}
}

public class PerspectiveCamera:Camera
{
    public float FOV;
    public PerspectiveCamera(CameraConfig config) :base(config)
	{
		FOV = config.FOV;
	}
	public PerspectiveCamera(CameraConfig config, Scene scene):base(config,scene)
	{
		FOV=config.FOV;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="plane">The plane to project to </param>
	/// <param name="px_x"></param>
	/// <param name="px_y"></param>
	/// <returns>The ray's direction vector in the camera coordinate system.</returns>
	/// <exception cref="NotImplementedException"></exception>
	public override Ray CastRay(Plane plane, int px_x, int px_y)
	{
		//Calculate size of the plane, if the width is considered to be size 2.
		//TODO: These caculations are the same for every ray, move the out of the function so it is not calculated unecessarily, can be moved to Camera base constructor.
		double w = 2;
		double h = plane.PxHeight * (w /plane.PxWidth);
		
		double w_pixel_step = w / plane.PxWidth;
		double h_pixel_step=h/plane.PxHeight;
	
        double x = px_x * w_pixel_step + w_pixel_step / 2 - w / 2;
		double y = px_y * h_pixel_step + h_pixel_step / 2 - h / 2;
		//The intersected point in the plane in the camera system
		double z = (w / 2f) / Math.Tan(FOV / 360f * Math.PI);
        Vector3d intersection_point =new Vector3d(x,-y,-z);
		
		return new Ray( Vector3d.Zero,Vector3d.Normalize(intersection_point));
	}
}

/// <summary>
/// Might be implemented later.
/// </summary>
public class ParallelCamera :Camera
{
    public float PlaneDistance { get; set; }
    
    public ParallelCamera(CameraConfig config) :base(config)
	{
		
    }
	public ParallelCamera(CameraConfig config, Scene scene):base(config,scene)
	{

	}
	/// <summary>
	/// Casts perpendicular rays to the plane from the pixel.
	/// </summary>
	/// <param name="plane"></param>
	/// <param name="px_x"></param>
	/// <param name="px_y"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
    public override Ray CastRay(Plane plane, int px_x, int px_y)
    {
        throw new NotImplementedException();
    }
}


public interface SurfaceProperties
{
    public Material Material { get; set; }
    public float[] Intersection(Ray ray, Vector3d position);
	public Vector3d SurfaceNormal(Vector3d point);

}

/// <summary>
/// Represents an SceneObject in the scene. Also defines operations between the scene objects. 
/// </summary>
public class SceneObject:SceneEntity,SurfaceProperties
{
	public Vector3d Color { get; set; }
	public Material Material { get; set; }
	public SceneObject()
	{
		Color = new Vector3d(255, 0, 0);
		Material = new Phong(0.2f,0.8f,0.2f,100);
		Position = new Vector3d(0,0,0);
	}
	public SceneObject(Vector3d color,Material material)
	{
		Color= color;
		Material = material;
	}
	//TODO: Might be useful to define a local coordinate system for this.
	public virtual float[] Intersection(Ray ray,Vector3d position)
	{
		return new float[] {float.PositiveInfinity};
	}

	public virtual Vector3d SurfaceNormal(Vector3d point)
	{
		return Vector3d.Zero;
	}
   
}

public class Sphere:SceneObject
{
	public float Radius;
	public Vector3d Center { get { return Position; } }


	public Sphere():base()
	{
		Radius = 1;
	}
	/// <summary>
	/// Initializes a sphere in the scene with the defined color, position, and radius.
	/// </summary>
	/// <param name="color"></param>
	/// <param name="Pos"></param>
	/// <param name="R"></param>
	public Sphere(Vector3d color, Vector3d Pos,Material material,float R) : base(color,material)
	{
		Radius = R;
		Position = Pos;
		Material = material;
	}

	/// <summary>
	/// Calculates whether the ray and the onject intersect each other. It is important that the ray and the 
	/// object's data must be in the same coordinate system. Currently it will also accept a position argument.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="pos">The object's position in the camera's system. </param>
	/// <returns></returns>
    public override float[] Intersection(Ray ray, Vector3d cam_sys_pos)
	{
		//Calculate coefficients
		double a = Vector3d.Dot(ray.Direction,ray.Direction);
		double b = 2 * Vector3d.Dot(ray.Direction, ray.Origin - cam_sys_pos);
		double c = Vector3d.Dot(cam_sys_pos, cam_sys_pos) + Vector3d.Dot(ray.Origin, ray.Origin) - 2 * Vector3d.Dot(cam_sys_pos, ray.Origin) - Radius * Radius;

		double discriminant = b * b - 4 * a * c;

		if (discriminant > 0)
		{
			float t1 = (float)((-b - Math.Sqrt(discriminant)) / 2 * a);
			float t2 = (float)((-b + Math.Sqrt(discriminant)) / 2 * a);
			return new float[] { t1, t2 };
		}
		else if (discriminant == 0)
			return new float[] { (float)(-b / 2 * a) };

        return null;
    }

	/// <summary>
	/// Returns the normal of a given point, if it is on the sphere. (Not a very general function, maybe 
	/// parameterizing the sphere by azimuth and zenith angle would be better).
	/// </summary>
	/// <param name="point"></param>
	/// <returns></returns>
	public override Vector3d SurfaceNormal(Vector3d point)
	{
		return Vector3d.Normalize(Center - point);
	}
}

public class Box:SceneObject,SurfaceProperties
{

	public Box() : base()
	{

	}
	public Box(Vector3d color,Material material):base(color,material)
	{

	}

	public new float[] Intersection(Ray ray,Vector3d cam_sys_pos)
	{
		return new float[] { };
	}

	public new Vector3d SurfaceNormal(Vector3d point)
	{
		return Vector3d.Zero;
	}
}

public class Cylinder:SceneObject,SurfaceProperties
{
	public float Radius,Height;

	public Cylinder(Vector3d color,Material material,float Rad,float Hei,Vector3d Pos):base(color,material)
	{
		Radius = Rad;
		Height = Hei;
		Position = Pos;
	}


    public new float[] Intersection(Ray ray, Vector3d cam_sys_pos)
    {
        return new float[] { };
    }

    public new Vector3d SurfaceNormal(Vector3d point)
    {
        return Vector3d.Zero;
    }


}

/// <summary>
/// In case something lighting sepcific comes to my mind. Light 
/// </summary>
public interface LightSource
{
	public Ray GetRay(Vector3d point);
	public Vector3d DiffuseLighting { get; set; }
	public Vector3d SpecularLighting { get; set; }
}

public class PointLight:LightSource
{
	public Vector3d Position;
    public Vector3d DiffuseLighting { get; set; }
    public Vector3d SpecularLighting { get; set; }

	public PointLight()
	{
		Position = new Vector3d(0, 0, 0);
		DiffuseLighting = new Vector3d(1, 1,1);
		SpecularLighting = new Vector3d(1, 1, 1);
	}

    public PointLight(Vector3d position, Vector3d diffuse_lighting, Vector3d specular_lighting)
	{
		Position = position;
		DiffuseLighting = diffuse_lighting;
		SpecularLighting = specular_lighting;
	}

	/// <summary>
	/// Calculates the 
	/// </summary>
	/// <param name="point"></param>
	/// <returns></returns>
	public Ray GetRay(Vector3d point)
	{
		return new Ray(Position,point-Position);
    }
}

public class DirectionalLight:LightSource	
{
    public Vector3d Direction;
    public Vector3d DiffuseLighting { get; set; }
    public Vector3d SpecularLighting { get; set; }
    public DirectionalLight()
    {
        Direction = new Vector3d(1, 1, 1);
        DiffuseLighting = new Vector3d(1, 1, 1);
        SpecularLighting = new Vector3d(1, 1, 1);
    }

    public Ray GetRay(Vector3d point)
	{
		return new Ray(new Vector3d(0, 0, 0), new Vector3d(0, 0, 0));
	}
}
