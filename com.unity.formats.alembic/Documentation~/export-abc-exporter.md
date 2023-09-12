# Using the Alembic Exporter component

One way to export alembics out of Unity is to use the Alembic Exporter Component, which is available by default in the _Alembic for Unity_ package.

To configure a Scene to export an Alembic file, you need to perform the following main steps:

1. Add the [Alembic Exporter component](ref_Exporter.md) to a GameObject in the Scene. You can add it to an empty GameObject in the Scene; it is not necessary to add it to the GameObjects being exported.

2. Configure the component to export the entire Scene or individual GameObject hierarchies.

3. Enter Play Mode to start the recording (see [this section](ref_Exporter.md#exportRef_H) to learn how to control the recording).

## Draw Call Batching

> ***Warning:*** Using the Alembic Exporter component automatically disables Draw Call Batching. Because of this, you might notice your Scene slowing down, because the elements are no longer static.

If the target Mesh group is valid after being batched, then the Alembic package exports it. In some cases, Unity batches the data multiple times and the results may change.

To re-enable batching in your project after using the Exporter component, open the **Rendering** section of **Player settings** (from Unity's main menu: **Edit** > **Project Settings** > **Player** > **Other Settings** > **Rendering**). However, you should not re-enable Draw Call Batching while using the Alembic Exporter component.
