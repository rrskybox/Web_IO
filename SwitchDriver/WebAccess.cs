using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASCOM.Web_IO.Switch
{
    internal partial class WebAccess
    {
        static string web_IP_address;
        static string web_Page;
        static HttpClient web_Client;

        public WebAccess(string ip_address)
        {
            web_IP_address = ip_address;
            web_Page = IO_Codes.Web_IO_Page;
            web_Client = new HttpClient();
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
        }

        public bool Connect()
        {
            //This call is a no-op in that the target server does not breath
            return true;
        }

        public bool Disconnect()
        {
            //This call is a no-op in that the target server does not breath
            return true;
        }

        public bool CheckConnection()
        {
            //This call is a no-op in that the target server does not breath
            return true;
        }

        public void AccessPage(string content)
        {
            WriteSwitchState(content);
            ReadSwitchStates();
        }

        public void AccessPage()
        {
            ReadSwitchStates();
        }

        private void WriteSwitchState(string content)
        {
            var task = Task.Run(() => WriteSwitchAsync(content));
            task.Wait();
            string nada = task.Result;
        }

        private async static Task<string> WriteSwitchAsync(string content)
        {
            string query = "http://" + web_IP_address + "/" + web_Page + "/" + content;
            string hzResultText = await web_Client.GetStringAsync(query);
            return null;
        }

        private void ReadSwitchStates()
        {
            var task = Task.Run(() => ParsePageAsync());
            task.Wait();
            SwitchHardware.switchState = task.Result;
        }

        private async static Task<bool?[]> ParsePageAsync()
        {
            string hzPage1Text = null;
            string hzPage2Text = null;
            string page1Query = "http://" + web_IP_address + "/" + web_Page + "/" + IO_Codes.LastPage;
            string page2Query = "http://" + web_IP_address + "/" + web_Page + "/" + IO_Codes.NextPage;

            Task<string> readHttp1 = web_Client.GetStringAsync(page1Query);
            hzPage1Text = await readHttp1;
            Task<string> readHttp2 = web_Client.GetStringAsync(page2Query);
            hzPage2Text = await readHttp2;
            string switchResponse = hzPage1Text + "\r\n\r\n" + hzPage2Text;
            bool?[] switchState = SwitchStatus(hzPage1Text + hzPage2Text);
            return switchState;
        }

        public static bool?[] SwitchStatus(string queryResults)
        {
            //Parses the Http page return from Web_IO to interpret href links as current switch states
            //  That is, the reference link for the on/off command buttons (text) is the url string for turning
            //  the respective relay off or on.  So, if the relay is off, then the HTTP href will be the
            //  the command to turn it on, and vice versa.  You just have to dig it out for each Relay.

            //Array for reading in two digit command strings, by Relay: true = on, false = off
            bool?[] switchArray = new bool?[8];
            //target string for matching command string in Http response
            string targetString = web_IP_address + "/" + web_Page + "/";
            int targetIndex = 0;
            //hopscotch through the response.  Find a matching instance, scrape off the cammand code
            // at the end, and set the respective switch array value to true or false,
            // true 
            while (targetIndex != -1)
            {
                //find the start of the next occurance of "http://ip address/30000/" from the current position of
                //  the indexer.  The "/" is the key in that other occurances will not have the ending "/"
                targetIndex = queryResults.IndexOf(targetString, targetIndex);
                //Pick up the substring following the "/", which would be the two charactor
                //  string whose location is the indexer + the length of the comparison string
                if (targetIndex != -1)
                {
                    targetIndex += targetString.Length;
                    string ss = queryResults.Substring(targetIndex, 2);
                    switch (ss)
                    {
                        case "00":
                            switchArray[0] = true;
                            break;
                        case "01":
                            switchArray[0] = false;
                            break;
                        case "02":
                            switchArray[1] = true;
                            break;
                        case "03":
                            switchArray[1] = false;
                            break;
                        case "04":
                            switchArray[2] = true;
                            break;
                        case "05":
                            switchArray[2] = false;
                            break;
                        case "06":
                            switchArray[3] = true;
                            break;
                        case "07":
                            switchArray[3] = false;
                            break;
                        case "08":
                            switchArray[4] = true;
                            break;
                        case "09":
                            switchArray[4] = false;
                            break;
                        case "10":
                            switchArray[5] = true;
                            break;
                        case "11":
                            switchArray[5] = false;
                            break;
                        case "12":
                            switchArray[6] = true;
                            break;
                        case "13":
                            switchArray[6] = false;
                            break;
                        case "14":
                            switchArray[7] = true;
                            break;
                        case "15":
                            switchArray[7] = false;
                            break;
                        default:
                            break;
                    }
                }
            }
            return switchArray;
        }
    }
}

