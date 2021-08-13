# Importing a local Alembic file through your project folder

This is the default recommended import method. Use it when you can have a copy of the Alembic source file in a folder within your Unity project, for example:
* To import smaller assets.
* To import assets in a context of individual work that does not involve team collaboration.

## Importing the file

To import an Alembic file through your Unity project folder:

1. From your computer file system, drag the Alembic (`.abc`) file to any folder of your Project view.
   <br />This creates a separate copy of your original file in your Unity project.

2. In the Project view, select the Alembic file you just copied.

3. In the Inspector, adjust the properties of the [Alembic Importer window](ref_Importer.md) to customize the import process.

4. Drag the Alembic file from the Project window to the Hierarchy.

## Applying Materials

The Alembic format doesn't support any Material data.

By default, to guarantee an acceptable basic display of the imported Alembic asset, Unity automatically assigns all of its submeshes the **Default Material** corresponding to your project's render pipeline.

However, once you imported the file, you can [override the default material assignation](materials.md) for all submeshes, either manually or automatically based on the original Face Set names, directly from the Alembic Importer window.

## Editing the import options

You can adjust the import options after you added the Alembic asset to your Scene.

To do so:

1. In the Project window, select the Alembic file.

2. In the Inspector, edit the properties of the [Alembic Importer window](ref_Importer.md).

3. Select **Apply** at the bottom of the window, or select **Revert** if you actually want to abandon your changes.

>**Note:** If these buttons are inactive, it means that the Alembic Importer currently uses the import options as seen in the window.
