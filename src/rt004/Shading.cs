using OpenTK.Mathematics;

namespace rt004;

public abstract class ReflectanceModel
{
    public abstract Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting, List<LightSource> light,bool shadows = true);
}

public class Phong:ReflectanceModel
{
    public float k_s,k_d,k_a,alpha;

    public Phong(float k_a, float k_d, float k_s, float alpha)
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
    public override Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting,List<LightSource> light_sources,bool shadows=true)
    {
        if (ray.FirstIntersectedObject==null || light_sources.Count()==0)//If the object is not intersected or there are                                                                
            return ambient_lighting;                                     //no light sources in the scene, the pixel value
                                                                         //is just the ambient lighting
        Vector3d intersection_point = ray.GetRayPoint(ray.FirstIntersection);
        Vector3d surface_normal = ray.FirstIntersectedObject.SurfaceNormal(ray).Xyz;

        Vector3d view_direction=-ray.Direction;

        Vector3d ambient_component = ambient_lighting * k_a;
        Vector3d diff_spec_comps=Vector3d.Zero;

        foreach (LightSource light in light_sources)
        {
            Ray light_ray = new Ray(light.Position,intersection_point-light.Position,ray.RayScene);
            if (shadows)
            {
                //Since the ray contains the direction normalized, we only have to check if the ray intersects 
                //any objects between the light source, and the object.
                Ray shadowRay = new Ray(intersection_point-Globals.ROUNDING_ERR*light_ray.Direction, -light_ray.Direction,ray.RayScene);
                if (shadowRay.FirstIntersectedObject != null)
                    continue; 
            }
            
            Vector3d incoming_light_direction = -light_ray.Direction;
            Vector3d reflected_light_direction = 2 * Vector3d.Dot(incoming_light_direction, surface_normal) * surface_normal - incoming_light_direction;

            Vector3d diffuse_component = k_d * Math.Max(Vector3d.Dot(incoming_light_direction, surface_normal), 0.0) * light.DiffuseLighting;
            Vector3d specular_component = k_s * Math.Pow(Math.Max(Vector3d.Dot(reflected_light_direction, view_direction), 0.0), alpha) * light.SpecularLighting;

            diff_spec_comps += (diffuse_component + specular_component);
        }
        Vector3d final_color = (ambient_component + diff_spec_comps) * ray.FirstIntersectedObject.Color;

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

    public bool Glossy { get { return false; } }

    public bool Transparent { get { return true; } }

    public ReflectanceModel getReflectance { get { return new Phong(0.0f, 0.0f, 0.0f, 0.0f); } }
}
public class Phong1 : Material
{
    public double RefractionIndex { get { return 2; } }
    public bool Glossy{get{ return true; }}
    public bool Transparent { get { return false; } }
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
}