using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace NTPMonlistCheck
{
    struct dataReturn
    {
        public bool connect;
        public int datasend;
        public int NormalResponseSize;
        public int monlistV2;
        public int monlistV3;
    };

    

    class Program
    {
        public static int getcount(byte[] data)
        {
            int length = data.Length;
            int count;
            for (count = length - 1; count >= 0; count--)
            {
                if (data[count] != 0x00)
                {
                    break;
                }
            }
            return count + 1;
        }
        static void Main(string[] args)
        {
            /*
            dataReturn dt = new dataReturn();
            dt = check("stdtime.gov.hk");
            //dt = check("192.168.11.108");
            Console.WriteLine("{0}|{1}|{2}|{3}|{4}",dt.connect.ToString(),dt.datasend.ToString(),dt.NormalResponseSize.ToString(),dt.monlistV2.ToString(),dt.monlistV3.ToString());
            dt = check("stdtime.gov.hk");
            //dt = check("192.168.11.108");
            Console.WriteLine("{0}|{1}|{2}|{3}|{4}", dt.connect.ToString(), dt.datasend.ToString(), dt.NormalResponseSize.ToString(), dt.monlistV2.ToString(), dt.monlistV3.ToString());
            */
            Console.ReadLine();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] files = Directory.GetFiles(baseDirectory, "*.log");
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                string newFileName = fi.FullName.Replace(fi.Extension, ".log2");
                Console.WriteLine(newFileName);
                StreamReader sr = new StreamReader(file);
                StreamWriter sw = new StreamWriter(newFileName);
                while (!sr.EndOfStream)
                {
                    string data = sr.ReadLine();
                    string[] datalist = data.Split('|');
                    string ip = datalist[1].Trim();
                    dataReturn dr = new dataReturn();
                    dr = check(ip);
                    if (!dr.connect)//if not connect neet to retry since that some time it may not replay due to network problem.
                    {
                        Console.WriteLine("Recheck due to disconnect");
                        dr = check(ip);
                    }
                    sw.WriteLine(data + "|{0}|{1}|{2}|{3}|{4}", dr.connect.ToString(), dr.datasend.ToString(), dr.NormalResponseSize.ToString(), dr.monlistV2.ToString(), dr.monlistV3.ToString());
                    sw.Flush();
                }
                sr.Close();
                sr.Dispose();
                sw.Close();
                sw.Dispose();
                
            }
            Console.ReadLine();
        }



        public static dataReturn check(string server)
        {
            dataReturn returnValue = new dataReturn();
            returnValue.connect = false;
            returnValue.datasend = 48;
            returnValue.NormalResponseSize = 0;
            returnValue.monlistV2 = 0;
            returnValue.monlistV3 = 0;

            string ntpServer = server;

            var ntpData = new byte[48]; //Initialize byte array for Data we wish to send
            var ntpReceive = new byte[48]; //Initialize byte array for Data we wish to receive
            ntpData[0] = 0x1B; //NTP Magic data - LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            //Create new instance of a Socket  with params defined above
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                Console.WriteLine("Attempting to connect to {0}", ntpServer.ToString());
                socket.ReceiveTimeout = 5000; //Set Limit of 5 seconds for connection
                socket.Connect(ntpServer, 123);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not connect to NTP Server {0} - Check that UDP Port 123 is not being blocked by something like a firewall...", ntpServer.ToString());
                Console.ResetColor();
                return returnValue;
            }
            

            socket.Send(ntpData); //Send the NTP Time Request.

            try
            {
                //Attempt to recieve the response from NTP Server
                socket.Receive(ntpReceive);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Did not receive a response from the remote NTP Server {0} - Check that UDP Port 123 is not being blocked by something like a firewall...", ntpServer.ToString());
                Console.ResetColor();
                return returnValue;
            }

            returnValue.connect = true;

            if (ntpReceive.Count() > 0)
            {
                returnValue.NormalResponseSize = ntpReceive.Count();

                //Do some magic to convert the NTP response into human readable time
                ulong intPart = (ulong)ntpReceive[40] << 24 | (ulong)ntpReceive[41] << 16 | (ulong)ntpReceive[42] << 8 | (ulong)ntpReceive[43];
                ulong fractPart = (ulong)ntpReceive[44] << 24 | (ulong)ntpReceive[45] << 16 | (ulong)ntpReceive[46] << 8 | (ulong)ntpReceive[47];

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

                Console.WriteLine("NTP Server responded with the following time:");
                Console.WriteLine(networkDateTime);

                Console.WriteLine("\nAttempting to check if the remote server will return MONLIST information...");
                //===================NTP V2===============================
                byte[] monlistdata = new byte[] { 0x17, 0x00, 0x03, 0x2a, 0x00, 0x00, 0x00, 0x00 }; //Initialize byte array for MONLIST data we wish to send(NTP V2)
                byte[] monlistdataReceive = new byte[1048576]; //Initialize byte array for Monlist we wish to receive (some paper said that it would be 48k max, so i use a bigger value 1mb to see how large data i can get)


                try
                {
                    Console.WriteLine("Attempting to connect to {0}", ntpServer.ToString());
                    socket.ReceiveTimeout = 5000; //Set Limit of 5 seconds for connection
                    socket.Connect(ntpServer, 123);

                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not connect to NTP Server {0} - Check that UDP Port 123 is not being blocked by something like a firewall...", ntpServer.ToString());
                    Console.ResetColor();
                    //returnValue.connect = false;
                }

                if (returnValue.connect)
                {
                    socket.Send(monlistdata); //Send the NTP Time Request.

                    try
                    {
                        //Attempt to recieve the response from NTP Server
                        socket.Receive(monlistdataReceive);
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("NTP Server {0} is NOT responding to a MON_GETLIST V2, that's great!", ntpServer.ToString());
                        Console.ResetColor();
                    }

                    //Clean up after ourselves and dispose of socket.           
                    //socket.Close();

                    if (monlistdataReceive[7] == 72)
                    {
                        //returnValue.monlistV2 = monlistdataReceive.Count();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("NTP Server {0} IS returning Monlist information when MON_GETLIST V2", ntpServer.ToString());
                        Console.ResetColor();
                    }
                    returnValue.monlistV2 = getcount(monlistdataReceive);

                }
                //======================NTP V3===================================================================================
                monlistdata = new byte[] { 0x1b, 0x00, 0x03, 0x2a, 0x00, 0x00, 0x00, 0x00 }; //Initialize byte array for MONLIST data we wish to send(NTP V3)
                monlistdataReceive = new byte[1048576]; //Initialize byte array for Monlist we wish to receive


                try
                {
                    Console.WriteLine("Attempting to connect to {0}", ntpServer.ToString());
                    socket.ReceiveTimeout = 5000; //Set Limit of 5 seconds for connection
                    socket.Connect(ntpServer, 123);

                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not connect to NTP Server {0} - Check that UDP Port 123 is not being blocked by something like a firewall...", ntpServer.ToString());
                    Console.ResetColor();
                    //returnValue.connect = false;
                }

                if (returnValue.connect)
                {
                    socket.Send(monlistdata); //Send the NTP Time Request.

                    try
                    {
                        //Attempt to recieve the response from NTP Server
                        socket.Receive(monlistdataReceive);
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("NTP Server {0} is NOT responding to a MON_GETLIST V3, that's great!", ntpServer.ToString());
                        Console.ResetColor();
                    }

                    //Clean up after ourselves and dispose of socket.           
                    socket.Close();

                    if (monlistdataReceive[7] == 72)
                    {
                        
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("NTP Server {0} IS returning Monlist information when MON_GETLIST V3", ntpServer.ToString());
                        Console.ResetColor();
                    }
                    returnValue.monlistV3 = getcount(monlistdataReceive);
                }

            }

            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No NTP response received from NTP Server {0} - Check that UDP Port 123 is not being blocked by something like a firewall...", ntpServer.ToString());
                Console.ResetColor();
                Console.WriteLine("Press Any key to exit.");
                return returnValue;
            }
            socket.Close();
            return returnValue;
        }
    }
}
