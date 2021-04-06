# Importing an external Alembic file from outside your project folder

Use this method when you cannot or do not want to manage the Alembic source file through a copy of a folder of your Unity project, for example:
* To import very large assets.
* To import assets in a context of collaborative work that involves multiple teams and shared storage locations.

>**Note:** If you can afford managing a copy of the Alembic inside your project folder, you should preferably use the [default recommended import method](import-file-local.md).

## Importing the file

To import an Alembic file from outside your Unity project:

1. In the Hierarchy, add a new empty GameObject and keep it selected.

2. In the Inspector, add an **Alembic Stream Player** component to the GameObject you just created.

3. Use the browse button next to the **Alembic File** field to select the Alembic (`.abc`) file to import, or directly type the path in the field.
   >**Note:** You can only select a file from any local folder or any mapped network drive available through your file system. You cannot target a file through a direct network path or an FTP/HTTP link.

4. Adjust the properties of the [Alembic Stream Player component](ref_StreamPlayer.md#alembic-asset-located-outside-your-project) to customize the import process.

## Applying Materials

The Alembic format does not support any Material data. However, once you imported the file, you can [reassign the Default Material](matshad.md#materials) to a custom Material.

The Alembic for Unity package also provides you with several [Shaders](matshad.md#shaders) specifically designed for Alembic data.

## Editing the import options

You can adjust the import options after you linked the external Alembic file to your project.

To do so, edit the properties of the [Alembic Stream Player component](ref_StreamPlayer.md#alembic-asset-located-outside-your-project). The Alembic importer automatically applies them to the imported asset.

## Synchronizing the Alembic nodes

The Alembic Stream Player does not automatically synchronize the GameObject hierarchy of the imported asset with the node structure of the external Alembic file. If changes occur in the hierarchy of the external file, for example due to collaborative work, you must manually perform the synchronization on your imported assed.

To do so, select the GameObject that you used to import the Alembic asset, and in the Inspector, in the [Alembic Stream Player component](ref_StreamPlayer.md#alembic-asset-located-outside-your-project), use the following buttons:
* **Create Missing GameObjects** locally creates any GameObjects that would be missing according to the actual node hierarchy of the external file.
* **Remove Unused GameObjects** locally removes any GameObjects that would not correspond to an actual node of the external file.

>**Note:** If your imported Alembic asset is part of a Prefab, you should perform these actions directly from the Prefab asset located in your Project window rather than from any Prefab instance of your Scene, in order to avoid any unexpected issues with Prefab overrides.
