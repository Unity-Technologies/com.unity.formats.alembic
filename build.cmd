@echo off

cd External
call build.cmd
cd ..

SET depsdir=%cd%\External\install
SET installdir=%cd%
if "%PROCESSOR_ARCHITECTURE%"=="ARM64" (
    SET targetArch = "ARM64"
) else (
    SET targetArch = "x64"
)


if exist build (
    rmdir /s /q build
)
mkdir build
cd build
cmake .. ^
    -A %targetArch% ^
    -DCMAKE_GENERATOR_PLATFORM=%targetArch% ^
    -DCMAKE_BUILD_TYPE=Release ^
    -DALEMBIC_DIR=%depsdir% ^
    -DUSE_STATIC=ON ^
    -DENABLE_DEPLOY=OFF ^
    -DCMAKE_PREFIX_PATH=%depsdir% ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_CXX_FLAGS="/MP"
cmake --build . --target INSTALL --config Release
cd ..
