@echo off

echo "downloading external libararies..."
powershell.exe -Command "(new-object System.Net.WebClient).DownloadFile('https://github.com/unity3d-jp/AlembicImporter/releases/download/20180122/External.7z', 'External/External.7z')"
cd External
7z\7za.exe x -aos External.7z
cd ..
