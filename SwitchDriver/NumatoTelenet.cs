////Class of methods and properties for the Numato telenet communications
//// protocol layer.  An instantiation of the class represents one socket
//// connection to the device.  Configuration parameters are contained in
//// the NumatoIOCodes module
////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;

namespace ASCOM.NumatoEnetGPIO.Switch
{
    class NumatoTelenet
    {

        public System.Net.Sockets.TcpClient TelnetClient;
        public System.Net.Sockets.NetworkStream TelenetStream;

        internal int Numato_Port = 23;
        internal string Numato_Config = "00ff"; //'Chnl 0-7 input, chnl 8-15 output (relays)

        public NumatoTelenet()
        {
            return;
        }

        public bool CheckConnection()
        ////Determines whether a socket is open to the device
        //// returns true if ( connected, false otherwise
        {
            if (TelnetClient == null)
            {
                return (false);
            }
            else
            {
                return (true);
            }
        }

        public void SetConfiguration()
        ////Sets the I/O states for the Numato controller
        ////  Clears the input queue then sends the configuration
        ////  parameters from NumatoIOcodes, which, unless modif (ied
        ////  sets the first 8 channels for digital input and the second
        ////  eight channels for digital output for relay control.
        {
            Telnet_Clear();
            Telnet_Send("gpio iodir" + " " + Numato_Config);
            string switchrcv = Telnet_Receive();
            return;
        }

        public bool Connect(string ipaddress)
        ////Function attempts to create a socket connection to the Numato device.
        //// return;s true if ( successful, false otherwise
        {
            string tn_receive;

            TelnetClient = new System.Net.Sockets.TcpClient();
            try
            {
                TelnetClient.Connect(ipaddress, (int)Numato_Port);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return (false);
            }
            //Set up input stream
            TelenetStream = TelnetClient.GetStream();
            tn_receive = Telnet_Receive();

            //Sign on as admin twice to clear log on queue -- ignor results and clear input stream
            Telnet_Send("admin");
            tn_receive = Telnet_Receive();

            Telnet_Send("admin");
            tn_receive = Telnet_Receive();
            Telnet_Clear();
            return (true);
        }

        public bool Disconnect()
        {
            TelnetClient = null;
            TelenetStream = null;
            return (true);
        }

        public bool GetChannel(int channelnumber)
        //Checks a specif (ic channel for its on/off state.  returns True if ( on, false if ( anything } else {.
        // Requires parsing the response to  
        //Note: had to modif (y this to use gpio readall.  gpio read x will reset channel X (firmware bug, I suppose)
        {
            Telnet_Clear();

            Telnet_Send("gpio readall");
            string switchrcv = Telnet_Receive();
            if (switchrcv == null)
            {
                return (false);
            }
            if (switchrcv.Length != 4)
            {
                return (false);
            }
            //switchrcv will be a 4 digit hex number for 16 channels
            // convert to unsigned integer then bitwise and to a logical array
            ushort allChState = Convert.ToUInt16(switchrcv, 16);
            bool[] chArray = new bool[16];
            ushort bitPlace = 1;
            for (int i = 0; i < 16; i++)
            {
                if ((allChState & bitPlace) != 0)
                    chArray[i] = true;
                else
                    chArray[i] = false;
                bitPlace = (ushort)(bitPlace << 1);
            }
            return (chArray[channelnumber]);

            //MessageBox.Show("dd: " + switchrcv + "  " + allChState);
            //string switchrcvlow = switchrcv.Substring(2, 2);
            //int switchintlow = Convert.ToInt32(switchrcvlow, 16);
            //string switchstrlow = Convert.ToString(switchintlow, 2);
            //do
            //{
            //    switchstrlow = "0" + switchstrlow;
            //} while (switchstrlow.Length < 8);
            //string switchrcvhigh = switchrcv.Substring(0, 2);
            //int switchinthigh = Convert.ToInt32(switchrcvhigh, 16);
            //string switchstrhigh = Convert.ToString(switchinthigh, 2);
            //do
            //{
            //    switchstrhigh = "0" + switchstrhigh;
            //} while (switchstrhigh.Length < 8);
            //if (channelnumber < 8)
            //{
            //    if (switchstrlow[7 - channelnumber] == '1')
            //    {
            //        return (true);
            //    }
            //    else
            //    {
            //        return (false);
            //    }
            //}
            //else
            //{
            //    if (switchstrhigh[15 - channelnumber] == '1')
            //    {
            //        return (true);
            //    }
            //    else
            //    {
            //        return (false);
            //    }
            //}
        }

        public void ChannelOn(int channelnumber)
        //Turns a specif (ic channel on, if ( possible
        {
            char chnlchar;
            Telnet_Clear();
            if (channelnumber < 10)
            {
                chnlchar = (char)(channelnumber + 48);
            }
            else
            {
                chnlchar = (char)(channelnumber + 55);
            }
            Telnet_Send("gpio set " + chnlchar);
            return;
        }

        public void ChannelOff(int channelnumber)
        //Turns a specif (ic channel off, if ( possible
        {
            char chnlchar;
            Telnet_Clear();

            if (channelnumber < 10)
            {
                chnlchar = (char)(channelnumber + 48);
            }
            else
            {
                chnlchar = (char)(channelnumber + 55);
            }
            Telnet_Send("gpio clear " + chnlchar);
            Telnet_Clear();
            return;
        }

        public void Telnet_Clear()
        //Empties the receive buffer/queue
        {
            string tn_receive;
            while (true)
            {
                Telnet_Send("");
                tn_receive = Telnet_Receive();
                if (tn_receive == "")
                {
                    return;
                }
            }
        }

        public string Telnet_Receive()
        //Uses the Telnet protocol to receive byte information from the device
        {
            string rstring = "";
            int rcommand;
            int roption;
            byte[] rbytes = new byte[TelnetClient.ReceiveBufferSize];
            if (TelenetStream.CanWrite && TelenetStream.CanRead)
            {
                int bCount = TelenetStream.Read(rbytes, 0, (int)(TelnetClient.ReceiveBufferSize));
                int i = 0;
                while (i < bCount && rbytes[i] != 0)
                {
                    switch (rbytes[i])
                    {
                        case 255: //Telnet Command: get command and option
                            rcommand = rbytes[i + 1];
                            roption = rbytes[i + 2];
                            i += 2;
                            if (roption != 45)
                                System.Windows.Forms.MessageBox.Show("what the ...");
                            else
                                Handle_Command((byte)roption);
                            break;
                        case 27://ESC code:
                            i++;//Should be a open bracket here
                            i++; //first code
                            if (rbytes[i] == 50)//gonna be a "2J"
                                while (i < bCount && rbytes[i] != 109)
                                    i++; //walk through the string until = "m"
                            break;
                        case 10:
                            break;
                        case 13:
                            break;
                        case 62:
                            break;
                        default:
                            rstring += Convert.ToChar(rbytes[i]);
                            break;
                    }
                    i++;
                }
            }
            return (rstring);
        }

        public void Telnet_Send(string sstring)
        //Uses the Telnet protocol the send byte information to the device
        {
            byte[] telnetbytes = new byte[100];
            sstring = sstring + "\r\n";
            char[] schars = sstring.ToCharArray();

            int i = 0;
            //For thing = 0 To Len(passwordbytes) - 1
            foreach (char sc in schars)
            {
                telnetbytes[i] = (byte)sc;
                i += 1;
            }
            if (TelenetStream.CanWrite && TelenetStream.CanRead)
            {
                TelenetStream.Write(telnetbytes, 0, schars.Length);
            }
            System.Threading.Thread.Sleep(200);
            return;
        }

        public void Telnet_Cmd_Send(byte sendcmd, byte sendbyte)
        //Uses Telnet protocol to send a specif (ic command and byte to the device
        {
            if (TelenetStream.CanWrite && TelenetStream.CanRead)
            {
                byte[] telnetbytes = new byte[100];
                telnetbytes[0] = 255;
                telnetbytes[1] = sendcmd;
                telnetbytes[2] = sendbyte;
                TelenetStream.Write(telnetbytes, 0, 3);
            }
            return;
        }

        public void Handle_Command(byte toption)
        //Uses Telenet protocol to do something
        {
            byte TELNET_WILL = 251;
            Telnet_Cmd_Send(TELNET_WILL, toption);
            return;
        }

        //public void TestNumato(string ipaddress)
        ////Utility to exercise the device Telnet layer
        //{
        //    Connect(ipaddress);

        //    string tn_receive = Telnet_Receive();
        //    System.Windows.Forms.MessageBox.Show(tn_receive);

        //    Telnet_Send("admin");
        //    tn_receive = Telnet_Receive();
        //    //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //    Telnet_Send("admin");
        //    tn_receive = Telnet_Receive();
        //    //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //    Telnet_Send("ver");
        //    tn_receive = Telnet_Receive();
        //    //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //    Telnet_Send("gpio readall");
        //    tn_receive = Telnet_Receive();
        //    //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //    int i;
        //    char chnlchar;

        //    while (true)
        //    {
        //        for (i = 0; i < 7; i++)
        //        {
        //            if (i < 2)
        //            {
        //                chnlchar = (char)(i + 56);
        //            }
        //            else
        //            {
        //                chnlchar = (char)(i + 63);
        //            }

        //            Telnet_Send("gpio clear " + chnlchar);

        //            tn_receive = Telnet_Receive();

        //            //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //            Telnet_Send("gpio readall");
        //            tn_receive = Telnet_Receive();
        //            //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //            Telnet_Send("gpio set " + chnlchar);
        //            tn_receive = Telnet_Receive();
        //            //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //            Telnet_Send("gpio readall");
        //            tn_receive = Telnet_Receive();
        //            //MsgBox(tn_receive, MsgBoxStyle.OkOnly)

        //        }
        //        return;
        //}

    }
}


