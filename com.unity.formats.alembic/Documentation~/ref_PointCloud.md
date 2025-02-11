# Alembic Point Cloud component

The **Alembic Point Cloud** component allows you to define the shape and volume of the particle's point cloud.

To access the Alembic Point Cloud component, in the Hierarchy, within an imported Alembic asset instance, select a GameObject that corresponds to an Alembic node that contains particles.

> [!NOTE]
> When you import the Alembic asset in Unity, you must select **Import Points** among the Alembic import settings.

![Alembic Point Cloud component options](images/abc_point_cloud_options.png)

| ***Property*** | ***Description*** |
|:---|:---|
| **Script**        | The script that defines this component. You cannot modify this property. |
| **Bounds Center** | Set the position in **X**, **Y**, and **Z** for the center of the particle cloud. |
| **Bounds Extents** | Set the bounding limit for the particle cloud. Each **X**, **Y**, and **Z** value defines the maximum distance between the **Bounds Center** value and the extents of the bounding box (AABB). For more information, see the [Bounds struct reference page in the Unity manual](https://docs.unity3d.com/ScriptReference/Bounds.html). |
| **Sort** | Check to enable particle sorting. Particle sorting allows you to set realistic particle effects by defining the order in which Unity renders particles. For example, the particles that are drawn last overlay the particles that were drawn earlier. |
| **Sort From** | Set the point of reference for sorting particles. For example, if you select a camera, Unity renders the particles furthest away from that camera first, so that the closest particles to the camera overlay the others. |

## Additional resources

* [Import Alembic files](import.md)
* [Work with Particles](particles.md)
