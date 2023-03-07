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


}

/// <summary>
/// For some shortcuts like rad-deg conversion
/// </summary>
public class MathTools
{

}

