# Exporting Unity GameObjects to Alembic

The Alembic exporter supports exporting single frame and multi-frame Alembic files and can export GameObjects with the following components:

- Static Meshes (**MeshRenderer** component)
- Skinned Meshes (**SkinnedMeshRenderer** component)
- Particles (**ParticleSystem** component)
- Cameras (**Camera** component)

To configure a scene to export an Alembic file, add the [Alembic Exporter component](ref_Exporter.html) to a GameObject in the scene. You can add it to an empty object in the scene; it is not necessary to add it to the objects being exported. 

You can configure the component to export the entire scene or individual object hierarchies.

> ***Warning:*** Using the Alembic Exporter component automatically disables Draw Call Batching. Because of this, you may notice your Scene slowing down, since the elements are no longer static. 

If the Mesh group is valid after being batched then the Alembic package exports it. In some cases the data is batched multiple times and the results may change.  

If you want to control the Batch settings, open the Rendering section of Player settings (from Unity's main menu: **Edit** > **Project Settings** > **Player** > **Other Settings** > **Rendering**). However, it is not recommended to re-enable Draw Call Batching while using the Alembic Exporter component.

