using Util;
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
                    case "file_name":
                        fileName = value;
                        break;
                }
            }
            else
            {
                //Specifiy which parameter was wrong
                Logging.InvalidArgument();              
            }
        }
    
        // HDR image.
        FloatImage fi = new FloatImage(wid, hei, 3);

        // TODO: put anything interesting into the image.
        // TODO: use fi.PutPixel() function, pixel should be a float[3] array [R, G, B]
        for (int h = 0; h < hei; h++)
        {
          for (int w = 0; w <wid; w++)
          {
              fi.PutPixel(w,h,new float[3] { 100.3f,0.3f,0.3f});
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
