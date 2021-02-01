# Alembic for Unity

Alembic for Unity is a Unity package developed and [distributed by Unity](https://docs.unity3d.com/Packages/com.unity.formats.alembic@latest), but also open to [user contribution](CONTRIBUTIONS.md).

Alembic is a data format mainly used in the VFX industry to store very large vertex cache data such as complex cloth and fluid simulation results, or complex animation rigs. For more information, see http://www.alembic.io/

The main features of the package include Alembic file import and export, which allows you to use Unity as a rendering or compositing tool, or perform various simulations in Unity and pass the results to other DCC tools.

## Before you start

**IMPORTANT:** Use this repository only if you need to build the Alembic for Unity package from its source. Otherwise, to use Alembic for Unity, you should install its [latest available official version](https://docs.unity3d.com/Packages/com.unity.formats.alembic@latest) from your Unity Editor, through the Package Manager.

## Building the package

### Requirements

The latest official version of Alembic for Unity built from this repository is compatible with the following versions of the Unity Editor:
- 2019.4 and later (recommended)

You can build and use the Alembic for Unity package on the following 64-bit desktop platforms:
- Microsoft Windows (x86-64)
- macOS (x86-64 and arm64)
- Linux (x86-64)

### Pre-requisites

To be able to build this package, you must install the following external dependencies:

- CMake 3 or later

- C++ compiler, according to your platform:
  - On Windows: Visual Studio 2017 or later, with C++ toolchain
  - On macOS: Clang (Xcode 12.3 or later)
  - On linux: GCC 7 or later

### Build steps

1. Clone this repository.

1. Checkout the submodules (only required the first time):

    `git submodule update --init --recursive`

1. Execute the command to run the build, according to your platform:
    - On Windows: `build.cmd`
    - On macOS or Linux: `build.sh`

### Build result

The build process stores the result (package, C# and native plugin code) at `com.unity.formats.alembic`

## Contributing

We appreciate all the help we can get to improve the Alembic for Unity package. Read the [instructions](CONTRIBUTIONS.md) if you want to contribute.

![example](Screenshots/alembic_example.gif)

## Reporting an issue

See the Alembic for Unity's team [recommendations](ISSUE_TEMPLATE.md) about the information you should ideally provide if you want to report an issue.
