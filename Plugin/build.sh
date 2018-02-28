#! /bin/sh

# Build script for OSX
# 50-50 chance it would work for linux, except for the .bundle versus .so bit

# parse arguments
alembic=$1
if [ x"$alembic" == x ] ; then
  echo "USAGE: $0 /path/to/alembic/source"
  exit 1
fi

# avoid a problem with anaconda, which installs HDF5 dynamic only
if echo "$PATH" | grep -q anaconda ; then
  echo "WARNING: Your path has anaconda in it; try running this script with"
  echo "    PATH=/everything/but/anaconda $0 $*"
  sleep 2
fi

plugindir=`dirname "$0"`
plugindir=`cd "$plugindir" && pwd`


# echo and exit on error, like a makefile
set -ex

# build & install Alembic
mkdir alembic-build
pushd alembic-build
cmake "$alembic" \
        -DUSE_HDF5=ON \
        -DALEMBIC_SHARED_LIBS=OFF \
        -DCMAKE_INSTALL_PREFIX="`pwd`/../alembic-installed"
make -j4
make test
make install
popd

# build & install abci ('make' installs into the local
# com.unity.alembic package)
mkdir plugin-build
pushd plugin-build
cmake "$plugindir" \
        -DALEMBIC_DIR="`pwd`/../alembic-installed" \
        -DUSE_STATIC=ON \
        -DENABLE_ISPC=OFF
make -j4
make test
popd
