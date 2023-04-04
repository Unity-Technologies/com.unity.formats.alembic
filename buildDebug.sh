#!/bin/bash

# Produce fast and small code (but not debuggable), and produce it to be
# relocatable since in the end we'll link it all together in a shared object.
# Note that cmake seems to clobber the -O3 with a -O2, but we can dream.
export CXXFLAGS="-O0 -fomit-frame-pointer -fPIC"
export CFLAGS="-O0 -fomit-frame-pointer -fPIC"

if [ "$(uname)" == "Darwin" ]; then
   export CXXFLAGS="${CXXFLAGS} -arch x86_64 -arch arm64"
   export CFLAGS="${CFLAGS} -arch x86_64 -arch arm64"
   export MACOSX_DEPLOYMENT_TARGET=10.14

fi

export MAKEFLAGS="-j12"

pushd External
./buildDebug.sh
popd

if [[ -e build ]]; then
    rm -rf build
fi

depsdir=${PWD}/External/install
installdir=${PWD}
mkdir -p build
pushd build
cmake .. -DCMAKE_BUILD_TYPE=Debug \
    -DALEMBIC_DIR=${depsdir} \
    -DUSE_STATIC=ON \
    -DENABLE_DEPLOY=OFF \
    -DCMAKE_PREFIX_PATH=${depsdir} \
    -DCMAKE_INSTALL_PREFIX=${installdir}

cmake --build . --target install --config Debug
popd
