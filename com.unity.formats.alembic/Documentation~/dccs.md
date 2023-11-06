# Creating Alembic files in Autodesk® Digital Content Creation applications

Many Digital Content Creation (DCC) applications allow you to export models as Alembic (­­­­.abc­­­­) files. Depending on your asset roundtrip scenario, you might need to export your Alembic files from your DCC application with additional data so that Unity can access and properly interpret this data at import.

The table below gives high-level instructions according to the type of data you would need to export from either Autodesk® Maya® or Autodesk® 3ds Max® and use in Unity.

| Data to export | Autodesk® Maya® | Autodesk® 3ds Max® | Typical use case |
| :--- | :--- | :--- | :--- |
| Face Sets | On export, enable **Write Face Sets** | In the **Export Data** section, enable **Material IDs** | When Unity imports an Alembic mesh that contains face sets, it creates a sub-mesh for every face set. This allows you to apply a unique material for every face set/sub-mesh instead of one material for the whole object.<br /><br />**Note:** You need face sets to use the [Alembic Material Remapper](materials.md#automatic-re-mapping-based-on-face-set-names). |
| Color Sets | On export, enable **Write Color Sets** | In the **Export Data** section, enable **Vertex Colors** | Vertex Colors allow you to define a specific color for each vertex of a mesh. During rendering, Unity uses this color as the surface color of the object. |
| UV Sets | On export, enable **Write UV Sets** | In the **Export Data** section, enable **UVs** | UVs are used to map textures onto polygon meshes. They define what portion of a texture should be applied to which polygon of a mesh. |

>[!NOTE]
>There is currently no way to export an Alembic file that includes materials.
