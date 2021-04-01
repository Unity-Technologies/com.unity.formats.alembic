# Importing Alembic files

There are two ways to import Alembic files in Unity, depending on the way you can or want to handle their sources:

* [Through your Unity project folder](import-file-local.md)

  This is the default recommended import method.
  <br />Use it when you can have a copy of the Alembic source file in a folder within your Unity project. For example, To import smaller assets or any assets that you are using in a context of individual work that does not involve team collaboration.

* [From outside your Unity project folder](import-file-external.md)

  Use this method when you cannot or do not want to manage the Alembic source file through a copy of a folder of your Unity project. For example, to import very large assets, or any assets that you are using in a context of collaborative work that involves multiple teams and shared storage locations.

>**Note:** The Alembic Importer can only import Alembic files encoded with the Ogawa archive type. It does not support HDF5.
