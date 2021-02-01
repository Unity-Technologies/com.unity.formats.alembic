#!/bin/bash

pushd External
./buildDebug.sh
popd

if [[ -e build ]]; then
    rm -rf build
fi

export MAKEFLAGS="-j12"


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
