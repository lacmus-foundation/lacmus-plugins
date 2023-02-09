#!/bin/bash
rm -rf ./build && \
rm -rf ./App/bin/Release/ \
rm -rf ./LacmusPlugin/bin/Release/ \
rm -rf ./LacmusRetinanetPlugin/bin/Release/
rm -rf ./LacmusRetinanetPlugin.Cuda/bin/Release/
rm -rf ./LacmusRetinanetPlugin.DirectML/bin/Release/
echo "restoring packeges\n"
dotnet restore
echo -n "building for linux\n"
dotnet publish --framework net6.0 --runtime="linux-x64" -c Release -o ./build/app/linux_amd64 App/App.csproj
echo -n "building for win\n"
dotnet publish --framework net6.0 --runtime="win-x64" -c Release -o ./build/app/win_amd64 App/App.csproj
echo -n "building for osx\n"
dotnet publish --framework net6.0 --runtime="osx-x64" -c Release -o ./build/app/osx_amd64 App/App.csproj
echo -n "building lacmus plugin [cpu]\n"
dotnet publish --framework net6.0 -c Release -o ./build/plugins/LacmusRetinanetPlugin.Cpu LacmusRetinanetPlugin/LacmusRetinanetPlugin.csproj
echo -n "building lacmus plugin [cuda]\n"
dotnet publish --framework net6.0 -c Release -o ./build/plugins/LacmusRetinanetPlugin.Cuda LacmusRetinanetPlugin.Cuda/LacmusRetinanetPlugin.Cuda.csproj
echo -n "building lacmus plugin [direct-ml]\n"
dotnet publish --framework net6.0 -c Release -o ./build/plugins/LacmusRetinanetPlugin.DirectML LacmusRetinanetPlugin.DirectML/LacmusRetinanetPlugin.DirectML.csproj
echo -n "building lacmus yolo plugin [cpu]\n"
dotnet publish --framework net6.0 -c Release -o ./build/plugins/LacmusYolo5Plugin.Cpu LacmusYolo5Plugin/LacmusYolo5Plugin.csproj
echo -n "building lacmus yolo plugin [cuda]\n"
dotnet publish --framework net6.0 -c Release -o ./build/plugins/LacmusYolo5Plugin.Cuda LacmusYolo5Plugin.Cuda/LacmusYolo5Plugin.Cuda.csproj
echo -n "building lacmus yolo plugin [direct-ml]\n"
dotnet publish --framework net6.0 -c Release -o ./build/plugins/LacmusYolo5Plugin.DirectML LacmusYolo5Plugin.DirectML/LacmusYolo5Plugin.DirectML.csproj

#mkdir ./build/app/linux_amd64/plugins
#cp -r ./build/plugins/LacmusYolo5Plugin.Cpu ./build/app/linux_amd64/plugins/LacmusYolo5Plugin.Cpu
#cd ./bin/app/
#zip -r -9 ./linux.zip ./linux/
#zip -r -9 ./win10.zip ./win10/
#zip -r -9 ./osx.zip ./osx/
