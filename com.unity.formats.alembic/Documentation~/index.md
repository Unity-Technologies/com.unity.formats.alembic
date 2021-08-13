# Alembic for Unity

Use the Alembic package to import and export [Alembic](http://www.alembic.io/) files into your Unity Scenes, where you can playback and record animation directly in Unity. The Alembic format bakes animation data into a file so that you can stream the animation in Unity directly from the file. This saves a significant amount of resources, because the modeling and animating does not happen directly inside Unity.

The Alembic package brings in vertex cache data from a 3D modeling software, such as facial animation (skinning) and cloth simulation (dynamics). When you play it back inside Unity, it looks exactly the same as it did in the 3D modeling software.

>**Important:** before you start using this package, you must be aware of its [limitations and known issues](known-issues.md).


## Features

The Alembic package supports these features:

* [Importing](import.md) data from Meshes, Particle Cloud Points, Curves, and Cameras.

* [Material automatic remapping](materials.md) on imported Alembic assets based on Face Set names.

* Applying [Alembic Shaders](motion-vectors.md#shaders) and [Motion Blur](motion-vectors.md#blur) effects.

* Customizing [particle and point cloud effects](particles.md).

* Playing animation by streaming data through [Timeline](timeline.md) or [Unity Animation](animClip.md).

* Playing Alembic animation [using imported animation clips](time_ImportedClip.md).

* [Exporting](export.md) Unity GameObjects to an Alembic file (through Exporter or Recorder).

> **Note:** If you need to use the Alembic Clip Recorder feature, you must also install the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) package (minimum version: 2.0.0).


## Package technical details

### Installation

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Manual/upm-ui-install.html).

### Requirements

This version of Alembic for Unity is compatible with the following versions of the Unity Editor:

* 2019.3 and later (recommended)

### Limitations and known issues

* Alembic for Unity **only supports** the following build targets: **Windows 64-bit**, **MacOS X**, **Linux 64-bit**, and **Stadia**.

See the full list of [limitations and known issues](known-issues.md), which also provides workarounds in some cases.


## Feedback

Tell Unity about your experience using Alembic for Unity on [the Alembic-For-Unity forum](https://forum.unity.com/threads/alembic-for-unity.521649/).
