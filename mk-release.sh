#!/bin/bash
rm -rf ./build && \
rm -rf ./App/bin/Release/ \
rm -rf ./LacmusPlugin/bin/Release/ \
rm -rf ./LacmusRetinanetPlugin/bin/Release/
echo "restoring packeges\n"
dotnet restore
echo -n "building for linux\n"
dotnet publish --framework netcoreapp3.1 --runtime="linux-x64" -c Release -o ./build/app/linux_amd64 App/App.csproj
echo -n "building for win\n"
dotnet publish --framework netcoreapp3.1 --runtime="win-x64" -c Release -o ./build/app/win_amd64 App/App.csproj
echo -n "building for osx\n"
dotnet publish --framework netcoreapp3.1 --runtime="osx-x64" -c Release -o ./build/app/osx_amd64 App/App.csproj
echo -n "building lacmus plugin [cpu]\n"
dotnet publish --framework netcoreapp3.1 -c Release -o ./build/plugins/LacmusRetinanetPlugin.Cpu LacmusRetinanetPlugin/LacmusRetinanetPlugin.csproj
echo -n "building lacmus plugin [cuda]\n"
dotnet publish --framework netcoreapp3.1 -c Release -o ./build/plugins/LacmusRetinanetPlugin.Cuda LacmusRetinanetPlugin.Cuda/LacmusRetinanetPlugin.Cuda.csproj
echo -n "building lacmus plugin [direct-ml]\n"
dotnet publish --framework netcoreapp3.1 -c Release -o ./build/plugins/LacmusRetinanetPlugin.DirectML LacmusRetinanetPlugin.DirectML/LacmusRetinanetPlugin.DirectML.csproj
#cd ./bin/app/
#zip -r -9 ./linux.zip ./linux/
#zip -r -9 ./win10.zip ./win10/
#zip -r -9 ./osx.zip ./osx/