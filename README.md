# NTPMonlistCheck
A project that check the data return by sending magic byte of different version of monlist command to a NTP Server to test the return byte.

This project designed to checked the reflected ratio of NTP monlist command used at RDDoS.

# Network Time Protocol (NTP) 

Network Time Protocol (NTP) is a networking protocol for clock synchronization between computer systems, it use UDP port 123.

This service support a monitoring service allows administrators to query the server for traffic counts of connected clients which is “monlist” command. 

It is a read only command that not protected by the NTP authentication mechanism. 

The attacker sending a "get monlist" request to a vulnerable NTP server, with the source address spoofed to be the victim’s address, then the NTP server will return a list of address up to 600 machine that NTP interact with. This will greatly increase the attack traffic and consume all the bandwidth of the victim.

# How To
You need to get a list of IP address that contain NTP Server first.

1. You can get this by using Zmap with following command:
```
zmap -w /home/kenny/zmap/hk.zone -B 3M -M udp --probe-args=file:/home/kenny/zmap/ntp_123.pkt -p 123 -o NTP_Server.txt
```

2. Use netcat and to get the whois information of the ip get of previous step
```
netcat whois.cymru.com 43 < NTP_Server_cn.txt | sort -n > NTP_Server_cn_host.txt
```

3. Run the Program. 

4. It will first send a normal request to a NTP server to get the current time to make sure that the service is running normally, then it will send the monlist V2/V3 command respectively to see if the server response with the command and measure the size of response data.
Check [Monlist.png](../../blob/master/Monlist.png) for output reference.
