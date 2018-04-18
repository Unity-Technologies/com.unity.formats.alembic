Building on any platform:
1. Get the dependences:
        HDF5 - a legacy file format
        ilmbase - utilities related to openexr
        Alembic - the package we're wrapping
        ispc - a compiler for vectorized math routines (gives a 3x speedup)
2. Build the dependences.

3. Get Unity Alembic package source code
        Get Unity Alembic Plugin at https://github.com/unity3d-jp/AlembicForUnity
4. Fetch submodules
        git submodule update --init --recursive
5. Build the abci plugin
6. Install it in the AlembicImporter project
7. Test by dragging an abc file into the project and into the scene, then scrubbing the "time" on the abc file.

To publish you also need:
8. Get npm for your platform.

NOTE: source tree requires submodules

    git submodule update --init --recursive

====================
Building on Windows:
====================

???


================
Building on OSX and linux (from source):
================

0. On linux, build on centos7. That'll produce a binary compatible with later distros. The limiting factor is the g++ ABI version.

1. Install ispc:
        On OSX you can:
            sudo port install ispc
        On linux, download a version. Check build.sh for the appropriate version and the right place to put it.

2. Check the build.sh file to find the appropriate versions of the dependences and where to find them. The links can't be gotten with curl, you have to hit them with a browser, by hand.

3. Make a new directory to hold the build, outside the git repository:
        plugindir=`pwd`
        mkdir -p ~/projects/alembic-importer-build
        cd ~/projects/alembic-importer-build

4. Run:
        sh "${plugindir}"/build.sh

5. Watch the build happen (cmake and make and oh my).
6. Install the latest npm. You'll need to look up the version number:
        sudo port install npm5


==================
Moving the package
==================

Paths are hardcoded in two places:
- abci/CMakeLists.txt says where to install the built module (the swig
  front-end, basically)
- AlembicImporter/AbcAPI.cs says where to find the build module at runtime, relative to the virtual filesystem

Because Unity Package Manager for 2018.1 doesn't quite support native
modules, we need to specify a platform-dependent path in the DllImport.

==================
Publishing by hand
==================

0. Set up bintray.
  0.a Get your username by asking on slack #devs-packman.
  0.b Get your API key on bintray.com by hovering over your account
        (upper-right) and going to 'edit profile', then selecting 'API key'
  0.c Go to the directory that contains package.json and run:
    curl -u<USER>@unity:<API_KEY> https://staging.unity.com/auth >> .npmrc

1. Install the binaries for each platform (via sneakernet for now, should do it through git at some point)

2. Update the version number
      * Remember to use semver versioning: major.minor.patch
      * Remember to update the Changelog and the Documentation
      * Remember to check LICENSE.md and make sure nothing's changed; if so, call up Legal

2. Go to the directory that holds package.json and on the command-line run
    npm publish

That publishes to the staging repo. Tell QA to take a look and fill in the QAReport.md. Then when it's cleared QA ask around for how to push to production.
