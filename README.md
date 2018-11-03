# Reverse Proxy

This is a simple Reverse Proxy implementation.
It allows you to access remote server in DMZ-Network without external IP/Internet access.
Reverse proxy support any TCP protocol such as SSL/TLS

Proxy consisnt of 2 components: 
- **Remote Server** that listen to incoming connections from external network and proxying traffic through to **Agent** (a secure server)
- **Agent** that connects to external **Remote Server** and proxying incoming/outcoming traffic to/from a secure server

## How it works
- **Agent** is proxying all traffic to a secure server without any access from extenral network
- **Agent** connects to **Remote Server**, placed in external network and it's proxying all incoming packets, so secure server may not has external IP at all or Firewall may block all incoming connections from WEB.
- **Remote server** listen to incoming connections. For external clients **Remote server** acting just like a target server (e.g. some database or file server)
- **Remote server** interacts with **Agent** with special packets which represents tarffic between external client and internal server

## Connection diagram

![Pic. 1](https://user-images.githubusercontent.com/1194059/47950632-9395c080-df76-11e8-8aaa-eb9997315ba2.png)
