# About Alembic for Unity

Use the com.unity.formats.alembic package to import [Alembic](http://www.alembic.io/) files into your Unity scene. This lets you bring in for example facial animation and cloth simulation from other software packages and have it reproduce exactly the same way in Unity.

## Requirements

Version 0.1.2-preview is compatible with Unity Editor 2018.1. See below to use Alembic for Unity in an earlier version of Unity.

The package is available on 64-bit desktop platforms:
* Windows 10
* macOS Sierra (10.12)
* GNU/Linux (Centos 7, Ubuntu 16.x and Ubuntu 17.x)

## Known Limitations

Version 0.1.2-preview is a preview release. Expect APIs and functionality to change in incompatible ways.

We welcome hearing about your experience on [this forum thread](https://forum.unity.com/threads/alembic-for-unity.521649/).

# Installing Alembic for Unity

Because Alembic for Unity is in preview, to install the package, you will need to edit your [project manifest](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@1.8/manual/index.html#PackManManifestsProject). In each project where you want to use Alembic for Unity, open the file `Packages/manifest.json` and add a reference to the `com.unity.formats.alembic` package. For example:

```
{
    "dependencies": {
        "com.unity.formats.alembic": "0.1.2-preview"
    }
}
```

Once you do this, the package will become visible in the Package Manager window so that you will be able to easily update it. You will need to manually add the package in each project where you want to use it.

The package is only available on Unity 2018.1 and later.  To install Alembic for Unity in an earlier version of Unity, head to the [project repository](https://github.com/unity3d-jp/AlembicImporter/releases).

# Using Alembic for Unity

Drag an abc file into the project view.

![Drag the file](images/drag-to-project.png)

Then drag the asset into the scene and scrub the time component.

![Scrub the time](images/scrub-time.png)

To animate using Timeline, create a timeline and animate the time:

![Timeline](images/timeline.png)


# Document Revision History

|Date|Reason|
|---|---|
|2018-03-19|Created. Matches package version 0.1.2-preview.|
