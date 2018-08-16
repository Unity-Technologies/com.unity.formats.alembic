Alembic for Unity Public API
===
# AlembicRecorder Class
|Methods|  |
|:---|:---| 
|__BeginRecording__  | Starts recording with the Alembic recorder. |
|__EndRecording__ | Stops recording with the Alembic recorder. |
|__ProcessRecording__ | Processes the currently buffered frame during the recording process. |
|__Dispose__ |Discards the recorder buffer data.|

# AlembicRecorderSettings Class
|Properties| Type |  |
|:---|:---|:---|
|__outputPath__  |string| Output path of the recorder; defaults to Your Project/Output/Output.abc. |
|__AssumeNonSkinnedMeshesAreConstant__ |bool| Assumes that meshes without a skinned mesh renderer will not toggle on or off during the recording process; reduces the filesize of your output file. |
|__CaptureMeshRenderer__ |bool| Toggle to enable or disable the capture of mesh renderers. |
|__CaptureCamera__ |bool|Toggle to enable or disable the capture of cameras. |
|__MeshSubmeshes__ |bool|Toggle to enable or disable the capture of submeshes. Scens that contain meshes with with multiple materials should have this checked, to maintain proper export of material IDs.|
|__conf__ |struct|Access the aeConfig struct to further customize the recorder settings via code.|

# aeConfig Struct
|Properties| Type |  |
|:---|:---|:---|
|__archiveType__  |aeArchiveType| Enum to select the Alembic archive type your file will be recorded as. |
|__frameRate__ |int| Framerate of your recorded file. |
|__swapHandedness__ |bool|Toggle to swap the handedness of the transform matrix of your file. Enabled by default to maintain transform fidelity. |
|__scaleFactor__ |float|Scale the size of your export. |

# aeArchiveType enum
|Properties|  |
|:---|:---|
|__Ogawa__  | Default archive type; offers smaller filesize and better runtime performance. |
|__HDF5__ | Legacy archive type. |

# Bool Class
This class emulates C# booleans as a byte to ease passing and receiving C++ Code.
|Methods|Argument |  |
|:---|:---|:---| 
|__ToBool__  |bool| Converts a C# bool to a byte Bool |
|__ToBool__ |Bool| Converts a byte Bool to a C# bool. |
