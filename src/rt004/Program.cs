using System.Formats.Asn1;
using System.Numerics;
using Util;
using System.Drawing;
using System.Globalization;
//using System.Numerics;

namespace rt004;


internal class Program
{
  

  static void Main(string[] args)
  {
        // Parameters.
        // TODO: parse command-line arguments and/or your config file.
        int wid = 600;
        int hei = 450;
        string fileName = "demo.pfm";

        Color color1 = Color.FromArgb(255, 255, 255);
        Color color2 = Color.FromArgb(0, 255, 0);

        foreach (string arg in args)
        {
            string[] key_val = arg.Split('=');
            string key, value;
            if (key_val.Length == 2)
            {
                key = key_val[0];
                value = key_val[1];
                switch(key)
                {
                    case "width":
                        if(!int.TryParse(value, out wid))
                        {
                            Logging.InvalidArgument(key);
                            wid = 600;
                        }
                        break;
                    case "height":
                        if(!int.TryParse(value, out hei))
                        {
                            Logging.InvalidArgument(key);
                            hei = 450;
                        }
                        break;

                    case "color1":
                        if (!UInt32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint c1))
                        {
                            Logging.InvalidArgument(key);
                            color1 = Color.FromArgb(255, 255, 255);

                        }
                        else
                        {
                            int[] col1 = ColorTools.hex_to_rgb((int)c1);
                            color1 = Color.FromArgb(col1[0], col1[1], col1[2]);
                        }
                        break;
                    case "color2":
                        if (!UInt32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint c2))
                        {

                            Logging.InvalidArgument(key);
                            color2 = Color.FromArgb(0, 255, 0);
                            break;
                        }
                        else
                        {
                            int[] col2 = ColorTools.hex_to_rgb((int)c2);
                            color2 = Color.FromArgb(col2[0], col2[1], col2[2]);
                        }
                        break;
                    case "file_name":
                        fileName = value;
                        break;
                }
            }
            else
            {
                Logging.InvalidArgument();              
            }
        }

        

        // HDR image.
        FloatImage fi = new FloatImage(wid, hei, 3);

        // TODO: put anything interesting into the image.
        // TODO: use fi.PutPixel() function, pixel should be a float[3] array [R, G, B]
        float radius = (float)Math.Min(wid,hei)/2f;
                      
        for (int h = 0; h < hei; h++)
        {
          for (int w = 0; w <wid; w++)
          {
                double centered_x = w - wid / 2d;
                double centered_y = h - hei / 2d;

                
                if (Math.Pow((centered_x), 2) + Math.Pow((centered_y), 2) < Math.Pow(radius, 2))
                {
                    Color color_rgb = ColorTools.lerpRGB(color1, color2, 1f/hei*(float)h);
                    fi.PutPixel(w, h, new float[3] { color_rgb.R, color_rgb.G, color_rgb.B });
                }
                else
                    fi.PutPixel(w, h, new float[3] { 256f, 256f, 256f });

                
            }
        }

        //This part might not work correctly
        string[] name_extension = fileName.Split(".");
        string file_extension;
        if(name_extension.Length==2)
        {
            file_extension = name_extension[1];
            switch (file_extension)
            {
                case "pfm":
                    fi.SavePFM(fileName);
                    break;
                case "hdr":
                    fi.SaveHDR(fileName);
                    break;
            }
        }
        else
        {
            fi.SavePFM(fileName+".pfm");
        }
        


        Console.WriteLine("HDR image is finished.");
  }
}
