SET deploydir=%cd%/Source/external/ThirdParty/Deploy/Windows
SET installdir=%cd%/build/install
echo %deploydir%
md build
cd build
cmake .. -DALEMBIC_DIR=%deploydir% -DHDF5_USE_STATIC_LIBRARIES=ON -DHDF5_ROOT=%deploydir% -DUSE_STATIC=ON -DENABLE_DEPLOY=OFF -DCMAKE_BUILD_TYPE=Release -DCMAKE_PREFIX_PATH=%deploydir% -DCMAKE_INSTALL_PREFIX=%installdir% -G "Visual Studio 15 2017 Win64"
cmake --build . --target INSTALL --config Release
