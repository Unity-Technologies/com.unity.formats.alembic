Building on any platform:
1. Get Alembic.
        Get Alembic releases at https://github.com/alembic/alembic/releases
2. Get HDF5 (needed for legacy alembic files)
3. Build the abci plugin
4. Install it in the AlembicImporter project
5. Test by dragging an abc file into the project and into the scene, then scrubbing the "time" on the abc file.

To publish you also need:
6. Get npm for your platform.


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

6. Install the latest npm. You'll need to look up the version number:
        sudo port install npm5

==================
Building on Linux:
==================

Same as OSX, but hdf5 and ilmbase have different install lines:

On Ubuntu:
        sudo apt install hdf5-dev ilmbase-dev
On CentOS:
        sudo yum install hdf5-devel ilmbase-devel


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

==================
Publishing
==================

0. Set up bintray.
  0.a Get your username by asking on slack #devs-packman.
  0.b Get your API key on bintray.com by hovering over your account
        (upper-right) and going to 'edit profile', then selecting 'API key'
  0.c Go to the directory that contains package.json and run:
    curl -u<USER>@unity:<API_KEY> https://staging.unity.com/auth >> .npmrc

1. Install the binaries for each platform (via sneakernet for now, should
        do it through git at some point)

2. Update the version number (remember to use semver versioning:
        major.minor.patch with optional -beta)

2. Go to the directory that holds package.json and on the command-line run
    npm publish

That publishes to unity-dev, which is for development. When it's ready for
QA do the same but for unity-staging. Then when it's cleared QA ask around
for how to push to production.
