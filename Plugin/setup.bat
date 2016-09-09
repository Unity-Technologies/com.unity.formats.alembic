
IF EXIST "external/libs" (
    echo "skipping setup"
) ELSE (
    cd external
    7z\7za.exe x -aos libs.7z
)
