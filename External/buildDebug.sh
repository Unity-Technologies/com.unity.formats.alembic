#!/bin/bash

installdir=$(pwd)/install
tgzdir=$(pwd)

if [[ -e ${installdir} ]]; then
    rm -rf ${installdir}
fi
mkdir -p ${installdir}

if [[ -e ilmbase-build ]]; then
    rm -rf ilmbase-build
fi
mkdir -p ilmbase-build
pushd ilmbase-build
cmake ../openexr/IlmBase -DCMAKE_BUILD_TYPE=Debug \
    -DCMAKE_INSTALL_PREFIX="${installdir}" \
    -DCMAKE_PREFIX_PATH="${installdir}" \
    -DNAMESPACE_VERSIONING=OFF \
    -DBUILD_SHARED_LIBS=OFF
cmake --build . --target install --config Debug
popd

if [[ -e alembic-build ]]; then
    rm -rf alembic-build
fi
mkdir -p alembic-build
pushd alembic-build
cmake ../alembic -DCMAKE_BUILD_TYPE=Debug \
    -DCMAKE_INSTALL_PREFIX="${installdir}" \
    -DCMAKE_PREFIX_PATH="${installdir}" \
    -DUSE_BINARIES=OFF \
    -DUSE_TESTS=OFF \
    -DALEMBIC_SHARED_LIBS=OFF \
    -DALEMBIC_ILMBASE_LINK_STATIC=ON \
    -DILMBASE_ROOT="${installdir}" 
cmake --build . --target install --config Debug
popd
