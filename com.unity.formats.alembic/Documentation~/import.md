# Importing Alembic files

You can import Alembic files like [any other Asset](https://docs.unity3d.com/Manual/ImportingAssets.html):

1. Drag your .abc file into your Project view.
2. Select the file in the Project view and open the Inspector view.
3. Customize how you want Unity to import the file. To do this, adjust the [options on the Alembic Import Settings window](ref_Importer.md).
4. Drag your file from your Project into your Scene.

Once imported, you might also want to [reassign the Default Material](matshad.md#materials) to a custom Material, because the Alembic format does not support any Material data.

In addition, the Alembic package provides a number of [Shaders](matshad.md#shaders) that are customized for Alembic data.
