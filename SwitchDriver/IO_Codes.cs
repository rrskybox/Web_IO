using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Web_IO.Switch
{
    internal static class IO_Codes
    {
        //'Common constants for I/O codes, commands and channels
        //' for Numato 16 channel ethernet GPIO card

        public const string Relay_00_Off = "00";
        public const string Relay_00_On = "01";
        public const string Relay_01_Off = "02";
        public const string Relay_01_On = "03";
        public const string Relay_02_Off = "04";
        public const string Relay_02_On = "05";
        public const string Relay_03_Off = "06";
        public const string Relay_03_On = "07";
        public const string Relay_04_Off = "08";
        public const string Relay_04_On = "09";
        public const string Relay_05_Off = "10";
        public const string Relay_05_On = "11";
        public const string Relay_06_Off = "12";
        public const string Relay_06_On = "13";
        public const string Relay_07_Off = "14";
        public const string Relay_07_On = "15";

        public const string NextPage = "42";
        public const string LastPage = "43";

        public const string SetNewIP = "41";
        public const string Enter = "40";


        public const string Web_IO_IP = "192.168.1.4";
        public const string Web_IO_Page = "30000";
        public const int Switch_Channels = 8;

        public static string[] SwitchNames = new string[]
        {
            "Mount",
            "AllSky",
            "Camera/Guider/Filter",
            "FlatMan",
            "Fan",
            "Dew Heater",
            "Focuser/Rotator",
            "Dome"
        };

        public static string[] SwitchDescription =
     {
            "AC Relay for dedicated PMX+ 48V power supply",
            "AC Relay for dedicated AllSky power supply, if not DC",
            "AC Relay for dedicated STXL 12V power supply",
            "AC Relay for dedicated FlatMan 12V power supply, if not DC",
            "DC Relay for distributed 12V power to PlaneWave Fans",
            "DC Relay for distributed 12V power to Delta Dew Controller",
            "DC Relay for distributed 12V power to Optec Gemini",
            "DC Relay for distributed 12V power to MaxDome II"
        };

        public static string[] Supported =
{
            "Dispose",
            "GetSwitch",
            "GetSwitchDescription",
            "GetSwitchName",
            "GetSwitchValue",
            "MaxSwitchValue",
            "MinSwitchValue",
            "SetSwitch",
            "CanWrite",
            "SetSwitchName",
            "SetSwitchValue",
            "SetupDialog",
            "SwitchStep",
            "Connected",
            "Description",
            "DriverInfo",
            "DriverVersion",
            "InterfaceVersion",
            "MaxSwitch",
            "SupportedActions"
        };

        public static string[] NotSupported =
        {
             "Action",
             "CommandBlind",
             "CommandBool",
             "CommandString"
        };

        public static bool[] SwitchWrite = 
            {
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true};

    }
}


