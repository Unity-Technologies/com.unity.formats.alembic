# Alembic Custom Data component

The Alembic Custom Data component allows you to see additionnal custom information stored with each Face Set. A Face Set is a sub-group of faces that compose an object.

To access the Custom Data component, select a child node of an imported Alembic asset in your scene hierarchy. If there is any additionnal data associated to the object, the component will be added automatically. The absence of the Custom Data component means there is no custom data associated to an object.

![Alembic Custom Data component options](images/abc_custom_data.png)
 
| ***Property*** | ***Description*** |
|:---|:---|
| **Face Set Names** | Names of the Face Sets that compose the selected object. |
| **Element**        | The custom data associated to a Face Set.                |

It is important to note that this is a read-only component. As such, it cannot be used to add Custom Data to an object.

