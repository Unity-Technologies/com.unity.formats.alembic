# Exporting Unity GameObjects to Alembic

The Alembic exporter supports exporting single frame and multi-frame Alembic files, and can export GameObjects with the following components:

- Static Meshes ([**MeshRenderer**](https://docs.unity3d.com/Manual/class-MeshRenderer.html) component)
- Skinned Meshes ([**SkinnedMeshRenderer**](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html) component)
- Particles ([**ParticleSystem**](https://docs.unity3d.com/Manual/class-ParticleSystem.html) component)
- Cameras ([**Camera**](https://docs.unity3d.com/Manual/class-Camera.html) component)

To configure a Scene to export an Alembic file, add the [Alembic Exporter component](ref_Exporter.md) to a GameObject in the Scene. You can add it to an empty GameObject in the Scene; it is not necessary to add it to the GameObjects being exported.

You can configure the component to export the entire Scene, or individual GameObject hierarchies.

> ***Warning:*** Using the Alembic Exporter component automatically disables Draw Call Batching. Because of this, you might notice your Scene slowing down, because the elements are no longer static.

If the Mesh group is valid after being batched, then the Alembic package exports it. In some cases, Unity batches the data multiple times and the results may change.

To re-enable batching in your project after using the Exporter component, open the **Rendering** section of **Player settings** (from Unity's main menu: **Edit** > **Project Settings** > **Player** > **Other Settings** > **Rendering**). However, you should not re-enable Draw Call Batching while using the Alembic Exporter component.
