using OpenTK.Mathematics;
using System.Drawing;

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
    public static Color lerpRGB(Color color1, Color color2, double t)
    {
        double r1 = color1.R / 256f;
        double g1 = color1.G / 256f;
        double b1 = color1.B / 256f;

        double r2 = color2.R/256f;
        double g2 = color2.G / 256f;
        double b2 = color2.B / 256f;

        double r = (r1 + ((r2 - r1) * t))*256;
        double g = (g1 + ((g2 - g1) * t))*256;
        double b = (b1 + ((b2 - b1) * t)) * 256;

        return Color.FromArgb((int)r, (int)g, (int)b);
    }

    public static Vector3d ArrToV3d(double[] color)
    {
        return new Vector3d(color[0], color[1], color[2]);
    }

    public static double[] V3dToArr(Vector3d color)
    {
        return new double[] { (double)color.X, (double)color.Y, (double)color.Z };
    }
}

/// <summary>
/// For some shortcuts like rad-deg conversion
/// </summary>
public static class MathTools
{
    public static double Deg2Rad(double deg)
    {
        return (deg / 360.0d) * 2.0d * Math.PI;
    }

    public static double Rad2Deg(double rad)
    {
        return rad / (2 * Math.PI) * 360;
    }
}


