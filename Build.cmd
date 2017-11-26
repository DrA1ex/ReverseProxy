dotnet clean
dotnet restore
dotnet publish -c Release -r win10-x64

rmdir bin /s/q >nul 2>&1

xcopy ReverseProxy.Agent\bin\Release\netcoreapp2.0\win10-x64\*.exe bin\Agent\*.* > nul
xcopy ReverseProxy.Agent\bin\Release\netcoreapp2.0\win10-x64\*.dll bin\Agent\*.* > nul
xcopy ReverseProxy.Agent\bin\Release\netcoreapp2.0\win10-x64\*.json bin\Agent\*.* > nul
xcopy ReverseProxy.Agent\bin\Release\netcoreapp2.0\win10-x64\*.config bin\Agent\*.* > nul

xcopy ReverseProxy.RemoteServer\bin\Release\netcoreapp2.0\win10-x64\*.exe bin\RemoteServer\*.* > nul
xcopy ReverseProxy.RemoteServer\bin\Release\netcoreapp2.0\win10-x64\*.dll bin\RemoteServer\*.* > nul
xcopy ReverseProxy.RemoteServer\bin\Release\netcoreapp2.0\win10-x64\*.json bin\RemoteServer\*.* > nul
xcopy ReverseProxy.RemoteServer\bin\Release\netcoreapp2.0\win10-x64\*.config bin\RemoteServer\*.* > nul