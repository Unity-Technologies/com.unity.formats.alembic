#!/bin/bash

## Any subsequent(*) commands which fail will cause the shell script to exit immediately, otherwise the job will fail silently.
set -e

# Produce fast and small code (but not debuggable), and produce it to be
# relocatable since in the end we'll link it all together in a shared object.
# Note that cmake seems to clobber the -O3 with a -O2, but we can dream.
export CXXFLAGS="-O3 -fomit-frame-pointer -fPIC"
export CFLAGS="-O3 -fomit-frame-pointer -fPIC"

OSX_DEPLOYMENT_TARGET=
if [ "$(uname)" == "Darwin" ]; then
   export CXXFLAGS="${CXXFLAGS} -arch x86_64 -arch arm64"
   export CFLAGS="${CFLAGS} -arch x86_64 -arch arm64"
   OSX_DEPLOYMENT_TARGET=10.14

fi

export MAKEFLAGS="-j12"

pushd External
./build.sh
popd

if [[ -e build ]]; then
    rm -rf build
fi

depsdir=${PWD}/External/install
installdir=${PWD}
mkdir -p build
pushd build
cmake .. -DCMAKE_BUILD_TYPE=RelWithDebInfo \
    -DALEMBIC_DIR=${depsdir} \
    -DUSE_STATIC=ON \
    -DENABLE_DEPLOY=OFF \
    -DCMAKE_PREFIX_PATH=${depsdir} \
    -DCMAKE_INSTALL_PREFIX=${installdir} \
    -DCMAKE_OSX_DEPLOYMENT_TARGET=${OSX_DEPLOYMENT_TARGET}

cmake --build . --target install --config RelWithDebInfo
popd
