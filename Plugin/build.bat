call buildtools.bat

msbuild abci.sln /t:Build /p:Configuration=Master /p:Platform=Win32 /m /nologo
IF %ERRORLEVEL% NEQ 0 (
    pause
    exit /B 1
)

msbuild abci.sln /t:Build /p:Configuration=Master /p:Platform=x64 /m /nologo
IF %ERRORLEVEL% NEQ 0 (
    pause
    exit /B 1
)
