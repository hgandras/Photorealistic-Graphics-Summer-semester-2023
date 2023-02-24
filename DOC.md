# Photorealistic-Graphics-Summer-semester-2023
The ray tracer implementation for the course Photorealistic Graphics at Charles University summer sememster 2023 by Hegyi Gáspár András. Solution is in the src/004 folder. 

# Current functionality
The program generates an image with a circle inside with the diameter of the size of the shorter side, and interpolates between 2 colors inside 
the circle with the pixel height.

# Arguments 
The program currently accepts 5 arguments, which are the following:
- width: int
- height: int
- filename: string
- start color: hex, First color in the interpolation
- max color: hex, Second color in the interpolation

Command line arguments are given values with the "=" sign.  
An example command-line argument can be the following:  width=600 height=450 file_name=demo.pfm color1=000000 color2=FFFFFF
# Config
The config file is a json file, which is currently accessed by the GeneralConfig and the CameraConfig classes. In the config file however 
the colors need the 0x prefix before their hex value.

# Architecture
 ## Util classes
 ## Main components
 
