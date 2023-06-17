using OpenTK.Mathematics;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;

namespace rt004;

public interface ReflectanceModel
{
    public Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting, List<LightSource> light,bool shadows = true);
}

public class Phong:ReflectanceModel
{
    public double k_s,k_d,k_a,alpha;

    public Phong(double k_a, double k_d, double k_s, double alpha)
    {
        this.k_s = k_s;
        this.k_d = k_d;
        this.k_a = k_a;
        this.alpha = alpha;
    }

    /// <summary>
    /// Calculates the reflected color value based on the phong model. The input values must be in the 
    /// world coordinate system. If objects are added as parameter, the shading will calculate their shadows.
    /// </summary>
    /// <param name="ray">The ray shot through the pixel to be evaluated</param>
    /// <param name="ambient_lighting">The color value of the ambient lighting</param>
    /// <param name="light">The light sources in the scene.</param>
    /// <param name="objects">The objects</param>
    /// <returns></returns>
    public  Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting,List<LightSource> light_sources,bool shadows=true)
    {
        if (ray.FirstIntersectedObject==null || light_sources.Count()==0)//If the object is not intersected or there are                                                                
            return ambient_lighting;                                     //no light sources in the scene, the pixel value
                                                                         //is just the ambient lighting
        Vector3d intersection_point = ray.GetRayPoint(ray.FirstIntersection);
        Vector3d surface_normal;
        if(ray.FirstIntersectedObject.Texture!=null)
        {
            surface_normal = ray.FirstIntersectedObject.Texture.Normal(ray);
            intersection_point = ray.FirstIntersectedObject.Texture.BumpMap(ray);
        }
        else
            surface_normal = ray.FirstIntersectedObject.SurfaceNormal(ray);
        bool IsOutside = ShadeTools.CheckIncidentLocation(surface_normal, ray.Direction);
        if (!IsOutside)
            surface_normal = -surface_normal;

        Vector3d view_direction=-ray.Direction;

        Vector3d ambient_component = ambient_lighting * k_a;
        Vector3d diff_spec_comps=Vector3d.Zero;

        foreach (LightSource light in light_sources)
        {
            Ray light_ray = new Ray(light.Position,intersection_point-light.Position,ray.RayScene);
            if(shadows)
            {
                //Since the ray contains the direction normalized, we only have to check if the ray intersects 
                //any objects between the light source, and the object.
                Ray shadowRay = new Ray(intersection_point-light_ray.Direction, -light_ray.Direction,ray.RayScene);
                if (shadowRay.FirstIntersectedObject != null)
                    continue; 
            }
            
            Vector3d incoming_light_direction = -light_ray.Direction;
            Vector3d reflected_light_direction = 2 * Vector3d.Dot(incoming_light_direction, surface_normal) * surface_normal - incoming_light_direction;

            Vector3d diffuse_component = k_d * Math.Max(Vector3d.Dot(incoming_light_direction, surface_normal), 0.0) * light.DiffuseLighting;
            Vector3d specular_component = k_s * Math.Pow(Math.Max(Vector3d.Dot(reflected_light_direction, view_direction), 0.0), alpha) * light.SpecularLighting;

            diff_spec_comps += (diffuse_component + specular_component);
        }
        Vector3d final_color;
        if(ray.FirstIntersectedObject.Texture!=null)
        {
            final_color = (ambient_component + diff_spec_comps) * ray.FirstIntersectedObject.Texture.Sample(ray);
        }
        else
            final_color = (ambient_component + diff_spec_comps) * ray.FirstIntersectedObject.Color;

        return final_color;
    }
}

public class CookTorrance : ReflectanceModel
{

    public double k_s, k_a,m;

    public CookTorrance(double k_s,double k_a,double m)
    {
        this.k_a = k_a;
        this.k_s = k_s;
        this.m = m;
    }

    private double MicrofacetDistribution(double n_dot_h)
    {
        if (n_dot_h < 0)
        {
            return 0.0;
        }
        double delta = Math.Acos(n_dot_h);
        double result = Math.Pow(Math.E, -1 * Math.Pow(Math.Tan(delta) / m,2)) / (Math.PI * m * m * Math.Pow(n_dot_h, 4));

        return result;
    }

    private double MaskingDistribution(double n_dot_h,double n_dot_v, double n_dot_l, double v_dot_h)
    {
        double[] results = new double[] { 1.0, 2 * n_dot_h * n_dot_v / v_dot_h, 2 * n_dot_h * n_dot_l / v_dot_h };
        return results.Min();
    }
    public Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting, List<LightSource> light_sources, bool shadows = true)
    {
        if (ray.FirstIntersectedObject == null || light_sources.Count() == 0)
            return ambient_lighting;
        
        //Vectors in the scene
        Vector3d intersection_point = ray.GetRayPoint(ray.FirstIntersection);
        Vector3d ideal_surface_normal;
        if (ray.FirstIntersectedObject.Texture != null)
        {
            ideal_surface_normal = ray.FirstIntersectedObject.Texture.Normal(ray);
            intersection_point = ray.FirstIntersectedObject.Texture.BumpMap(ray);
        }
        else
            ideal_surface_normal = ray.FirstIntersectedObject.SurfaceNormal(ray);
        Vector3d view_direction = -ray.Direction;
        bool IsOutside = ShadeTools.CheckIncidentLocation(ideal_surface_normal, ray.Direction);
        if (!IsOutside)
            ideal_surface_normal = -ideal_surface_normal;
        Vector3d ambient_component = ambient_lighting * k_a;

        
        double n_dot_v = Math.Max(Vector3d.Dot(ideal_surface_normal,view_direction),0.0);
        

        Vector3d specular_component = Vector3d.Zero;
        foreach(LightSource light in light_sources)
        {
            //Light ray and adding shadows
            Ray light_ray = new Ray(light.Position, intersection_point - light.Position, ray.RayScene);
            if (shadows)
            {
                Ray shadowRay = new Ray(intersection_point - light_ray.Direction, -light_ray.Direction, ray.RayScene);
                if (shadowRay.FirstIntersectedObject != null)
                    continue;
            }

            Vector3d light_direction=-light_ray.Direction;
            Vector3d half_angle = Vector3d.Normalize(light_direction+view_direction);

            //Used dot products
            double n_dot_l = Math.Max(Vector3d.Dot(ideal_surface_normal, light_direction), 0.0);
            double n_dot_h = Math.Max(Vector3d.Dot(ideal_surface_normal,half_angle),0.0);
            double v_dot_h = Math.Max(Vector3d.Dot(view_direction, half_angle), 0.0);
            if (n_dot_l > 0)
            {
                double specular_coefficient = MicrofacetDistribution(n_dot_h) * MaskingDistribution(n_dot_h, n_dot_v, n_dot_l, v_dot_h) / (4 * n_dot_l * n_dot_v);

                specular_component += specular_coefficient * light.SpecularLighting;
            }
        }
        Vector3d final_color;
        if (ray.FirstIntersectedObject.Texture != null)
        {
            final_color = (ambient_component + specular_component) * ray.FirstIntersectedObject.Texture.Sample(ray);
        }
        else
            final_color = (ambient_component + specular_component) * ray.FirstIntersectedObject.Color;

        return final_color;
    }
}


/// <summary>
/// Interface that defines materials. The getMaterial readonly property returns a ReflectanceModel that is preset, and 
/// its parameters cannot be modified.
/// </summary>
public interface Material
{
    public double RefractionIndex { get; }
    public bool Glossy { get; }
    public bool Transparent { get; }
    public ReflectanceModel getReflectance { get; }
}

public class Air : Material
{
    public double RefractionIndex { get { return 1; } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return true; } }

    public ReflectanceModel getReflectance { get { return new Phong(0.0f, 0.0f, 0.0f, 0.0f); } }
}
public class Phong1 : Material
{
    public double RefractionIndex { get { return 1.5; } }
    public bool Glossy{get{ return true; }}
    public bool Transparent { get { return true; } }
    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.8f, 0.2f, 10); } }
}
public class Phong2 : Material
{
    public double RefractionIndex { get { return 4; } }
    
    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.5f, 0.5f, 150); } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return false; } }
}
public class Phong3 : Material
{
    public double RefractionIndex { get { return 4; } }

    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.6f, 0.4f, 80); } }

    public bool Glossy { get { return false; } }

    public bool Transparent { get { return false; } }
}

public class Phong4:Material
{
    public double RefractionIndex { get { return 4; } }
    public ReflectanceModel getReflectance { get { return new Phong(0.2f, 0.2f, 0.8f, 400); } }

    public bool Glossy { get { return false; } }

    public bool Transparent { get { return true; } }
}

public class Phong5:Material
{
    public double RefractionIndex { get { return 4; } }
    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.6f, 0.4f, 80); } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return false; } }
}

public class CookTorrance1 : Material
{
    public double RefractionIndex { get { return 1.5; } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return false; } }

    public ReflectanceModel getReflectance { get { return new CookTorrance(0.1, 0.5, 0.3); } }
}


public interface Texture
{
    public Vector3d Normal(Ray ray);
    public Vector3d BumpMap(Ray ray);
    public Vector3d Sample(Ray ray);
}

public class CheckerBoard:Texture
{
    public double w, h;
    public Vector3d color1, color2;

    public CheckerBoard()
    {
        this.w = 0.3;
        this.h = 0.15;
        this.color1=new Vector3d(0.0,0.0,0.0);
        this.color2 = new Vector3d(1.0, 1.0, 1.0);
    }

    public Vector3d BumpMap(Ray ray)
    {
        return ray.GetRayPoint(ray.FirstIntersection);
    }

    public Vector3d Normal(Ray ray)
    {
        return ray.FirstIntersectedObject.SurfaceNormal(ray);
    }

    public Vector3d Sample(Ray ray)
    {
        Vector2d SurfaceCoordinates;
        if(ray.FirstIntersectedObject== null)
        {
            return ray.RayScene.BackgroundColor;
        }
        SurfaceCoordinates = ray.FirstIntersectedObject.SurfaceIntersection(ray);

        double u= SurfaceCoordinates.X;
        double v = SurfaceCoordinates.Y;

        int x_parity = (int)Math.Floor(u / w) % 2;
        int y_parity = (int)Math.Floor(v / h) % 2;

        int final_tile = (x_parity + y_parity) % 2;
        switch(final_tile)
        {
            case 0:
                return color1;

            default: 
                return color2;
        }
    }
}

public class SinCosBumpMap : Texture
{
    public Vector3d BumpMap(Ray ray)
    {
        Vector2d uv = ray.FirstIntersectedObject.SurfaceIntersection(ray);
        double u = uv.X;
        double v = uv.Y;

        double B= Math.Cos(20*v)*Math.Sin(20*u)/100;
        Vector3d FinalPoint = ray.GetRayPoint(ray.FirstIntersection) + B * ray.FirstIntersectedObject.SurfaceNormal(ray);

        return FinalPoint;
    }

    public Vector3d Normal(Ray ray)
    {
        Matrix3d LocalSystem = ray.FirstIntersectedObject.GetLocalCoordinate(ray);
        Vector3d N = LocalSystem.Row2;
        Vector3d U = LocalSystem.Row0;
        Vector3d V = LocalSystem.Row1;

        Vector3d n_cross_v = Vector3d.Cross(N,V);
        Vector3d n_cross_u = Vector3d.Cross(N, U);

        Func<double, double, double> dBdU = (u, v) =>  Math.Cos(20*v)*Math.Cos(20 * u)*20 / 100;
        Func<double, double, double> dBdV = (u, v) => -Math.Sin(20*v)*Math.Sin(20*u)*20/100;

        Vector2d uv = ray.FirstIntersectedObject.SurfaceIntersection(ray);
        double u = uv.X;
        double v = uv.Y;

        Vector3d modified_normal = N + (dBdU(u, v) * n_cross_v - dBdV(u, v) * n_cross_u) / N.Length;

        return Vector3d.Normalize(modified_normal);
    }

    public Vector3d Sample(Ray ray)
    {
        return ray.FirstIntersectedObject.Color;
    }
}


public static class ShadeTools
{
    /// <summary>
    /// Calculates the approximate fresnel term.
    /// </summary> 
    /// <param name="incoming_ray">A vector pointing from the incident point to the light source</param>
    /// <param name="incident_surface_normal">The surface normal at the incident point</param>
    /// <param name="n_1"></param>
    /// <param name="n_2"></param>>
    /// <returns>A double representing the intensity of the reflected light from the surface</returns>
    public static double Schlick(Vector3d incoming_light_dir, Vector3d incident_surface_normal, double n_1, double n_2)
    {
        double R0 = Math.Pow((n_1 - n_2) / (n_1 + n_2), 2);
        double cos_theta = Vector3d.Dot(incoming_light_dir, incident_surface_normal);
        return R0 + (1 - R0) * Math.Pow(1 - cos_theta, 5);
    }

    /// <summary>
    /// Calculates the fresnel term.
    /// </summary> 
    /// <param name="incoming_ray">A vector pointing from the incident point to the light source</param>
    /// <param name="incident_surface_normal">The surface normal at the incident point</param>
    /// <param name="n_1"></param>
    /// <param name="n_2"></param>>
    /// <returns>Returns the intensity of the reflected light from the surface</returns>
    public static double Fresnel(Vector3d incoming_light_dir, Vector3d incident_surface_normal, double n_1, double n_2)
    {
        double cos_incident = Vector3d.Dot(incoming_light_dir, incident_surface_normal);
        double n_relative = n_1 / n_2;
        double CritAngleCheck = 1 - n_relative * n_relative * (1 - cos_incident * cos_incident);
        if (CritAngleCheck >= 0)
        {
            double cos_refracted = Math.Sqrt(CritAngleCheck);

            //S polarized intensity
            double F_s = Math.Pow((n_2 * cos_incident - n_1 * cos_refracted) / (n_2 * cos_incident + n_1 * cos_refracted), 2);
            //P polarized light
            double F_p = Math.Pow((n_1 * cos_refracted - n_2 * cos_incident) / (n_1 * cos_refracted + n_2 * cos_incident), 2);

            return (F_s + F_p) / 2;
        }
        return 1;
    }

    /// <summary>
    /// Checks whether the ray is coming inside, or outside of the object, by checking the angle between 
    /// the normal and the incident ray direction. Since the normal always points outside, this indicates if the normal has
    /// to be flipped.
    /// </summary>
    /// <param name="normal"></param>
    /// <param name="incident_direction"></param>
    /// <returns>bool, true if the ray and normal are on the outside surface of the object, false if ray is 
    /// on the inside surface.</returns>
    public static bool CheckIncidentLocation(Vector3d normal, Vector3d incident_direction)
    {
        double angle = Vector3d.CalculateAngle(normal, incident_direction);
        if (angle > Math.PI / 2.0 || angle < -Math.PI / 2.0)
            return true;
        return false;
    }
}