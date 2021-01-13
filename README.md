# Alembic for Unity

**Download the latest release available for your Editor from the PackageManagerUI. Use this repository only if you are trying to build plugin from source.**

## Alembic?
![example](Screenshots/alembic_example.gif)
Alembic is a data format mainly used in the video industry and is used to store huge vertex cache data. In the video industry, simulation results such as skinning and dynamics are baked for all frames, converted to a vertex cache, stored in Alembic, and passed to renderer or composite software. Alembic Headquarters: http://www.alembic.io/

Many modern DCC tools support Alembic, and if you can import and export Alembic, you can use Unity as a rendering or compositing tool, or perform various simulations in Unity and pass the results to other DCC tools. You will be able to do such things. There are new ways to use it, such as 3D recording of games. This plugin enables import and export of Alembic in Unity.

It has been confirmed to work on Windows (64bit), Mac, Linux and Unity 2019.4 or later. To use it, first import this package into your project.
## Building 
The Unity Alembic package supports Windows, OSX, Linux as build targets.
External dependencies: CMake >=3, C++ compiler (Windows VS2017 with C++ toolchain),
Clang on OSX, GCC on Linux.
The build result (package, C# and native plugin code) can be found at: com.unity.formats.alembic

1) Checkout submodules: git submodule update --init --recursive
2) Windows run build.cmd
   OSX, Linux run build.sh

## Contributing
We appreciate all the help we can get. Please see [Contributing](Contributing.md) for more details.