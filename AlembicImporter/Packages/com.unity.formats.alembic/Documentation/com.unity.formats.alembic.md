# About the Alembic package

Use the Alembic package to import [Alembic](http://www.alembic.io/) files into your Unity scenes. This lets you bring in vertex cache data, for example facial animation (skinning) and cloth simulation (dynamics), from other software packages and have it playback exactly the same way in Unity.

The Alembic package supports the import and playback of Meshes, Points and Cameras.

## Requirements

The Alembic Package is compatible with Unity Editor 2018.1 and above.

The package is available on 64-bit desktop platforms:
* Windows 10
* macOS Sierra (10.12)
* GNU/Linux (Centos 7, Ubuntu 16.x and Ubuntu 17.x)

## Known Limitations

* There is no exposed public API in the Alembic package.

We welcome hearing about your experience on [this forum thread](https://forum.unity.com/threads/alembic-for-unity.521649/).

# Quick start

This is a quick guide to add Alembic assets to your project. By the end you will know how to import and playback Alembic content.

To import Alembic content drag an ABC file into the project view. This will import the ABC asset.

![Drag the file](images/drag-to-project.png)

Then drag the ABC asset into the scene and scrub the Time property on the component.

![Scrub the time](images/scrub-time.png)

To animate using Timeline Editor, can create a Timeline. You can  animate the time property directly as shown below. 

![Timeline](images/timeline.png)

Alternatively, you can drag and drop the scene object with the ABC asset onto an empty area within Timeline's Clip Editor this will create an Alembic Track with an Alembic Clip. If you are starting from an empty Timeline you may need to manually create the Alembic track first by right clicking the Timeline's Track view on the left hand side of the editor.

![Timeline](images/timeline2.png)

# Importing Alembic Files

When you put ABC files in the Assets folder under your Unity Project, Unity automatically imports and stores them as Unity Assets. To view the import settings in the Inspector, click on the file in the Project window. You can customize how Unity imports the selected file by setting the properties on this window:

![The Import Settings window](images/abc_import_options.png)

| Property:| Function: |
|:---|:---| 
|__Normals__  <br/>"Read From File",<br/>"Compute If Missing",<br/> "Always Compute",<br/> "Ignore" |Defines if the normals from .abc file are used or whether they are calculate based on vertex position. The default "Compute If Missing" will use abc file normals, otherwise they will be calculated. |
|__Tangents__ <br/> "Compute" <br/> "None"| Defines if the tangents are computed and is enabled by default. Since the ABC file has no tangent data, there are only 2 choices. However, please note that the calculation of tangents requires normals and UV data, and if these are missing the tangent cannot be computed.  Please note that the calculation of tangents is expensive, so if not require then disabling of this option will increase the speed of playback.|
|__Camera Aspect Ratio__ <br/> "Camera Aperture",<br/>"Default Resolution",<br/>"Current Resolution"| Defines whether to set the Unity Camera's aspect ratio. By default the ABC file will set the camera aspect ratio. The Alembic Importer uses the default resolution from the Player Settings. The Current Resolution refers to the aspect ratio defined by the screen. |
|__Scale Factor__ | Sets the scale factor to scale the points and velocities by |
|__Swap Handedness__ | Choose swap handedness to invert the X direction |
|__Interpolate Samples__ | Define whether to interpolate animation. If enabled then the animation will be interpolated for Transform, Camera, and Mesh where the topology does not change such that the number of vertices and indices are immutable.<br/><br/>If Interpolate Samples is enabled, or velocity data is included in the .abc file, you can pass velocity data to an Alembic shader. 
|__Swap Face Winding__ | Choose swap face winding to invert the orientation of the polygon. |
|__Turn Quad Edges__ | Choose turn quad edge to invert the arrangement of the triangles when dividing the quadrilateral polygon into triangles.|
|__Import Xform__ |Choose whether to import Transform data.|
|__Import Camera__ |Choose whether to import Camera data.|
|__Import Poly Mesh__ |Choose whether to import Mesh data.|
|__Import Points__ |Choose whether to import Point data.|
|__Time Range__ |Defines the frame start and frame end for the Alembic animation.|

# Exporting Alembic Files

The Alembic exporter supports exporting single frame and multi-frame Alembic files and can export GameObjects with the following components:
* MeshRenderer
* SkinnedMeshRenderer
* ParticleSystem
* Camera

To configure a scene to export an ABC file, add the AlembicExporter component to a GameObject in the scene. It does not need to be added to the objects being exported, but can be added to an empty object for example.
The component can be configured to export the entire scene or individual object hierarchies.

Using the AlembicExporter component automatically disable Draw Call Batching. If the Mesh group is valid after being batched then it the  will be exported, in some cases the data will be batched multiplied times and the results may change.  If you want to control the Batch settings they can be found in the Rendering section of Player Settings.

The Alembic exporter can be customized by setting the properties on this component:

![The Export Settings window](images/abc_export_options.png)

| Property:| Function: |
|:---|:---| 
|__Output Path__ |Specify the location where the Alembic Exporter will save the ABC file. By default the output path is relative to the current Unity project path. |
|__Archive Type__ |Choose the Alembic format specification, the default is Ogawa and provides smaller and better performance than HDF5.|
|__Xform Type__ |Choose the transform type. The default is TRS and records the TRS channels for position, rotation, scale of an object. The alternative is matrix (Matrix).|
|__Time Sampling Type__ |Choose between Uniform and Acyclic time sampling. Uniform time sampling will |
|__Time Sampling Frame Rate__ |{TODO}|
|__Time Sampling Fix Delta Time__ |{TODO}|
|__Swap Handedness__ |Choose swap handedness to change from a left hand coordinate system (Unity) to a right hand coordinate system (Maya).|
|__Swap Faces__ |Choose swap faces to reverse the front and back of a face.|
|__Swap Factor__ |Choose swap factor to convert system units, for example 0.1, converts it to 1/10 size. This also affects position and speed.|
|__Capture Scope__<br/>"Entire Scene",<br/>"Target Branch" | Choose the scope of the export. By default the entire scene will be exported but it can be configured to export just a branch of the scene. ![Alembic Export Target](images/abc_export_target.png) |
|__Assume None Skinned Meshes Are Constant__ |{TODO}|
|__Capture MeshRenderer__ |{TODO}|
|__Capture SkinnedMeshRenderer__ |{TODO}|
|__Capture Particle__ |{TODO}|
|__Capture Camera__ |{TODO}|
|__Mesh Components Normals__ |{TODO}|
|__Mesh Components UV1__ |{TODO}|
|__Mesh Components UV2__ |{TODO}|
|__Mesh Components Vertex Color__ |{TODO}|
|__Mesh Components Submeshes__ |{TODO}|
|__Capture On Start__ |{TODO}|
|__Ignore First Frame__ |{TODO}|
|__Max Capture Frame__ |{TODO}|
|__Detailed Log__ |{TODO}|
|__Begin Recording__ |{TODO}|
|__One Shot__|Button to export the current frame to the ABC file.|

# Controlling Alembic playback

The import and playback of Alembic data is controlled by the `Alembic Stream Player` component.

If you change the Time parameter you can see that Mesh animate. To play the animation this parameter can be controlled from the Timeline, Animator component or via scripts.

Vertex Motion Scale is a magnification factor when calculating velocity. The greater the velocity and motion scale, the more blurring will be applied by the MotionBlur post processing effect.

You can customized import and playback through the properties on this component:

![The Stream Player Settings window](images/abc_stream_player.png)

| Property:| Function: |
|:---|:---| 
|__Time Range__ | {TODO}|
|__seconds__ | {TODO}|
|__Time__ | {TODO}|
|__Vertex Motion Scale__ | {TODO}|
|__Ignore Visibility__ |{TODO} |
|__Async Load__ |{TODO} |
|__Recreate Missing Nodes__ |{TODO} |

> ***Note:*** Please note that copies of .abc files are created under `Assets / StreamingAssets`. This is necessary for streaming data since it requires that the .abc file remain after building the project.

# Using Timeline: Working with Alembic

The Timeline can be used to playback and record Alembic animation including:

* Playback of Alembic animation by controlling the `Alembic Stream Player`
* Create sequences using Alembic clips with the ability to trim times and adjust clip-ins
* Record Alembic data directly from the Timeline to an ABC files

## Record and playback the Alembic using an `Infinite Clip`

You can control the playback of Alembic using an `Infinite Clip` on a Timeline `Animation Track` bound to the game object with the Alembic Stream Player component. In the recording mode any animatable parameters that are changed will be recorded to as an animation source. This infinite clip can then be converted into Animation Clip which can then be used with the object's Animation State Machine.

![Controlling Stream Player With Infinite Clip](images/abc_infinite_clip.png)

## Playback using `Alembic Shot` clips

![Alembic Shot Clip](images/abc_shot_clip.gif)

You can playback Alembic as a `Alembic Shot` on a `Alembic Track`. To create an Alembic Shot drag an scene object with an Alembic Stream Player componet on the Clips view portion of the Timeline Editor. If the Timeline editor is empty create an temporary track so that you can see the Clips view portion.

![Alembic Clip Editor](images/abc_clip_editor.png)

## Recording with the `Alembic Recorder` clip

You can record to Alembic ABC files using the Alembic Recorder Clip. The following types of components can be recorded:
* Static Meshes (MeshRenderer)
* Skinned Meshes (SkinnedMeshRenderer)
* Particle (ParticleSystem)
* Cameras (Camera)

To configure the Timeline to record an object to Alembic you need to define the scope of the recording. By default the entire scene will be recorded but you can scope it to a object by setting the object as a Scope Target by setting the properties on Alembic Recorder Clip component:

![Alembic Shot Clip](images/abc_recorder_clip.png)

| Property:| Function: |
|:---|:---| 
|__Output Path__ |{TODO}|
|__Archive Type__ |{TODO} |
|__Xform Type__ |{TODO} |
|__Swap Handedness__ |{TODO} |
|__Swap Faces__ |{TODO} |
|__Swap Factor__ |{TODO} |
|__Capture Scope__ |{TODO} |
|__Assume None Skinned Meshes Are Constant__ |{TODO} |
|__Capture MeshRenderer__ |{TODO} |
|__Capture SkinnedMeshRenderer__ |{TODO} |
|__Capture Particle__ |{TODO} |
|__Capture Camera__ |{TODO} |
|__Mesh Components Normals__ |{TODO} |
|__Mesh Components UV1__ |{TODO} |
|__Mesh Components UV2__ |{TODO} |
|__Mesh Components Vertex Color__ |{TODO} |
|__Mesh Components Submeshes__ |{TODO} |
|__Ignore First Frame__ |{TODO} |
|__Detailed Log__ |{TODO} |

The effect the recording press "Play". The ABC file will be recorded to the output path which by default is outside the Project Asset folder. You can then bring the Alembic file back into the Project and play it back using the Timeline.

![Alembic Shot Clip](images/abc_recorded_clip.png)

<a name="materials"></a>
# Working with Materials

## Assigning Materials on Import

By default the Alembic will import Meshes using the `Default Material`. So, you will need to manually reassign your Materials for each object.

The Alembic package does not support remapping Face Set names to Materials or creating Materials from Face Set names.

## Alembic Materials

The Alembic package includes the following Materials:

| Property:                     | Function: |
|:------------------------------|:----------| 
|__Overlay__                    |{TODO} |
|__Points Standard__            |{TODO} |
|__Points Transparent__         |{TODO} |
|__Points Motion Vectors__      |{TODO} |
|__Standard__                   |Standard PBR material with motionblur support added |
|__Standard (Roughness setup)__ |Standard (Roughness setup) PBR with roughness material with motionblur support added |
|__Standard__(Specular setup)   |Standard (Specular setup) material with motionblur support added |

## Motion Blur

The Alembic shaders included add motion vector generation. This is useful for rendering that requires motion vectors, such as the post processing effect MotionBlur. If you want to add the motion vector generation function to your own shader, add the line `UsePass "Hidden / Alembic / MotionVectors / MOTIONVECTORS"` into SubShader. Please see AlembicMotionVectors.cginc for details. Since the velocity data is passed to the fourth UV, the apex position of the previous frame is calculated based on it. Left is unprocessed, right is output of motion vector and MotionBlur applied by the Post Processing Stack.<br/>![Alembic MotionBlur](images/abc_motionblur.png)|

<a name="dccs"></a>
# Working with Autodesk Maya® 

Maya's shading group can be imported as submesh. Maya needs to export with "Write Face Sets" option enabled. Please note that this option is off by default.

The Alembic import supports Maya's vertex color and multiple UV sets. It is necessary to export from Maya by setting "Write Color Sets" and "Write UV Sets" option. Please note these are off by default.

To export from Autodesk Maya® with materials and vertex colors you will need the following highlighted Alembic export settings:

![Autodesk Maya Alembic Settings](images/abc_maya_options.png)|
