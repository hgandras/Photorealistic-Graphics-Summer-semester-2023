using System.Formats.Asn1;
using System.Numerics;
using Util;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using OpenTK.Mathematics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
//using System.Numerics;

namespace rt004;

internal class Program
{
  

  static void Main(string[] args)
  {

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole(options => { options.TimestampFormat = "[HH:mm:ss] "; }).AddDebug().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<Program>();
        // Parameters.
        //Default parameters are specified in the config file, and the command-line arguments 
        //can overwrite them.
        Config config=new Config("Config.json");
        GeneralConfig generalConfig = config.GeneralConfig;
        logger.LogInformation("Config file loaded");

        int wid = config.PlaneConfig.Width;
        int hei = config.PlaneConfig.Height;
        string fileName = generalConfig.fileName;
        
        int[] col1 = ColorTools.hex_to_rgb(Convert.ToInt32(generalConfig.color1,16));
        Color color1 = Color.FromArgb(col1[0], col1[1], col1[2]);
        int[] col2 = ColorTools.hex_to_rgb(Convert.ToInt32(generalConfig.color2, 16));
        Color color2 = Color.FromArgb(col2[0], col2[1], col2[2]);

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
                            logger.LogWarning("Invalid value argument for {}", key);
                            wid = 600;
                        }
                        break;
                    case "height":
                        if(!int.TryParse(value, out hei))
                        {
                            logger.LogWarning("Invalid value argument for {}", key);
                            hei = 450;
                        }
                        break;

                    case "color1":
                        if (!UInt32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint c1))
                        {
                            logger.LogWarning("Invalid value argument for {}", key);
                            color1 = Color.FromArgb(255, 255, 255);
                        }
                        else
                        {
                            int[] components1 = ColorTools.hex_to_rgb((int)c1);
                            color1 = Color.FromArgb(col1[0], col1[1], col1[2]);
                        }
                        break;
                    case "color2":
                        if (!UInt32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint c2))
                        {

                            logger.LogWarning("Invalid value argument for {}", key);
                            color2 = Color.FromArgb(0, 255, 0);
                            break;
                        }
                        else
                        {
                            int[] components2 = ColorTools.hex_to_rgb((int)c2);
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
                logger.LogWarning("Invalid argument format {}", key_val[0]);  
            }
        }

        // HDR image.
        FloatImage fi = new FloatImage(wid, hei, 3);

        //Linearly interpolates between 2 based on the pixel height in a circle.
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
                    fi.PutPixel(w, h, new float[3] { 255f, 255f, 255f });              
          }
        }

        //Save image based on name extension, if that is not given, save as pfm
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
        logger.LogInformation("Demo HDR image created");
        //Create scene, and generate the image;
        Scene scene = new Scene(config);
        /* 
         * Here methods of the scene can be called (the ones that are implemented), like adding an object, pointing the camera to an added object, 
         * or simply doing any transformation with the camera. The default scene set up is defined by the config file.
         */
        //scene.SwapObject(1);
        //scene.Object.Translate(new Vector3d(0,-35,0));
        FloatImage img=scene.SynthesizeImage();
        img.SavePFM("PathTrace.pfm");
        logger.LogInformation("Path traced image created");
  }
}
