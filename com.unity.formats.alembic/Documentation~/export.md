# Exporting Unity GameObjects to Alembic

Using alembic to export objects out of Unity is useful when you want to bake data that is not key-frame animated. This allows you to get deterministic data that you can re-use in other Digital Content Creation Softwares (DCCs) or in Unity itself.

For example, if you have a physics simulation that does not behave exactly the same every time you run it, baking it to alembic ensures it will always behave the same. Similarly, you might want to bake the properties/position/orientation of a Cinemachine camera if you want to re-use it in another DCC.

## Export methods

There are two ways to export Unity GameObjects to Alembic:

- Via an [Alembic Exporter component](export-abc-exporter.md) (available by default in the Alembic package).
- Via an [Alembic Clip Recorder](export-abc-clip-recorder.md) (available if you install the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) package, minimum version: 2.0.0).

## Export capabilities

Both methods support exporting single frame and multi-frame Alembic files, and can export GameObjects with the following components:

- Static Meshes ([**MeshRenderer**](https://docs.unity3d.com/Manual/class-MeshRenderer.html) component)
- Skinned Meshes ([**SkinnedMeshRenderer**](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html) component)
- Particles ([**ParticleSystem**](https://docs.unity3d.com/Manual/class-ParticleSystem.html) component)
- Cameras ([**Camera**](https://docs.unity3d.com/Manual/class-Camera.html) component)

>[!NOTE]
>The Alembic Exporter uses the Ogawa archive type to encode the exported file data.
