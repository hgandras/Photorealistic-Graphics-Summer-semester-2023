using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace rt004;

public abstract class Material
{
    public abstract Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting, params LightSource[] light);
}

public class Phong:Material
{
    public float k_s,k_d,k_a,alpha;

    public Phong(float k_s, float k_d, float k_a, float alpha)
    {
        this.k_s = k_s;
        this.k_d = k_d;
        this.k_a = k_a;
        this.alpha = alpha;
    }

    /// <summary>
    /// Calculates the reflected color value based on the phong model. The input values must be in the 
    /// world coordinate system.
    /// </summary>
    /// <param name="ray">The ray shot through the pixel to be evaluated</param>
    /// <param name="ambient_lighting">The color value of the ambient lighting</param>
    /// <param name="light">The light sources in the scene.</param>
    /// <returns></returns>
    public override Vector3d GetReflectedColor(Ray ray, Vector3d ambient_lighting,params LightSource[] light_sources)
    {
        //TODO: Implement for multiple light sources.
        if (ray.FirstIntersectedObject==null || light_sources.Length==0)//If the object is not intersected or there are                                                                
            return ambient_lighting;                            //no light sources in the scene, the pixel value
                                                                //is just the ambient lighting
        Vector3d intersection_point = ray.GetRayPoint(ray.FirstIntersection);
        Vector3d surface_normal = ray.FirstIntersectedObject.SurfaceNormal(intersection_point);

        Vector3d view_direction=ray.Direction;

        float[] result_color = new float[3];
        Vector3d ambient_component = ambient_lighting * k_a;
        Vector3d diff_spec_comps=Vector3d.Zero;

        foreach (LightSource light in light_sources)
        {
            Vector3d incoming_light_direction = light.GetRay(intersection_point).Direction;
            Vector3d refleced_light_direction = 2 * Vector3d.Dot(incoming_light_direction, surface_normal) * surface_normal - incoming_light_direction;


            Vector3d diffuse_component = k_d * Math.Max(Vector3d.Dot(incoming_light_direction, surface_normal), 0.0) * light.DiffuseLighting;
            Vector3d specular_component = k_s * Math.Pow(Math.Max(Vector3d.Dot(refleced_light_direction, view_direction), 0.0), alpha) * light.SpecularLighting;

            diff_spec_comps += (diffuse_component + specular_component);
        }
        Vector3d final_color = (ambient_component + diff_spec_comps) * ray.FirstIntersectedObject.Color;

        return final_color;
    }
}
