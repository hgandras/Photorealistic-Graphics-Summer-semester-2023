using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
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

	public FloatImage Image;

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
    public LightSource? LightSource;
    public Camera Cam;
    public Shape? Object;
	public bool DisplayShadows;

	//Lists for storing all scene entities
	public readonly List<Shape> Objects;
	public readonly List<Camera> Cameras;
	public readonly List<LightSource> LightSources;

	//Logger
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
        WorldUpDirection = ColorTools.ArrToV3d(config.SceneConfig.WorldUpDirection);
        Cameras = new List<Camera> //Add perspective camera by default
		{
			new PerspectiveCamera(config.CameraConfig,this)
		};
        Cam = Cameras[0];
		AmbientLighting =ColorTools.ArrToV3d(config.SceneConfig.AmbientLighting);
        Plane = new ProjectionPlane(config.PlaneConfig);
		DisplayShadows = config.SceneConfig.Shadows;
		maxDepth = config.SceneConfig.MaxDepth;
		BackgroundColor = ColorTools.ArrToV3d(config.SceneConfig.BackgroundColor);
        Image = new FloatImage(Plane.Width, Plane.Height, 3);

        //Add scene elements based on properties
        try
		{
			foreach(LightSource ls in config.SceneConfig.LightSources)
			{
				AddLightSource(ls);
                logger.LogInformation("Light source {} added to scene", ls.GetType());
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
		Shape new_object;
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
	public void AddObject(Shape scene_object)
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
	/// in double.
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	public void SynthesizeImage()
	{
		//Iterate through the pixels 
		for(int h=0;h<Plane.Height;h++)
		{
			for (int w = 0; w < Plane.Width; w ++)
			{
				//Cast the rays for each h,w pixel
				List<Ray> rays = Cam.CastRay(Plane, w, h, Plane.RayPerPixel, false);
				Vector3d final_color = Vector3d.Zero;
				foreach (Ray ray in rays)
				{
					int Depth = 0;
					final_color += RecursiveRayTrace(ray, Depth);
				}
				Image.PutPixel(w, h, Array.ConvertAll(ColorTools.V3dToArr(final_color / Plane.RayPerPixel), item => (float)item));
			}
        }
	}

	private readonly object imageLock = new object();

	/// <summary>
	/// Renders the image utilising parallel programming.
	/// </summary>
	/// <returns></returns>
    public void SynthesizeImageParallel()
    {
        List<Task> tasks = new List<Task>();
        //Iterate through the pixels 
        for (int h = 0; h < Plane.Height; h++)
        {
			for (int w = 0; w < Plane.Width; w++)
			{
				var w_cpy = w;
				var h_cpy = h;
				Task task = Task.Run(() => WritePixel(h_cpy, w_cpy));
				tasks.Add(task);
			}
        }
        Task.WaitAll(tasks.ToArray());
    }

	private void WritePixel(int h,int w)
	{
        //Cast the rays for each h,w pixel
        List<Ray> rays = Cam.CastRay(Plane, w, h, Plane.RayPerPixel, false);
        Vector3d final_color = Vector3d.Zero;
        foreach (Ray ray in rays)
        {
            final_color += RecursiveRayTrace(ray, 0);
        }
        lock (imageLock)
        {
            Image.PutPixel(w, h, Array.ConvertAll(ColorTools.V3dToArr(final_color / Plane.RayPerPixel), item => (float)item));
        }
    }


    private Vector3d RecursiveRayTrace(Ray ray,int depth)
	{
        Vector3d pixel_color = Vector3d.Zero;
        Shape? FirstIntersectedObject = ray.FirstIntersectedObject;
		if (FirstIntersectedObject == null)
			return BackgroundColor;
		
        //Calculate reflected color values
		pixel_color += FirstIntersectedObject.Material.getReflectance.GetReflectedColor(ray, AmbientLighting, LightSources,DisplayShadows);

		if (depth++ >= maxDepth )
			return pixel_color;

        Vector3d SurfaceNormal = FirstIntersectedObject.SurfaceNormal(ray);
		bool IsOutside = ShadeTools.CheckIncidentLocation(SurfaceNormal, ray.Direction);
		if (!IsOutside)
			SurfaceNormal = -SurfaceNormal;
        //Ray direction is taken with negative, since it is casted from the camera, and not from the surface
        Vector3d ViewDirection = -ray.Direction;

		//Refraction indices of the materials on the boundary
		double n1=1;
        if (ray.RefractionIndex!=double.PositiveInfinity)
            n1 = ray.RefractionIndex;
		double n2 = FirstIntersectedObject.Material.RefractionIndex;
		if (!IsOutside) //Swap the two values, if the ray is coming out from the object.
			(n1,n2)= (n2,n1);

		//Approximate fresnel term for reflection
        double reflection_coeff = ShadeTools.Fresnel(ViewDirection, SurfaceNormal, n1, n2);
		double refraction_coeff = 1 - reflection_coeff;

        if (FirstIntersectedObject.Material.Glossy)
		{
			Vector3d ReflectedRayDirection = 2 * Vector3d.Dot(ViewDirection,SurfaceNormal) * SurfaceNormal + ray.Direction;
			ReflectedRayDirection = Vector3d.Normalize(ReflectedRayDirection);
			Ray ReflectedRay = new Ray(ray.GetRayPoint(ray.FirstIntersection), ReflectedRayDirection,this);
			pixel_color += reflection_coeff* RecursiveRayTrace(ReflectedRay, depth);
		}
		if(FirstIntersectedObject.Material.Transparent)
		{
            //TODO: Solve material boundaries. Right now it assumes that solids are only in contact with air, and no other solids.
            double cos_incident = Vector3d.Dot(ViewDirection, SurfaceNormal);
			double n_relative = n1/n2;
            double CritAngleCheck = 1 - n_relative * n_relative * (1 - cos_incident * cos_incident);
			if (CritAngleCheck >= 0)
			{
				double cos_refraction = Math.Sqrt(CritAngleCheck);
				Vector3d RefractedRayDirection = (n_relative * cos_incident - cos_refraction) * SurfaceNormal - n_relative * ViewDirection;
				Ray RefractedRay = new Ray(ray.GetRayPoint(ray.FirstIntersection), RefractedRayDirection,this);
				pixel_color +=  refraction_coeff* RecursiveRayTrace(RefractedRay, depth);
			}
		}
		
		return pixel_color;
	}
}

/// <summary>
/// Represents movable objects in the scene like objects, cameras. Light sources do not inherit from this class, since they behave slightly differently.
/// </summary>
public class SceneObject
{
    protected Vector4d homogeneous_pos;
	protected double[] pos;

	//Local coordinate system coordinates in world coordinate system
	public Vector3d X { get { return x.Xyz; } }
	public Vector3d Y { get { return y.Xyz; } }
	public Vector3d Z { get { return z.Xyz; } }
	protected Vector4d x;
    protected Vector4d y;
    protected Vector4d z;
	protected Matrix4d hom_xyz { get { return Matrix4d.Transpose(new Matrix4d(x, y, z, Vector4d.One)); } }

	protected Scene? Scene;

    public Vector3d Position
    {
        get {  return homogeneous_pos.Xyz; }
        set
        {
            pos[0] = (double)value.X;
            pos[1] = (double)value.Y;
            pos[2] = (double)value.Z;

            homogeneous_pos = new Vector4d(pos[0], pos[1], pos[2], 1);
        }
    }

	public SceneObject()
	{
		pos=new double[3] { 0,0,0};
		x = new Vector4d(1, 0, 0,1);
		y = new Vector4d(0, 1, 0,1);
		z = new Vector4d(0, 0, 1,1);
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="transform"></param>
	public virtual void Transform(Matrix4d transform)
	{
        homogeneous_pos = transform*homogeneous_pos;
		//Transform local coordinates only by the rotations.
		Matrix4d rotation = Matrix4d.Transpose(Matrix4d.Transpose(transform).ClearTranslation());
		x = rotation * x;
		y = rotation * y;
		z = rotation * z;
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
				
				return new Matrix3d(X, Y, Z);
			}
			else
			{	
				Console.WriteLine("WARNING: Scene is not set for camera.");
				return new Matrix3d();
			}
		}
	}
	
	public Matrix4d TransformMatrix
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
    /// Returns the coordinates of a point in the local system.
    /// </summary>
    /// <param name="point">Coordinates of a point in world system.</param>
    /// <returns></returns>
    public Vector3d TransformToLocal(Vector3d point)
    {
        Matrix4d camSystem = new Matrix4d(new Vector4d(X, 0), new Vector4d(Y, 0), new Vector4d(Z, 0), new Vector4d(0, 0, 0, 1));
        Matrix4d translation = Matrix4d.CreateTranslation(-Position);
        //translation.Transpose(); //OpenTK puts the translation to the last ROW, not the last column ???

        Vector4d lookat_cam_system = new Vector4d(point, 1);

        return (camSystem * translation * lookat_cam_system).Xyz;
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
	public Scene RayScene;

	/// <summary>
	/// Used when the intersection function is called. It stores the intersection parameter t as keys,
	/// and the objects as values. 
	/// </summary>
	public SortedDictionary<double, Shape> Intersections = new SortedDictionary<double, Shape>();

	/// <summary>
	/// Returns the first object that the ray encountered, based on the t value intervals in the Intersections property.
	/// </summary>
	public Shape? FirstIntersectedObject {
		get
		{
			Shape? fi = Intersections.Values.Count() == 0 ? null : Intersections[Intersections.Keys.Min()];
			return fi;
		}
	}

	/// <summary>
	/// Number of intersections per intersected shapes
	/// </summary>
	public Dictionary<Shape, int> IntersectionsPerShape
	{
		get
		{
			Dictionary<Shape, int> intersection_counts = new Dictionary<Shape, int>();
			foreach(Shape shape in Intersections.Values)
			{
				if (intersection_counts.ContainsKey(shape))
				{
					intersection_counts[shape]++;
				}
				else
					intersection_counts[shape] = 1;
			}
			return intersection_counts;
		}	
	}

	//These two attributes can be used to determine which material the ray is in right now.

	/// <summary>
	/// Shapes which don't contain the ray's origin.
	/// </summary>
	public List<Shape> OriginNotContained
	{
		get
		{
			List<Shape> shapes = new List<Shape>();
			foreach(Shape shape in IntersectionsPerShape.Keys)
			{
				if (IntersectionsPerShape[shape] % 2 == 0)
					shapes.Add(shape);
			}
			return shapes;
		}
	}

	/// <summary>
	/// Shapes which contain the ray's origin.
	/// </summary>
	public List<Shape> OriginContained
	{
		get
		{
            List<Shape> shapes = new List<Shape>();
            foreach (Shape shape in IntersectionsPerShape.Keys)
            {

				if (IntersectionsPerShape[shape] % 2 == 1 && shape.GetType()!=typeof(Plane))
				{
					shapes.Add(shape);
				}
            }
            return shapes;
        }
	}

	/// <summary>
	/// Returns the parameter t, which represents the ray intersection. If no objects are intersected return infinity.
	/// </summary>
	public double FirstIntersection {
		get
		{
			return FirstIntersectedObject == null ? double.PositiveInfinity : Intersections.Keys.Min();
		}
	}

	/// <summary>
	/// Returns the refraction index of the medium that the ray is going to be in after refraction.
	/// </summary>
	public double RefractionIndex
	{
		get
		{
			List<Shape> origin_contained = OriginContained;
			foreach(Shape shape in Intersections.Values)
			{
				if(origin_contained.Contains(shape) && shape!=FirstIntersectedObject)
				{
					return shape.Material.RefractionIndex;
				}
			}
			return double.PositiveInfinity;
		}
	}

	/// <summary>
	/// Origin, and direction. The constructor normalizes them before assigning it to its fields. An
	/// index of refraction can also be assigned to the constructor. It indicates the medium the 
	/// ray originates from, or is going through. We can assume that the ray goes through only one 
	/// medium, since a new ray is always casted when a surface is reached. If the ray is used 
	/// for other purposes, like calculating shadows, this parameter is not used in those cases
	/// anymway. 
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="direction"></param>
	public Ray(Vector3d origin, Vector3d direction)
	{
		Origin = origin;
		Direction = direction == Vector3d.Zero ? direction : Vector3d.Normalize(direction); 
    }

	/// <summary>
	/// Adds a scene to the ray, and calculates the intersections. It is important that the 
	/// ray origin and direction should be in the world coordinate system.
	/// </summary>
	/// <param name="scene"></param>
	/// <param name="origin"></param>
	/// <param name="direction"></param>
	public Ray(Vector3d origin, Vector3d direction, Scene scene) :this(origin,direction)
	{
		RayScene = scene;
		GetIntersections(scene);
	}

	/// <summary>
	/// Returns a point on the ray, given parameter value t. (R=O+t*D , where O is the ray origin, and 
	/// D is the  ray direction).
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	public Vector3d GetRayPoint(double t)
	{
		return Origin + t * Direction;
	}	

	/// <summary>
	/// Transforms the ray origin and direction to world coordinate system, and fills up the intersections
	/// dictionary.
	/// </summary>
	/// <param name="scene">The scene containing the shapes</param>
	public void GetIntersections(Scene scene)
	{
        foreach (Shape shape in scene.Objects)
        {
            //Calculate ray intersections
            double[]? intersections = shape.Intersection(this);
            if (intersections != null)
            {
				foreach(double t in intersections)
				{
					double param = t;
					if (Intersections.ContainsKey(param))
						param += Globals.ROUNDING_ERR;
					Intersections.Add(param, shape);
                }
            }
        }
    }
}

public class Camera : SceneObject
{
	/// <summary>
	/// The point the camera's -Z axis is going through.
	/// </summary>
	public Vector3d Target {
		get { return new Vector3d(t[0], t[1], t[2]); }
		set
		{
			t[0] = (double)value.X;
			t[1] = (double)value.Y;
			t[2] = (double)value.Z;
		}
	}
	private double[] t = new double[3];

	public Camera(CameraConfig config)
	{
		t = config.Target;
		pos = config.Position;
		homogeneous_pos = new Vector4d(pos[0], pos[1], pos[2], 1);
    }

	public Camera(CameraConfig config,Scene scene):this(config)
	{
		Scene = scene;

        z =new Vector4d( Vector3d.Normalize(Position - Target),1); //Target is in -Z
        x =new Vector4d( Vector3d.Normalize(Vector3d.Cross(Scene.WorldUpDirection, Z)),1);
        y =new Vector4d (Vector3d.Normalize(Vector3d.Cross(Z, X)),1);
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
		return TransformToLocal(target);
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
    public Vector3d LookAt(Shape obj)
	{
		return LookAt(obj.Position);
	}

    

	public virtual List<Ray> CastRay(ProjectionPlane plane, int px_x, int px_y,int ray_per_pixel=1,bool CameraSystem=false)
	{
		List<Ray> rays = new List<Ray>();
        rays.Add(new Ray(new Vector3d(), new Vector3d()));
		return rays;
	}
}

public class PerspectiveCamera:Camera
{
    public double FOV;
    public PerspectiveCamera(CameraConfig config) :base(config)
	{
		FOV = config.FOV;
	}
	public PerspectiveCamera(CameraConfig config, Scene scene):base(config,scene)
	{
		FOV=config.FOV;
	}

	/// <summary>
	/// Casts the given number of rays through the given pixel. Inside the pixel the location the ray is going through is 
	/// chosen with a uniform distribution.
	/// </summary>
	/// <param name="plane">The plane to project to </param>
	/// <param name="px_x"></param>
	/// <param name="px_y"></param>
	/// <param name="ray_per_pixel">The number of rays to be casted through a pixel. The ray's direction is chosen randomly within the square with a 
	/// uniform distribution.</param>
	/// <returns>The ray's direction vector in the chosen coordinate system. If it returns it in the
	/// world coordinate system, it also calculates the intersections.</returns>
	public override List<Ray> CastRay(ProjectionPlane plane, int px_x, int px_y,int ray_per_pixel=1,bool CameraSystem=false)
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
			Ray casted_ray = new Ray(Vector3d.Zero, intersection_point);
			if(!CameraSystem)
			{
				casted_ray.RayScene = Scene;
                casted_ray.Origin = this.Position;
                casted_ray.Direction = Vector3d.Normalize((Matrix4d.Invert(this.TransformMatrix) * new Vector4d(casted_ray.Direction, 1)).Xyz - casted_ray.Origin);
				casted_ray.GetIntersections(Scene);
            }
			rays.Add(casted_ray);
        }
        return rays;
	}
}


/// <summary>
/// Represents an SceneObject in the scene. Also defines operations between the scene objects. 
/// </summary>
public class Shape:SceneObject
{
	public Vector3d Color { get; set; }
	public Material Material { get; set; }
	public double Scale { get; set; }
	public Texture? Texture { get; set; }
	
	public Shape():base()
	{
		Color = new Vector3d(255, 0, 0);
		Material = new Phong1();
		Position = new Vector3d(0,0,0);
		Texture = null;
	}

    public Shape(Vector3d color,Material material,Texture? texture=null):base()
	{
		Color= color;
		Material = material;
		Texture = texture;
	}

	public virtual Matrix3d GetLocalCoordinate(Ray ray)
	{
		return Matrix3d.Identity;
	}

	public virtual double[]? Intersection(Ray ray)
	{
		return new double[] {double.PositiveInfinity};
	}

	public virtual Vector3d SurfaceNormal(Ray point)
	{
		return Vector3d.Zero;
	}

	public virtual Vector2d SurfaceIntersection(Ray ray)
	{
		return Vector2d.Zero;
	}
}

public class Sphere:Shape
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
	public Sphere(Vector3d color, Vector3d Pos,Material material,double R) : base(color,material)
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
    public override double[]? Intersection(Ray ray)
	{
		//Calculate coefficients
		double a = Vector3d.Dot(ray.Direction,ray.Direction);
		double b = 2 * Vector3d.Dot(ray.Direction, ray.Origin - Position);
		double c = Vector3d.Dot(Position, Position) + Vector3d.Dot(ray.Origin, ray.Origin) - 2 * Vector3d.Dot(Position, ray.Origin) - Radius * Radius;

		double discriminant = b * b - 4 * a * c;

		List<double> intersectionPoints = new List<double>();

		if (discriminant > 0)
		{
			double t1 = (double)((-b - Math.Sqrt(discriminant)) / 2 * a);
			double t2 = (double)((-b + Math.Sqrt(discriminant)) / 2 * a);
			if (t1 >Globals.ROUNDING_ERR)
				intersectionPoints.Add(t1);
			if (t2 > Globals.ROUNDING_ERR)
				intersectionPoints.Add(t2);
		}
		else if (discriminant == 0)
		{
			double t = (double)(-b / 2 * a);
			if (t > Globals.ROUNDING_ERR)
				intersectionPoints.Add(t);
		}
		if (intersectionPoints.Count() > 0)
			return intersectionPoints.ToArray();
        return null;
    }

    public override Vector3d SurfaceNormal(Ray ray)
	{
		if(!ray.Intersections.ContainsValue(this))
		{
			double[]? intersections = Intersection(ray);
			if(intersections!=null)
			foreach(double t in intersections)
				ray.Intersections.Add(t,this);
		}
        Vector3d first_intersection = ray.GetRayPoint(ray.FirstIntersection);
        Vector3d Normal = Vector3d.Normalize(first_intersection - Center);
		return Normal;
	}

    public override Vector2d SurfaceIntersection(Ray ray)
    {
		if(ray.FirstIntersectedObject==null)
		{
			return Vector2d.PositiveInfinity;
		}
		Vector3d Normal = ray.FirstIntersectedObject.SurfaceNormal(ray);

		double Azimuth = Math.Acos(-Vector3d.Dot(Y,Normal));
		double Elevation = Math.Acos(Vector3d.Dot(X,Normal)/Math.Sin(Azimuth))/2*Math.PI;

		double v = Azimuth / Math.PI;
		double u = Vector3d.Dot(Vector3d.Cross(Y, X), Normal) > 0 ? Elevation : 1 - Elevation;

		return new Vector2d(u,v);
    }

	
    public override Matrix3d GetLocalCoordinate(Ray ray)
    {
		Vector3d N = SurfaceNormal(ray);
		Vector3d U = Vector3d.Cross(N,Y);
		Vector3d V = Vector3d.Cross(U,N);

		return new Matrix3d(U,V,N);
    }
}

public class Plane : Shape
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

		//Set up local coordinate system
		y = new Vector4d(Normal, 1);

		double D = -Vector3d.Dot(Normal, Position);
		double x_coordinate = Position.X+1;
		double y_coordinate = Position.Y;
		double z_coordinate = (Normal.X * x_coordinate + Normal.Y * y_coordinate + D)/-Normal.Z;

		Vector3d x_vector = Vector3d.Normalize(new Vector3d(x_coordinate,y_coordinate,z_coordinate));
		x = new Vector4d(x_vector,1);
		z = new Vector4d(Vector3d.Cross(X,Y),1);	
	}

    public override Matrix3d GetLocalCoordinate(Ray ray)
    {
		return new Matrix3d(X,Y,Z);
    }

    /// <summary>
    /// Calculates the intersection of a plane, and a ray.
    /// </summary>
    /// <param name="ray"></param>
    /// <returns>The parameters t of the ray in the intersection points</returns>
    public override double[]? Intersection(Ray ray)
    {
        double denominator = (double)Vector3d.Dot(ray.Direction, -SurfaceNormal(ray));
        if (denominator >Globals.ROUNDING_ERR )
        {
            double numerator = (double)Vector3d.Dot((Position - ray.Origin),-SurfaceNormal(ray));
			if(numerator/denominator>Globals.ROUNDING_ERR)
                return new double[] { numerator / denominator };
		}
        return null;
    }

    public override Vector3d SurfaceNormal(Ray ray)
	{
		return Normal;
	}

    public override void Transform(Matrix4d transform)
    {
        base.Transform(transform);
        Matrix4d rotation = Matrix4d.Transpose(Matrix4d.Transpose(transform).ClearTranslation());
        Normal = (rotation * new Vector4d(Normal,1)).Xyz;
    }

    public override Vector2d SurfaceIntersection(Ray ray)
    {
		Vector3d intersection = ray.GetRayPoint(ray.FirstIntersection);
		Vector2d Pos = new Vector2d(intersection.X-Position.X,intersection.Z-Position.Z);
		Matrix2d local = new Matrix2d(X.Xz,Z.Xz);
		return Matrix2d.Invert(local) * Pos;
    }
}

/// <summary>
/// In case something lighting sepcific comes to my mind. Light 
/// </summary>
public class LightSource:SceneObject
{
	public virtual Vector3d DiffuseLighting { get; set; }
	public virtual Vector3d SpecularLighting { get; set; }
}

public class PointLight:LightSource
{
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
}

public class DirectionalLight:LightSource
{
    public Vector3d Direction;
    public DirectionalLight()
    {
        Direction = new Vector3d(1, 1, 1);
        DiffuseLighting = new Vector3d(1, 1, 1);
        SpecularLighting = new Vector3d(1, 1, 1);
    }

}
