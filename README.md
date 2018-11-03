# ReverseProxy

This is a simple Reverse Proxy implementation.
It allows you to access remote server in DM-zone without external IP/Internet access.

Proxy consisnt of 2 components: 
- *RemoteServer* that listen to incoming connections from external network and proxying traffic through to *Agent* (a secure server)
- *Agent that connects to external *RemoteServer* and proxying incoming/outcoming traffic to/from a secure server

![Pic. 1](https://user-images.githubusercontent.com/1194059/47950632-9395c080-df76-11e8-8aaa-eb9997315ba2.png)
