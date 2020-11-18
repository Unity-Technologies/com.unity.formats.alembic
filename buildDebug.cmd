@echo off

cd External
call buildDebug.cmd
cd ..

SET depsdir=%cd%\External\install
SET installdir=%cd%


if exist build (
    rmdir /s /q build
)
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Debug ^
    -DALEMBIC_DIR=%depsdir% ^
    -DUSE_STATIC=ON ^
    -DENABLE_DEPLOY=OFF ^
    -DCMAKE_PREFIX_PATH=%depsdir% ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -G "Visual Studio 15 2017 Win64"
cmake --build . --target INSTALL --config Debug
cd ..
