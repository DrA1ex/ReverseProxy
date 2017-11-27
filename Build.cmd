dotnet clean
dotnet restore
dotnet publish -c Release -f net462 -r win10-x64

rmdir bin /s/q >nul 2>&1

xcopy ReverseProxy.Agent\bin\Release\net462\win10-x64\publish\*.exe bin\Agent\*.* > nul
xcopy ReverseProxy.Agent\bin\Release\net462\win10-x64\publish\*.dll bin\Agent\*.* > nul
xcopy ReverseProxy.Agent\bin\Release\net462\win10-x64\publish\*.json bin\Agent\*.* > nul
xcopy ReverseProxy.Agent\bin\Release\net462\win10-x64\publish\*.config bin\Agent\*.* > nul

xcopy ReverseProxy.RemoteServer\bin\Release\net462\win10-x64\publish\*.exe bin\RemoteServer\*.* > nul
xcopy ReverseProxy.RemoteServer\bin\Release\net462\win10-x64\publish\*.dll bin\RemoteServer\*.* > nul
xcopy ReverseProxy.RemoteServer\bin\Release\net462\win10-x64\publish\*.json bin\RemoteServer\*.* > nul
xcopy ReverseProxy.RemoteServer\bin\Release\net462\win10-x64\publish\*.config bin\RemoteServer\*.* > nul