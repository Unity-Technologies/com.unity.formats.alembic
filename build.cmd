@echo off

cd External
call build.cmd
cd ..

SET depsdir=%cd%\External\install
SET installdir=%cd%


if exist build (
    rmdir /s /q build
)
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release ^
    -DALEMBIC_DIR=%depsdir% ^
    -DUSE_STATIC=ON ^
    -DENABLE_DEPLOY=OFF ^
    -DCMAKE_PREFIX_PATH=%depsdir% ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_CXX_FLAGS="/MP"
cmake --build . --target INSTALL --config Release
cd ..
