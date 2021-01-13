#!/bin/bash

installdir=$(pwd)/install
tgzdir=$(pwd)

# Produce fast and small code (but not debuggable), and produce it to be
# relocatable since in the end we'll link it all together in a shared object.
# Note that cmake seems to clobber the -O3 with a -O2, but we can dream.
export CXXFLAGS="-O3 -fomit-frame-pointer -fPIC"
export CFLAGS="-O3 -fomit-frame-pointer -fPIC"
export MAKEFLAGS="-j12"


if [[ -e ${installdir} ]]; then
    rm -rf ${installdir}
fi
mkdir -p ${installdir}

if [[ -e ilmbase-build ]]; then
    rm -rf ilmbase-build
fi
mkdir -p ilmbase-build
pushd ilmbase-build
cmake ../openexr/IlmBase -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_INSTALL_PREFIX="${installdir}" \
    -DCMAKE_PREFIX_PATH="${installdir}" \
    -DNAMESPACE_VERSIONING=OFF \
    -DBUILD_SHARED_LIBS=OFF
cmake --build . --target install --config Release
popd

if [[ -e alembic-build ]]; then
    rm -rf alembic-build
fi
mkdir -p alembic-build
pushd alembic-build
cmake ../alembic -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_INSTALL_PREFIX="${installdir}" \
    -DCMAKE_PREFIX_PATH="${installdir}" \
    -DUSE_BINARIES=OFF \
    -DUSE_TESTS=OFF \
    -DALEMBIC_SHARED_LIBS=OFF \
    -DALEMBIC_ILMBASE_LINK_STATIC=ON \
    -DILMBASE_ROOT="${installdir}" 
cmake --build . --target install --config Release
popd
