# Reverse Proxy

This is a simple implementation of a reverse proxy that allows you to access a remote server in a DMZ network without external IP or internet access. The reverse proxy supports any TCP protocol, including SSL/TLS.

The proxy consists of two components:
- **Remote Server**: Listens to incoming connections from the external network and proxies traffic to the **Agent** (a secure server).
- **Agent**: Connects to the external **Remote Server** and proxies incoming/outgoing traffic to/from a secure server.

## How it Works
- The **Agent** proxies all traffic to a secure server without any access from the external network.
- The **Agent** connects to the **Remote Server**, which is placed in the external network, and proxies all incoming packets. Therefore, the secure server may not have an external IP at all, or the firewall may block all incoming connections from the web.
- The **Remote Server** listens to incoming connections and acts as a target server for external clients (e.g., a database or file server).
- The **Remote Server** interacts with the **Agent** using special packets that represent traffic between the external client and internal server.

## Connection diagram

![Pic. 1](https://user-images.githubusercontent.com/1194059/47950632-9395c080-df76-11e8-8aaa-eb9997315ba2.png)

## Requirements
- .NET 4.6.2 for Windows
- .NET Core 2.0 for Linux, Mac and Windows

## Building
You can use Build.cmd or use dotnet toolkit:
For Windows:

```dotnet publish -c Release -f net462 -r win10-x64```

For Mac:

```dotnet publish -c Release -r osx-x64```

For Linux:

```dotnet publish -c Release -r linux-x64```

For all platforms:

```dotnet publish -c Release -f netcoreapp2.0```
