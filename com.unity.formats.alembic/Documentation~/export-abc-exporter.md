# Export via an Alembic Exporter component

Export a Unity GameObject as an Alembic file using an Alembic Exporter Component.

>[!NOTE]
>The Alembic Exporter Component is available by default in the _Alembic for Unity_ package.

## Setup guidelines

To configure a Scene to export an Alembic file:

1. Add an **Alembic Exporter component** to a GameObject in the Scene.<br />
   You can add it to an empty GameObject in the Scene, it is not necessary to add it to the GameObjects you need to export.

   ![The Export Settings window](images/abc_export_options.png)

2. Configure the component to export the entire Scene or individual GameObject hierarchies.

3. Set the [Alembic Exporter Component properties](ref_Exporter.md) according to your output needs:

   * In **Capture Settings**, specify the targeted export scope and the type of data you want to export.

   * In **Alembic Settings**, specify the ways to output the data as an Alembic file.

   * In **Output Path**, specify the name and location of the file to save the data to.

3. Set up the [capture options](ref_Exporter.md#exportRef_F) and review the descriptions of the [Capture Control buttons](ref_Exporter.md#exportRef_H) to get a capture timing strategy that fits your export needs.

4. Enter Play mode to start the export.

## Draw Call Batching

>[!WARNING]
>Using the Alembic Exporter component automatically disables Draw Call Batching. Because of this, you might notice your Scene slowing down, because the elements are no longer static.

If the target Mesh group is valid after being batched, then the Alembic package exports it. In some cases, Unity batches the data multiple times and the results may change.

To re-enable batching in your project after using the Exporter component, open the **Rendering** section of **Player settings** (from Unity's main menu: **Edit** > **Project Settings** > **Player** > **Other Settings** > **Rendering**). However, you should not re-enable Draw Call Batching while using the Alembic Exporter component.
