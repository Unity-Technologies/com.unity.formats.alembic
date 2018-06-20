# About the Alembic package

Use the Alembic package to import [Alembic](http://www.alembic.io/) files into your Unity scenes. This lets you bring in for example facial animation and cloth simulation from other software packages and have it reproduce exactly the same way in Unity.

## Requirements

Version 0.2.0-preview is compatible with Unity Editor 2018.2

The package is available on 64-bit desktop platforms:
* Windows 10
* macOS Sierra (10.12)
* GNU/Linux (Centos 7, Ubuntu 16.x and Ubuntu 17.x)

## Known Limitations

* There is no exposed public API in the Alembic package.

We welcome hearing about your experience on [this forum thread](https://forum.unity.com/threads/alembic-for-unity.521649/).

# Using Alembic for Unity

Drag an abc file into the project view.

![Drag the file](images/drag-to-project.png)

Then drag the asset into the scene and scrub the time component.

![Scrub the time](images/scrub-time.png)

To animate using Timeline, create a timeline and animate the time:

![Timeline](images/timeline.png)
