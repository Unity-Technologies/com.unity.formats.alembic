# Exporting Unity GameObjects to Alembic

You have two different options to export Unity GameObjects to Alembic:
- The [Alembic Exporter component](#alembic-exporter-component) (available by default in the Alembic package).
- The [Alembic Clip Recorder](#alembic-recorder) (only available if you installed the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) package, minimum version: 2.0.0).

Both options support exporting single frame and multi-frame Alembic files, and can export GameObjects with the following components:

- Static Meshes ([**MeshRenderer**](https://docs.unity3d.com/Manual/class-MeshRenderer.html) component)
- Skinned Meshes ([**SkinnedMeshRenderer**](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html) component)
- Particles ([**ParticleSystem**](https://docs.unity3d.com/Manual/class-ParticleSystem.html) component)
- Cameras ([**Camera**](https://docs.unity3d.com/Manual/class-Camera.html) component)

>**Note:** The Alembic Exporter uses the Ogawa archive type to encode the exported file data.

<a name="alembic-exporter-component"></a>
## Using the Alembic Exporter component

The Alembic Exporter component is available by default in the _Alembic for Unity_ package.

To configure a Scene to export an Alembic file, add the [Alembic Exporter component](ref_Exporter.md) to a GameObject in the Scene. You can add it to an empty GameObject in the Scene; it is not necessary to add it to the GameObjects being exported.

You can configure the component to export the entire Scene, or individual GameObject hierarchies.

> ***Warning:*** Using the Alembic Exporter component automatically disables Draw Call Batching. Because of this, you might notice your Scene slowing down, because the elements are no longer static.

If the Mesh group is valid after being batched, then the Alembic package exports it. In some cases, Unity batches the data multiple times and the results may change.

To re-enable batching in your project after using the Exporter component, open the **Rendering** section of **Player settings** (from Unity's main menu: **Edit** > **Project Settings** > **Player** > **Other Settings** > **Rendering**). However, you should not re-enable Draw Call Batching while using the Alembic Exporter component.


<a name="alembic-recorder"></a>
## Using the Alembic Clip Recorder

The Alembic Clip Recorder is an extension of the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html). It becomes available as soon as you install both the _Alembic for Unity_ and _Unity Recorder_ packages.

The Alembic Clip Recorder includes the overall functionality of the Unity Recorder in addition to its own [Recorder properties](ref_Recorder.md):
* You can use the Alembic Clip Recorder as any other recorder, either from the Unity Recorder window or through a Recorder Clip in a [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html) track.
* You can use more than one recorder of any type at the same time to simultaneously capture different data.
* The ways to start and stop the Alembic Clip Recorder are the same as for any other recorder type.

To set up an Alembic Clip Recorder, you need to perform the following main steps:

1. When adding a new recorder (either through the Recorder window or a Recorder Clip within Timeline), select **Alembic Clip**.

2. Set the [Recorder properties](ref_Recorder.md) specific to the Alembic Clip Recorder.

3. Set the global Recording Properties (shared with the other recorders, such as the frames to record).

> **Note:** The [Unity Recorder documentation](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) provides the generic instructions to add a recorder and set up the global Recording Properties.
