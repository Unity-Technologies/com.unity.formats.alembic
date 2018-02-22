Building on any platform:
1. Get Alembic.
        Get Alembic releases at https://github.com/alembic/alembic/releases
2. Get HDF5 (needed for legacy alembic files)
3. Build the abci plugin
4. Install it in the AlembicImporter project
5. Test by dragging an abc file into the project and into the scene, then scrubbing the "time" on the abc file.


====================
Building on Windows:
====================

???


================
Building on OSX:
================

0. Make sure your PATH is appropriately clean:
   take out anaconda and other such packaging systems, and any other path that
   our user wouldn't have. Symptom if you forgot this: link errors during build,
   or DllImport errors in Unity.

1. Download Alembic. Browse to:
        https://github.com/alembic/alembic/releases
   Download the tar.gz or the .zip file, then click on it to unpack it.

2. Install HDF5:
        sudo port install hdf5

3. Make a new directory to hold the build, outside the git repository. Remember the current directory.
        plugindir=`pwd`
        mkdir -p ~/projects/alembic-importer-build
        cd ~/projects/alembic-importer-build

4. Run:
        sh "${plugindir}"/build.sh ~/Downloads/alembic-x.y.z
   Replace x.y.z with the version you want to use.

5. Watch the build happen (cmake and make and oh my).
   The last two lines are for you to copy-paste into the terminal. They:
        (a) delete the previous abci plugin
        (b) copy the new abci plugin in its place.

==================
Building on Linux:
==================

Should be similar to building on OSX, but you might need to change the build.sh a bit.


==================
Moving the package
==================

Paths are hardcoded in two places:
- abci/CMakeLists.txt says where to install the built module (the swig
  front-end, basically)
- AbcAPI.cs says where to find the build module at runtime, relative to the
  project directory.

Because Unity Package Manager for 2017.3 and 2018.1 doesn't support native
modules, we need to load the library by hand. Otherwise we could ignore
that second path and use [DllImport].
