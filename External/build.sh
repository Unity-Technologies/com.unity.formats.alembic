#!/bin/bash

installdir=$(pwd)/install
tgzdir=$(pwd)

# Produce fast and small code (but not debuggable), and produce it to be
# relocatable since in the end we'll link it all together in a shared object.
# Note that cmake seems to clobber the -O3 with a -O2, but we can dream.
export CXXFLAGS="-O3 -fomit-frame-pointer -fPIC"
export CFLAGS="-O3 -fomit-frame-pointer -fPIC"
export MAKEFLAGS="-j12"

hdf5_version=1.10.1
hdf5_arch=$(uname)

if [[ -e ${installdir} ]]; then
    rm -rf ${installdir}
fi
mkdir -p ${installdir}

if [[ -e hdf5-build ]]; then
    rm -rf hdf5-build
fi
mkdir -p hdf5-build
pushd hdf5-build
tar xf ${tgzdir}/HDF5-${hdf5_version}-${hdf5_arch}.tar.gz
cp -R HDF5-${hdf5_version}-${hdf5_arch}/HDF_Group/HDF5/${hdf5_version}/* ${installdir}
popd

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
    -DUSE_HDF5=ON \
    -DHDF5_USE_STATIC_LIBRARIES=ON \
    -DALEMBIC_ILMBASE_LINK_STATIC=ON \
    -DUSE_STATIC_HDF5=ON \
    -DILMBASE_ROOT="${installdir}" \
    -DHDF5_ROOT="${installdir}"
cmake --build . --target install --config Release
popd
