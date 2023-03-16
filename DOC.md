# Photorealistic-Graphics-Summer-semester-2023
---
The ray tracer implementation for the course Photorealistic Graphics at Charles University summer sememster 2023 by Hegyi Gáspár András. Solution is in the src/004 folder. 

# Basic functionality, and usage
---
The program generates an image with a circle inside with the diameter of the size of the shorter side, and interpolates between 2 colors inside 
the circle with the pixel height.  

It generates another image based on path tracing, which is saved as PathTrace.pfm . The initial image is based on the config file. It contains 
5 planes, 3 spheres, a perspective camera set at [0,3,15], and looking at the origin, and 2 point light sources.

Right now modification of the scene is only possible through the config file, I plan to make modification available through
the command line later.  

## The config file
The config file is a json file, its contents are serialized into classes that have the same properties as in the
config file.  

__GeneralConfig__: The general config contains values for the demo pfm image, these are the two colors it interpolates through,
and the name of the saved file.
  
__CameraConfig__: The camera config allows defining camera attributes. These are the position of 
the camera in the world, and a target the camera os looking at. These two parametrs define the camera's orientation
in the scene. The last parameter is the field of view.  

__SceneConfig__: The scene config first declares the up direction in the world (this might be moved to the code as a constant scnen property, 
sonce there is no need to change this). Next up is the definition of the objects that are making up the scene.
Objects are the solids in the scene, and they can be put under the "Objects" entry. Currently there are two 
solids implemented, sphere, and infinite plane. Since both have different parameters that define them, they 
can be found under different entries under the object. Below is an example of putting one sphere, and one plane in the scene. 

```json
"Objects": {
      "Spheres": {
        "Number": 1,
        "Positions": [
          [ 0, 1, 0 ],
        ],
        "Colors": [
          [ 20, 20, 120 ],
        ],
        "Materials": [
          [ 0.2, 0.8, 0.2, 100 ],
        ],
        "Radiuses": [1]
      },
      "Planes": {
        "Number": 1,
        "Normals": [
          [ 0, 0, 1 ],
        ],
        "Positions": [
          [ 0, 0, -4 ],
        ],
        "Colors": [
          [ 230, 230, 230 ],
        ],
        "Materials": [
          [ 0.5, 0.95, 0.5, 150 ],
        ]
      }
    }, 
```
The number defines how many of the defined objects will be displayed. If the number is not the same as the positions, a warning is 
logged, in case leaving out objects was not our intention. Most of the attributes will be descibed later in more detail. Right now the only available material is
phong reflection model. The way materials are assigned to an object will be modified when the  Another oject can be added, if we increase the number, and add the next object's attributes to the corresponding fields.  

Last the light configurations are added. The ambient lighting with 3 values in a list, the positions of the point lights, and the 
directions of the directional lights.
```json
    "AmbientLighting": [ 1, 1, 1 ],
    "PointLightPositions": [
      [ 5, 5, 0 ],
      [-5,5,0]
    ],
    "DirectionalLightDirections": []
```
__PlaneConfig__: The plane config has only 2 fields, which are the width and height.  
## Arguments 
The program currently accepts 5 arguments, which are the following:
- width: int
- height: int
- filename: string
- start color: hex, First color in the interpolation
- max color: hex, Second color in the interpolation

Command line arguments are given values with the "=" sign.  
An example command-line argument can be the following:  width=600 height=450 file_name=demo.pfm color1=000000 color2=FFFFFF


# Architecture
---
Here is a brief description of how the command line application works. Besides I made a doxygen documentation for the classes, it will be uploaded when I finish all the XML commenting.
 ## Util classes
 ## Main components

# Future plans, and some faulty behaviours.
 
