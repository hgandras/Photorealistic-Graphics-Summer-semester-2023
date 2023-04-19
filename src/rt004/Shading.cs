using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace rt004;

public abstract class ReflectanceModel
{
    public abstract Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting, List<LightSource> light,List<SceneObject> objects=null);
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
    /// <returns></returns>
    public override Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting,List<LightSource> light_sources,List<SceneObject> objects=null)
    {
        if (ray.FirstIntersectedObject==null || light_sources.Count()==0)//If the object is not intersected or there are                                                                
            return ambient_lighting;                                     //no light sources in the scene, the pixel value
                                                                         //is just the ambient lighting
        Vector3d intersection_point = ray.GetRayPoint(ray.FirstIntersection);
        Vector3d surface_normal = ray.FirstIntersectedObject.SurfaceNormal(ray);

        Vector3d view_direction=ray.Direction;

        Vector3d ambient_component = ambient_lighting * k_a;
        Vector3d diff_spec_comps=Vector3d.Zero;

        foreach (LightSource light in light_sources)
        {
            if(objects!=null)
            {
                //Since the ray contains the direction normalized, we only have to check if the ray intersects 
                //any objects between the light source, and the object.
                Ray shadowRay = new Ray(intersection_point, -light.CastRay(intersection_point).Direction);

                foreach (SceneObject obj in objects)
                {
                    float[]? intersections = obj.Intersection(shadowRay);
                    if (intersections!=null)
                        shadowRay.Intersections.Add(obj,intersections);
                }
                if (shadowRay.FirstIntersectedObject != null)
                    continue; 
            }
            
            Vector3d incoming_light_direction = light.CastRay(intersection_point).Direction;
            Vector3d refleced_light_direction = 2 * Vector3d.Dot(incoming_light_direction, surface_normal) * surface_normal - incoming_light_direction;

            Vector3d diffuse_component = k_d * Math.Max(Vector3d.Dot(incoming_light_direction, surface_normal), 0.0) * light.DiffuseLighting;
            Vector3d specular_component = k_s * Math.Pow(Math.Max(Vector3d.Dot(refleced_light_direction, view_direction), 0.0), alpha) * light.SpecularLighting;

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
    public bool Glossy { get; }
    public bool Transparent { get; }
    public ReflectanceModel getReflectance { get; }
}

public class Phong1 : Material
{    

    public bool Glossy{get{ return true; }}
    public bool Transparent { get { return false; } }
    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.8f, 0.2f, 10); } }
}
public class Phong2 : Material
{
    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.5f, 0.5f, 150); } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return false; } }
}
public class Phong3 : Material
{
    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.6f, 0.4f, 80); } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return false; } }
}

public class Phong4:Material
{
    public ReflectanceModel getReflectance { get { return new Phong(0.2f, 0.2f, 0.8f, 400); } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return false; } }
}

public class Phong5:Material
{
    public ReflectanceModel getReflectance { get { return new Phong(0.1f, 0.6f, 0.4f, 80); } }

    public bool Glossy { get { return true; } }

    public bool Transparent { get { return false; } }
}