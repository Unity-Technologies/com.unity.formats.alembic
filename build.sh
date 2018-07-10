#!/bin/bash

deploydir=${PWD}/Source/external/ThirdParty/Deploy/MacOS
installdir=${PWD}/build/install
mkdir -p build
cd build
cmake .. -DALEMBIC_DIR=${deploydir} -DHDF5_USE_STATIC_LIBRARIES=ON -DHDF5_LIBRARIES=${deploydir}/lib/libhdf5.a';-ldl;-lpthread' -DHDF5_ROOT=${deploydir} -DUSE_STATIC=ON -DENABLE_DEPLOY=OFF -DCMAKE_BUILD_TYPE=Release -DCMAKE_PREFIX_PATH=${deploydir} -DCMAKE_INSTALL_PREFIX=${installdir}
cmake --build . --target install --config Release
