using System;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using OpenTK.Mathematics;
using Util;
using static System.Net.Mime.MediaTypeNames;

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
 */

public class Scene
{
	//Some scene properties
	public Vector3d WorldUpDirection;
    public Vector3d AmbientLighting;
	public Vector3d BackgroundColor;
    public ProjectionPlane Plane;
    public int PlaneWidthPx;
    public int PlaneHeightPx;
	public int maxDepth;
    public int ObjectCount { get { return Objects.Count(); } }
    public int CamCount { get { return Cameras.Count(); } }
	public int LightSourceCount { get { return LightSources.Count(); } }

    //Current scene entities
    public LightSource LightSource;
    public Camera Cam;
    public SceneObject Object;
	public bool DisplayShadows;

	//Private lists for storing all scene entities
	private List<SceneObject> Objects;
	private List<Camera> Cameras;
	private List<LightSource> LightSources;
    private ILogger logger;

	/// <summary>
	/// Constructor for the scene, sets it up based on the config file.
	/// </summary>
	public Scene(Config config)
	{
		logger = Logging.CreateLogger<Scene>();
        //Initializing some properties
        SceneGraph scenegraph = new SceneGraph(config.SceneConfig.SceneGraph);
        Objects = scenegraph.RetrieveObjects();
		LightSources = new List<LightSource>();
        Cameras = new List<Camera> //Add perspective camera by default
		{
			new PerspectiveCamera(config.CameraConfig,this)
		};
        Cam = Cameras[0];
		WorldUpDirection = ColorTools.ArrToV3d(config.SceneConfig.WorldUpDirection); 
		AmbientLighting =ColorTools.ArrToV3d(config.SceneConfig.AmbientLighting);
        Plane = new ProjectionPlane(config.PlaneConfig);
		DisplayShadows = config.SceneConfig.Shadows;
		maxDepth = config.SceneConfig.MaxDepth;
		BackgroundColor = ColorTools.ArrToV3d(config.SceneConfig.BackgroundColor);

		//Add scene elements based on properties
		try
		{
			foreach(LightSource ls in config.SceneConfig.LightSources)
			{
				logger.LogInformation("Light source {} added to scene",ls.GetType());
				AddLightSource(ls);
			}
			logger.LogInformation("Scene initialized with {} objects, and {} light sources", ObjectCount,LightSourceCount);
        }
		catch(IndexOutOfRangeException)
		{
			//TODO: If not all positions, colors are specified in the config file, then initialize the leftover scene entities with default parameters
			logger.LogWarning("Some scene object's properties were not defined, some SceneEnities are added with default parameters");
			//Environment.Exit(1);
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

			case "Plane":
				new_object = new Plane();
				Objects.Add(new_object);
				break;
        }
    }
	public void AddObject(SceneObject scene_object)
	{
		Objects.Add(scene_object);
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

	public void AddLightSource(LightSource ls)
	{
		LightSources.Add(ls);
	}

	/// <summary>
	///TODO: Not yet implemnted, removes an object by deleting a leafnode from the scene tree
	/// </summary>
	public void RemoveObject()
	{
		throw new NotImplementedException();
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
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	public FloatImage SynthesizeImage()
	{
		FloatImage image = new FloatImage(Plane.Width,Plane.Height,3);
		//Iterate through the pixels 
		for(int h=0;h<Plane.Height;h++)
		{
			for(int w=0;w<Plane.Width;w++)
			{
				//Cast the rays for each h,w pixel
				List<Ray> rays=Cam.CastRay(Plane, w, h,Plane.RayPerPixel);
				Vector3d final_color = Vector3d.Zero;
				//Transform ray to world coordinate system
				foreach (Ray ray in rays)
				{
					ray.Origin = Cam.Position;
					ray.Direction = Vector3d.Normalize((Matrix4d.Invert(Cam.CameraTransformMatrix) * new Vector4d(ray.Direction, 1)).Xyz - ray.Origin);
					int Depth = 0;
					final_color += RecursiveRayTrace(ray, Depth);	
				}
                image.PutPixel(w, h, ColorTools.V3dToArr(final_color/Plane.RayPerPixel));
            }
        }
		return image;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="depth"></param>
	/// <returns></returns>
	private Vector3d RecursiveRayTrace(Ray ray,int depth)
	{
        Vector3d pixel_color = Vector3d.Zero;
        for (int i = 0; i < Objects.Count(); i++)
        {
			//Calculate ray intersections
            float[]? intersections = Objects[i].Intersection(ray);
            if (intersections != null)
            {
                ray.Intersections.Add(Objects[i], intersections);
            }
        }
        SceneObject? sO = ray.FirstIntersectedObject;
		if (sO == null)
			return BackgroundColor;
		else
		{ 
            //Calculate reflected color values
            if (DisplayShadows)
			{
				List<SceneObject> otherObjects = new List<SceneObject>(Objects);
				otherObjects.Remove(sO);
				
				pixel_color += sO.Material.getReflectance.GetReflectedColor(ray, AmbientLighting, LightSources, otherObjects);
			}
			else
				pixel_color += sO.Material.getReflectance.GetReflectedColor(ray, AmbientLighting, LightSources);

			if (depth++ >= maxDepth )
				return pixel_color;

            Vector4d Norm = sO.SurfaceNormal(ray);
			Vector3d SurfaceNormal = Norm.Xyz;
			Vector3d ViewDirection = -ray.Direction;
			//Appriximate fresnel term for reflection
            double reflection_coeff = ShadeTools.Fresnel(ViewDirection, SurfaceNormal, 1, sO.Material.RefractionIndex);
			double refraction_coeff = 1 - reflection_coeff;
            if (sO.Material.Glossy)
			{
				//Ray direction is taken with negative, since it is casted from the camera, and not from the surface
				Vector3d ReflectedRayDirection = 2 * Vector3d.Dot(ViewDirection,SurfaceNormal) * SurfaceNormal + ray.Direction;
				ReflectedRayDirection = Vector3d.Normalize(ReflectedRayDirection);
				Ray ReflectedRay = new Ray(ray.GetRayPoint(ray.FirstIntersection), ReflectedRayDirection);
				
				pixel_color += reflection_coeff* RecursiveRayTrace(ReflectedRay, depth);
			}
			if(sO.Material.Transparent)
			{
                //TODO: Solve material boundaries. Right now it assumes that solids are only in contact with air, and no other solids.
                double Direction = Norm.W;
                double cos_incident = Vector3d.Dot(ViewDirection, SurfaceNormal);
				double n_relative;
				if (Direction == 1)
					n_relative = sO.Material.RefractionIndex;
				else
					n_relative = 1/sO.Material.RefractionIndex;
				double CritAngleCheck = 1 - n_relative * n_relative * (1 - cos_incident * cos_incident);
				if (CritAngleCheck >= 0)
				{
					double cos_refraction = Math.Sqrt(CritAngleCheck);
					Vector3d RefractedRayDirection = (n_relative * cos_incident - cos_refraction) * SurfaceNormal - n_relative * ViewDirection;
					Ray RefractedRay = new Ray(ray.GetRayPoint(ray.FirstIntersection), RefractedRayDirection);
					pixel_color +=  refraction_coeff* RecursiveRayTrace(RefractedRay, depth);
				}
			}
		}
		return pixel_color;
	}
}

/// <summary>
/// Represents movable objects in the scene like objects, cameras. Light sources do not inherit from this class, since they behave slightly differently.
/// </summary>
public class SceneEntity
{
    protected Vector4d homogeneous_pos;
	protected float[] pos;
	public Vector3d X { get { return x; } }
	public Vector3d Y { get { return y; } }
	public Vector3d Z { get { return z; } }
	protected Vector3d x;
    protected Vector3d y;
    protected Vector3d z;

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
		x = new Vector3d(1, 0, 0);
		y = new Vector3d(0, 1, 0);
		z = new Vector3d(0, 0, 1);
    }

	/// <summary>
	/// TODO: Transforms the coordinate system of the object by the given transformation matrix.
	/// </summary>
	/// <param name="transform"></param>
	public void Transform(Matrix4d transform,string sys="world")
	{
		switch(sys)
		{
			case "world":
                homogeneous_pos = transform*homogeneous_pos;
                break;
			//TODO: Add case for when the object is rotated around its own system. 

        }
    }

}

/// <summary>
/// Contains plane properties, these are right now are the height/width in pixel.
/// </summary>
public class ProjectionPlane
{
	//Width and height of the plane in pixels
	public int Width;
	public int Height;
	public int RayPerPixel;
	public double W { get { return 2; } }
	public double H { get { return Height * (W / Width); } }
    public double WPixelStep { get {return W / Width; } }
	public double HPixelStep { get { return H / Height ; } }
    public ProjectionPlane(PlaneConfig config)
	{
		Height = config.Height;
		Width = config.Width;
		RayPerPixel = config.RayPerPixel;
	}
}

/// <summary>
/// Class that represents a ray in the scene. Has an origin, direction, and intersection data(only after image was generated).
/// </summary>
public class Ray
{
    public Vector3d Origin;
    public Vector3d Direction;

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
		get 
		{
			Dictionary<SceneObject,float> first_intersection= Intersections.ToDictionary(k=>k.Key,v=>v.Value.Min());
			SceneObject? fi =first_intersection.Values.Count()==0 ? null: first_intersection.MinBy(kvp => kvp.Value).Key;
			return fi; 
		} 
	}
	
	/// <summary>
	/// Returns the parameter t, which represents the ray intersection.
	/// </summary>
	public float FirstIntersection { 
		get 
		{
			return FirstIntersectedObject==null?float.PositiveInfinity:Intersections[FirstIntersectedObject].Min(); 
		}
	}

	/// <summary>
	/// Origin, and direction. The constructor normalizes them before assigning it to its fields.
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="direction"></param>
	public Ray(Vector3d origin, Vector3d direction)
	{
		Origin = origin;
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
	public float ElevationAngle{ get; set; }
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
            Matrix4d camSystem = new Matrix4d(new Vector4d(X, 0), new Vector4d(Y, 0), new Vector4d(Z, 0), new Vector4d(0, 0, 0, 1));
            Matrix4d translation = Matrix4d.CreateTranslation(-Position);
            translation.Transpose();

			return camSystem * translation;
        }
	}

	/// <summary>
	/// Camera's X axis in the world coordinate system
	/// </summary>
	public new Vector3d X { get { return CameraSystem.Row0; } }
	/// <summary>
	/// Camera's Y axis in the world coordinate system
	/// </summary>
	public new Vector3d Y { get { return CameraSystem.Row1; } }
	/// <summary>
	/// Camera's Z axis in the world coordinate system
	/// </summary>
	public new Vector3d Z { get { return CameraSystem.Row2; } }

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
        Matrix4d camSystem = new Matrix4d(new Vector4d(X, 0), new Vector4d(Y, 0), new Vector4d(Z, 0), new Vector4d(0, 0, 0, 1));
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

	/// <summary>
	/// Looks at the object in the scene, but if the scene is no initialized yet, it looks at the origin
	/// </summary>
	/// <param name="objID"></param>
	/// <returns></returns>
	public Vector3d LookAt(int objID)
	{
		if (Scene != null)
		{
			Scene.SwapObject(objID);
			return LookAt(Scene.Object.Position);
		}
		//Also log here
		return Vector3d.Zero;
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
	public virtual List<Ray> CastRay(ProjectionPlane plane, int px_x, int px_y,int ray_per_pixel=1)
	{
		List<Ray> rays = new List<Ray>();
        rays.Add(new Ray(new Vector3d(), new Vector3d()));
		return rays;
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
	/// Casts the given number of rays with through the plane through the given pixel. Inside the pixel the location the ray is going through is 
	/// chosen with a uniform distribution.
	/// </summary>
	/// <param name="plane">The plane to project to </param>
	/// <param name="px_x"></param>
	/// <param name="px_y"></param>
	/// <param name="ray_per_pixel">The number of rays to be casted through a pixel. The ray's direction is chosen randomly within the square with a 
	/// uniform distribution.</param>
	/// <returns>The ray's direction vector in the camera coordinate system.</returns>
	public override List<Ray> CastRay(ProjectionPlane plane, int px_x, int px_y,int ray_per_pixel=1)
	{
		List<Ray> rays = new List<Ray>();
		Random rnd = new Random();
		for(int i = 0;i<ray_per_pixel;i++)
		{
			//Generate offsets in x-y directions
			double offset_x = rnd.NextDouble()*plane.WPixelStep;
			double offset_y = rnd.NextDouble()*plane.HPixelStep;
			//Add ranges to the pixels
            double x = px_x * plane.WPixelStep +offset_x - plane.W / 2;
            double y = px_y * plane.HPixelStep + offset_y - plane.H / 2;
            //The intersected point in the plane in the camera system
            double z = (plane.W / 2f) / Math.Tan(FOV / 360f * Math.PI);
            Vector3d intersection_point = new Vector3d(x, -y, -z); //y is negative for proper image coordinates
			rays.Add(new Ray(Vector3d.Zero,intersection_point));
        }
        return rays;
	}
}

public interface SurfaceProperties
{
    public Material Material { get; set; }
    public float[]? Intersection(Ray ray);
	/// <summary>
	///	If the ray input intersects the object, it returns the surface normal of the intersection point. Else it returns a zero vector. The last element 
	///	of the output vector indicates, whether the ray came from outside, or inside the object. It is 0, if it came from outside, and 1 otherwise.
	/// </summary>
	/// <param name="ray"></param>
	/// <returns></returns>
	public Vector4d SurfaceNormal(Ray ray);
}

/// <summary>
/// Represents an SceneObject in the scene. Also defines operations between the scene objects. 
/// </summary>
public class SceneObject:SceneEntity,SurfaceProperties
{
	public Vector3d Color { get; set; }
	public Material Material { get; set; }
	public double Scale { get; set; }
	
	public SceneObject()
	{
		Color = new Vector3d(255, 0, 0);
		Material = new Phong1();
		Position = new Vector3d(0,0,0);
		//On init the object's coordinates are directed the same way as the world system
		x = new Vector3d(1, 0, 0);
		y = new Vector3d(0, 1, 0);
		z = new Vector3d(0, 0, 1);
	}

    public SceneObject(Vector3d color,Material material)
	{
		Color= color;
		Material = material;
	}
	public virtual float[]? Intersection(Ray ray)
	{
		return new float[] {float.PositiveInfinity};
	}

	public virtual Vector4d SurfaceNormal(Ray point)
	{
		return Vector4d.Zero;
	}
}

public class Sphere:SceneObject
{
	public double Radius { get { return Scale; } set { Scale = value; } }
	public Vector3d Center { get { return Position; } }
	public Sphere():base()
	{
		Scale = 1;
	}

	/// <summary>
	/// Initializes a sphere in the scene with the defined color, position, and radius.
	/// </summary>
	/// <param name="color"></param>
	/// <param name="Pos"></param>
	/// <param name="R"></param>
	public Sphere(Vector3d color, Vector3d Pos,Material material,float R) : base(color,material)
	{
		Scale = R;
		Position = Pos;
		Material = material;
	}

	/// <summary>
	/// Calculates whether the ray and the object intersect each other. It is important that the ray and the 
	/// object's data must be in the same coordinate system. Currently it also accepts a position in the camera system argument.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="pos">The object's position in the camera's system. </param>
	/// <returns></returns>
    public override float[]? Intersection(Ray ray)
	{
		//Calculate coefficients
		double a = Vector3d.Dot(ray.Direction,ray.Direction);
		double b = 2 * Vector3d.Dot(ray.Direction, ray.Origin - Position);
		double c = Vector3d.Dot(Position, Position) + Vector3d.Dot(ray.Origin, ray.Origin) - 2 * Vector3d.Dot(Position, ray.Origin) - Radius * Radius;

		double discriminant = b * b - 4 * a * c;

		List<float> intersectionPoints = new List<float>();

		if (discriminant > 0)
		{
			float t1 = (float)((-b - Math.Sqrt(discriminant)) / 2 * a);
			float t2 = (float)((-b + Math.Sqrt(discriminant)) / 2 * a);
			if (t1 >Globals.ROUNDING_ERR)
				intersectionPoints.Add(t1);
			if (t2 > Globals.ROUNDING_ERR)
				intersectionPoints.Add(t2);
		}
		else if (discriminant == 0)
		{
			float t = (float)(-b / 2 * a);
			if (t > Globals.ROUNDING_ERR)
				intersectionPoints.Add(t);
		}
		if (intersectionPoints.Count() > 0)
			return intersectionPoints.ToArray();
        return null;
    }

    public override Vector4d SurfaceNormal(Ray ray)
	{
		if(!ray.Intersections.ContainsKey(this))
		{
			float[]? intersections = Intersection(ray);
			if(intersections!=null)
				ray.Intersections.Add(this, intersections);
		}
        Vector3d first_intersection = ray.GetRayPoint(ray.FirstIntersection);
        Vector3d Normal = Vector3d.Normalize(first_intersection - Center);
        double angle = Vector3d.CalculateAngle(Normal, ray.Direction);
		if (angle > Math.PI / 2.0 || angle < -Math.PI / 2.0)
			return new Vector4d(Normal,0);
		return new Vector4d(-Normal,1);
	}
}

public class Plane : SceneObject, SurfaceProperties
{

	public Vector3d Normal;

	/// <summary>
	/// Creates parametric equation of the plane with default values.
	/// </summary>
	public Plane()
	{
		Normal = new Vector3d(0,1,0);
		Material = new Phong1();
		Color = new Vector3d(0.9,0.9,0.9);
		Position = Vector3d.Zero;
	}

	/// <summary>
	/// Creates the plane from the normal, and a point that lies on the plane. 
	/// </summary>
	/// <param name="Point"></param>
	/// <param name="Normal"></param>
	public Plane(Vector3d Point,Vector3d Normal,Vector3d Color,Material Material)
	{
		this.Normal =Vector3d.Normalize(Normal);
		this.Material = Material;
		this.Color = Color;
		Position = Point;
	}

	/// <summary>
	/// Calculates the intersection of a plane, and a ray.
	/// </summary>
	/// <param name="ray"></param>
	/// <returns>The parameters t of the ray in the intersection points</returns>
    public override float[]? Intersection(Ray ray)
    {
        float denominator = (float)Vector3d.Dot(ray.Direction, -SurfaceNormal(ray).Xyz);
        if (denominator >Globals.ROUNDING_ERR )
        {
            float numerator = (float)Vector3d.Dot((Position - ray.Origin),-SurfaceNormal(ray).Xyz);
			if(numerator/denominator>Globals.ROUNDING_ERR)
                return new float[] { numerator / denominator };
		}
        return null;
    }

    public override Vector4d SurfaceNormal(Ray ray)
	{     
        double angle = Vector3d.CalculateAngle(Normal, ray.Direction);
		if (angle > Math.PI / 2.0 || angle<-Math.PI/2.0)
			return new Vector4d(Normal, 0);
		return new Vector4d(-Normal, 0);
	}
}

/// <summary>
/// In case something lighting sepcific comes to my mind. Light 
/// </summary>
public interface LightSource
{
	public Ray CastRay(Vector3d point);
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
	/// Casts a ray towards the desired point from the light source.
	/// </summary>
	/// <param name="point"></param>
	/// <returns></returns>
	public Ray CastRay(Vector3d point)
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

    public Ray CastRay(Vector3d point)
	{
		return new Ray(new Vector3d(0, 0, 0), new Vector3d(0, 0, 0));
	}
}
