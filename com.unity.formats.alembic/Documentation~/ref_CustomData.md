# Alembic Custom Data component

The Alembic Custom Data component displays additional custom information stored with each Face Set. A Face Set is a sub-group of faces that compose an object.

To access the Custom Data component, in the Hierarchy, within an imported Alembic asset instance, select a GameObject that corresponds to an Alembic node that contains custom data.

> [!NOTE]
> The Alembic importer adds this component automatically and exclusively when it detects custom data in a node of the imported Alembic asset.

![Alembic Custom Data component options](images/abc_custom_data.png)

| ***Property*** | ***Description*** |
|:---|:---|
| **Script**        | The script that defines this component. You cannot modify this property. |
| **Face Set Names** | Names of the Face Sets that compose the selected object. |
| **Element**        | The custom data associated with a Face Set.                |

It is important to note that the Custom Data component is read-only. As such, it cannot be used to add custom data to an object.
