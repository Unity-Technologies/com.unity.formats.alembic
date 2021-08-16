# Limitations and known issues

This page lists some known issues and limitations that you might experience with Alembic for Unity. It also gives basic instructions to help you work around them.

#### Limited support of build targets

**Limitation:** Alembic for Unity **only supports** the following build targets: **Windows 64-bit**, **MacOS X**, **Linux 64-bit**, and **Stadia**.

#### Material import/export not supported

**Limitation:** The Alembic format does not support Material import and export.

**Workaround:** The Alembic Importer includes a tool to [automatically remap materials](materials.md) on imported Alembic assets based on Face Set names.

#### Non-convex polygon import issues

**Known issue:** Importing meshes with non-convex polygons results in malformed geometry (for example, triangles with flipped normals).

#### Build issue due to Alembic Prefab with nested FBX GameObject

**Known issue:** Adding an FBX GameObject as a child of an Alembic Prefab prevents you from building your project.

**Workaround:** Instead of having the FBX GameObject nested in the Alembic Prefab, create a new Prefab that contains both the Alembic Prefab and the FBX GameObject as distinct children.

#### Alembic Stream Player reference broken after moving the Alembic source file

**Known issue:** If you move the source file of an imported Alembic asset to a different subfolder of your project through an external file management interface (for example, your workstation file system), the Alembic Stream Player reference to this file is broken. This happens even if you move the `.meta` file along with the source file.

**Workaround:** You should always use the Unity Editor Project window to move files within your project. If you absolutely need to use an external file management interface to move an Alembic asset, you need to fully re-import the asset in Unity.

#### No change rendered in Game view on Vertex Motion Scale value change

**Known Issue:** When you change the value of the **Vertex Motion Scale** property in the [Alembic Stream Player](ref_StreamPlayer.md) component, Unity doesn't automatically display the expected motion blur effect change in the Game view.

**Workaround:** To see the actual motion blur result when you change the **Vertex Motion Scale** property value, you need to slightly scrub through the Alembic asset time range, either via the Alembic Stream Player or via the Timeline window.
