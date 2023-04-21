using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rt004;
public static class ColorTools
{
    /// <summary>
    /// Converts the hexadecimal representation of a color to r,g,b values
    /// </summary>
    /// <param name="color_code"></param>
    /// <returns></returns>
    public static int[] hex_to_rgb(int color_code)
    {
        int b = color_code % 256;
        int g = ((color_code-b)/256)%256;
        int r = (((color_code - b) / 256)-g)/256;

        return new int[3] { r,g,b};
    }

    /// <summary>
    /// Linearly interpolates between 2 Colors. 
    /// </summary>
    /// <param name="color1"></param>
    /// <param name="color2"></param>
    /// <param name="t">The ratio of the "distance" between the 2 colors, a value in the interval [0,1]</param>
    /// <returns></returns>
    public static Color lerpRGB(Color color1, Color color2, float t)
    {
        float r1 = color1.R / 256f;
        float g1 = color1.G / 256f;
        float b1 = color1.B / 256f;

        float r2 = color2.R/256f;
        float g2 = color2.G / 256f;
        float b2 = color2.B / 256f;

        float r = (r1 + ((r2 - r1) * t))*256;
        float g = (g1 + ((g2 - g1) * t))*256;
        float b = (b1 + ((b2 - b1) * t)) * 256;

        return Color.FromArgb((int)r, (int)g, (int)b);
    }

    public static Vector3d ArrToV3d(float[] color)
    {
        return new Vector3d(color[0], color[1], color[2]);
    }

    public static float[] V3dToArr(Vector3d color)
    {
        return new float[] { (float)color.X, (float)color.Y, (float)color.Z };
    }
}

/// <summary>
/// For some shortcuts like rad-deg conversion
/// </summary>
public class MathTools
{

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
    public static double Schlick(Vector3d incoming_light_dir,Vector3d incident_surface_normal,double n_1,double n_2)
    {
        double R0 = Math.Pow((n_1-n_2)/(n_1+n_2),2);
        double cos_theta = Vector3d.Dot(incoming_light_dir,incident_surface_normal);
        return R0 + (1 - R0) * Math.Pow(1 - cos_theta,5);
    }

    /// <summary>
    /// Calculates the fresnel term.
    /// </summary> 
    /// <param name="incoming_ray">A vector pointing from the incident point to the light source</param>
    /// <param name="incident_surface_normal">The surface normal at the incident point</param>
    /// <param name="n_1"></param>
    /// <param name="n_2"></param>>
    /// <returns>Returns the intensity of the reflected light from the surface</returns>
    public static double Fresnel(Vector3d incoming_light_dir,Vector3d incident_surface_normal,double n_1,double n_2)
    {
        double cos_incident = Vector3d.Dot(incoming_light_dir,incident_surface_normal);
        double n_relative = n_1 / n_2;
        double CritAngleCheck = 1 - n_relative * n_relative * (1 - cos_incident * cos_incident);
        if(CritAngleCheck>=0)
        {
            double cos_refracted = Math.Sqrt(CritAngleCheck);

            //S polarized intensity
            double F_s = Math.Pow((n_2*cos_incident-n_1*cos_refracted)/(n_2*cos_incident+n_1*cos_refracted),2);
            //P polarized light
            double F_p= Math.Pow((n_1*cos_refracted-n_2*cos_incident)/(n_1*cos_refracted+n_2*cos_incident), 2);

            return (F_s + F_p) / 2;
        }
        return 1;
    }
}

