using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.NumatoEnetGPIO.Switch
{
    class SwitchCfg
    {

        //Configuration file for Kilolani Power Management channels

        public static int SwitchCount = 16;
        public static bool NameChanging = false;

        public static string[] SwitchName = {
            "IO 0",
            "IO 1",
            "IO 2",
            "IO 3",
            "IO 4",
            "IO 5",
            "IO 6",
            "IO 7",
            "Mount AC Power",
            "All Sky AC Power",
            "Camera AC Power",
            "FlatMan AC Power",
            "Fan DC Power",
            "Dew DC Power",
            "Optec DC Power",
            "Dome DC Power"
        };

        public static string[] SwitchDescription =
            {
            "IO 0",
            "IO 1",
            "IO 2",
            "IO 3",
            "IO 4",
            "IO 5",
            "IO 6",
            "IO 7",
            "AC Relay for dedicated PMX+ 48V power supply",
            "AC Relay for dedicated AllSky power supply, if not DC",
            "AC Relay for dedicated STXL 12V power supply",
            "AC Relay for dedicated FlatMan 12V power supply, if not DC",
            "DC Relay for distributed 12V power to PlaneWave Fans",
            "DC Relay for distributed 12V power to Delta Dew Controller",
            "DC Relay for distributed 12V power to Optec Gemini",
            "DC Relay for distributed 12V power to MaxDome II"
        };

        public static int[] SwitchMax = {
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1};

        public static int[] SwitchMin = {
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0,
          0};

        public static int[] SwitchStep = {
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1,
           1};

        public static bool[] SwitchWrite = {
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true};

        public static string[] SwitchCommands =
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


    }
}
