//tabs=4

//' --------------------------------------------------------------------------------
//' ASCOM Switch Driver for Web_IO 16 channel GPIO module with Ethernet IP interface
//'
//'    Specifically configured for the Kilolani Observatory power management system
//'       (See SwitchCnfg.vb for configuration details)
//'
//' Description:	This is an ASCOM SwitchV2 driver configured for running a specific
//'               hardwere instance of the Web_IO 16 channel Ethernet card.  
//'
//' Implements:	ASCOM Switch interface version: 2.0
//' Author:		(REM) Rick McAlister, rrskybox@yahoo.com
//'
//' Edit Log:     Rev 1.0 Initial Version
//'
//' Date			Who	Vers	Description
//' -----------	---	-----	-------------------------------------------------------
//' 09-23-2017	REM	1.0.0	Port from VB version
//' ---------------------------------------------------------------------------------
//'
//' The driver's ID is ASCOM.Web_IOrEnetGPIO.Switch
//'
//' The Guid attribute sets the CLSID for ASCOM.DeviceName.Switch
//' The ClassInterface/None addribute prevents an empty interface called
//' _Switch from being created and used as the [default] interface
//'

//' This definition is used to select code that's only applicable for one device type

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using ASCOM.Web_IO.Switch;
using System.Net;
using ASCOM.Astrometry.NOVASCOM;
using ASCOM.Utilities.Exceptions;

namespace ASCOM.Web_IO.Switch
{
    /// <summary>
    /// ASCOM Switch hardware class for Web_IO.
    /// </summary>
    [HardwareClass()] // Class attribute flag this as a device hardware class that needs to be disposed by the local server when it exits.
    internal static class SwitchHardware
    {
        // Constants used for Profile persistence
        internal const string Web_IOIPAddressProfileName = "IP Address";
        internal const string Web_IOIPAddressDefault = "192.168.1.4";
        internal const string traceStateProfileName = "Trace Level";
        internal const string traceStateDefault = "true";

        private static string DriverProgId = ""; // ASCOM DeviceID (COM ProgID) for this driver, the value is set by the driver's class initializer.
        private static string DriverDescription = ""; // The value is set by the driver's class initializer.
        private static bool connectedState; // Local server's connected state
        private static bool runOnce = false; // Flag to enable "one-off" activities only to run once.
        internal static Util utilities; // ASCOM Utilities object for use as required
        internal static AstroUtils astroUtilities; // ASCOM AstroUtilities object for use as required
        internal static TraceLogger tl; // Local server's trace logger object for diagnostic log with information that you specify

        internal static string web_IOIPAddress; // Web_IO device IP
        internal static WebAccess web_Access;  //Http driver

        internal static bool?[] switchState = new bool?[IO_Codes.Switch_Channels];

        internal static string? switchResponse = null;

        /// <summary>
        /// Initializes a new instance of the device Hardware class.
        /// </summary>
        static SwitchHardware()
        {
            try
            {
                // Create the hardware trace logger in the static initializer.
                // All other initialisation should go in the initializeHardware method.
                tl = new TraceLogger("", "Web_IO.Hardware");

                // DriverProgId has to be set here because it used by ReadProfile to get the TraceState flag.
                DriverProgId = Switch.DriverProgId; // Get this device's ProgID so that it can be used to read the Profile configuration values

                // ReadProfile has to go here before anything is written to the log because it loads the TraceLogger enable / disable state.
                ReadProfile(); // Read device configuration from the ASCOM Profile store, including the trace state

                LogMessage("SwitchHardware", $"Static initializer completed.");
            }
            catch (Exception ex)
            {
                try { LogMessage("SwitchHardware", $"Initialisation exception: {ex}"); } catch { }
                MessageBox.Show($"{ex.Message}", "Exception creating ASCOM.Web_IO.Switch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Place device initialisation code here
        /// </summary>
        /// <remarks>Called every time a new instance of the driver is created.</remarks>
        internal static void InitializeHardware()
        {
            // This method will be called every time a new ASCOM client loads your driver
            LogMessage("initializeHardware", $"Start.");

            // Make sure that "one off" activities are only undertaken once
            if (runOnce == false)
            {
                LogMessage("initializeHardware", $"Starting one-off initialisation.");

                DriverDescription = Switch.DriverDescription; // Get this device's Chooser description

                LogMessage("initializeHardware", $"ProgID: {DriverProgId}, Description: {DriverDescription}");

                connectedState = false; // initialize connected to false
                utilities = new Util(); //initialize ASCOM Utilities object
                astroUtilities = new AstroUtils(); // initialize ASCOM Astronomy Utilities object

                LogMessage("initializeHardware", "Completed basic initialisation");

                // Add your own "one off" device initialisation here e.g. validating existence of hardware and setting up communications

                LogMessage("initializeHardware", $"One-off initialisation complete.");
                runOnce = true; // Set the flag to ensure that this code is not run again

            }
        }

        // PUBLIC COM INTERFACE ISwitchV2 IMPLEMENTATION

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialogue form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public static void SetupDialog()
        {
            // Don't permit the setup dialogue if already connected
            if (IsConnected)
                MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm(tl))
            {
                var result = F.ShowDialog();
                if (result == DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        /// <summary>Returns the list of custom action names supported by this driver.</summary>
        /// <value>An ArrayList of strings (SafeArray collection) containing the names of supported actions.</value>
        public static ArrayList SupportedActions
        {
            get
            {
                LogMessage("SupportedActions Get", "Returning Supported Actions List");
                ArrayList sList = new ArrayList();
                foreach (string s in IO_Codes.Supported)
                    sList.Add(s);
                return sList;
            }
        }

        /// <summary>Invokes the specified device-specific custom action.</summary>
        /// <param name="ActionName">A well known name agreed by interested parties that represents the action to be carried out.</param>
        /// <param name="ActionParameters">List of required parameters or an <see cref="String.Empty">Empty String</see> if none are required.</param>
        /// <returns>A string response. The meaning of returned strings is set by the driver author.
        /// <para>Suppose filter wheels start to appear with automatic wheel changers; new actions could be <c>QueryWheels</c> and <c>SelectWheel</c>. The former returning a formatted list
        /// of wheel names and the second taking a wheel name and making the change, returning appropriate values to indicate success or failure.</para>
        /// </returns>
        public static string Action(string actionName, string actionParameters)
        {
            LogMessage("Action", $"Action {actionName}, parameters {actionParameters} is not implemented");
            throw new ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and does not wait for a response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        public static void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // TODO The optional CommandBlind method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandBlind must send the supplied command to the mount and return immediately without waiting for a response

            throw new MethodNotImplementedException($"CommandBlind - Command:{command}, Raw: {raw}.");
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and waits for a boolean response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        /// <returns>
        /// Returns the interpreted boolean response received from the device.
        /// </returns>
        public static bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            // TODO The optional CommandBool method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandBool must send the supplied command to the mount, wait for a response and parse this to return a True or False value

            throw new MethodNotImplementedException($"CommandBool - Command:{command}, Raw: {raw}.");
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and waits for a string response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        /// <returns>
        /// Returns the string response received from the device.
        /// </returns>
        public static string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // TODO The optional CommandString method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandString must send the supplied command to the mount and wait for a response before returning this to the client

            throw new MethodNotImplementedException($"CommandString - Command:{command}, Raw: {raw}.");
        }

        /// <summary>
        /// Deterministically release both managed and unmanaged resources that are used by this class.
        /// </summary>
        /// <remarks>
        /// TODO: Release any managed or unmanaged resources that are used in this class.
        /// 
        /// Do not call this method from the Dispose method in your driver class.
        ///
        /// This is because this hardware class is decorated with the <see cref="HardwareClassAttribute"/> attribute and this Dispose() method will be called 
        /// automatically by the  local server executable when it is irretrievably shutting down. This gives you the opportunity to release managed and unmanaged 
        /// resources in a timely fashion and avoid any time delay between local server close down and garbage collection by the .NET runtime.
        ///
        /// For the same reason, do not call the SharedResources.Dispose() method from this method. Any resources used in the static shared resources class
        /// itself should be released in the SharedResources.Dispose() method as usual. The SharedResources.Dispose() method will be called automatically 
        /// by the local server just before it shuts down.
        /// 
        /// </remarks>
        public static void Dispose()
        {
            try { LogMessage("Dispose", $"Disposing of assets and closing down."); } catch { }

            try
            {
                // Clean up the trace logger and utility objects
                tl.Enabled = false;
                tl.Dispose();
                tl = null;
            }
            catch { }

            try
            {
                utilities.Dispose();
                utilities = null;
            }
            catch { }

            try
            {
                astroUtilities.Dispose();
                astroUtilities = null;
            }
            catch { }
        }

        /// <summary>
        /// Set True to connect to the device hardware. Set False to disconnect from the device hardware.
        /// You can also read the property to check whether it is connected. This reports the current hardware state.
        /// </summary>
        /// <value><c>true</c> if connected to the hardware; otherwise, <c>false</c>.</value>
        public static bool Connected
        {
            get
            {
                LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    web_Access = new WebAccess(IO_Codes.Web_IO_IP);
                    web_Access.Connect();
                    LogMessage("Connected Set", "Connecting to Web_IO", web_IOIPAddress);
                    connectedState = true;
                }
                else
                {
                    connectedState = false;
                    LogMessage("Connected Set", "Disconnecting from Web_IO", web_IOIPAddress);
                    if (web_Access.Disconnect())
                    {
                        LogMessage("Connected Set", "Disconnected to Web_IO " + web_IOIPAddress);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a description of the device, such as manufacturer and model number. Any ASCII characters may be used.
        /// </summary>
        /// <value>The description.</value>
        public static string Description
        {
            // TODO customise this device description if required
            get
            {
                LogMessage("Description Get", DriverDescription);
                return DriverDescription;
            }
        }

        /// <summary>
        /// Descriptive and version information about this ASCOM driver.
        /// </summary>
        public static string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description if required
                string driverInfo = $"Information about the driver itself. Version: {version.Major}.{version.Minor}";
                LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        /// <summary>
        /// A string containing only the major and minor version of the driver formatted as 'm.n'.
        /// </summary>
        public static string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = $"{version.Major}.{version.Minor}";
                LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        /// <summary>
        /// The interface version number that this device supports.
        /// </summary>
        public static short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "2");
                return Convert.ToInt16("2");
            }
        }

        /// <summary>
        /// The short name of the driver, for display purposes
        /// </summary>
        public static string Name
        {
            // TODO customise this device name as required
            get
            {
                string name = "Short driver name - please customise";
                LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ISwitchV2 Implementation

        //private static int numSwitch = IO_Codes.Switch_Channels;

        /// <summary>
        /// The number of switches managed by this driver
        /// </summary>
        public static short MaxSwitch
        {
            get
            {
                tl.LogMessage("MaxSwitch Get", (IO_Codes.Switch_Channels).ToString());
                return IO_Codes.Switch_Channels;
            }
        }

        /// <summary>
        /// Return the name of switch n
        /// </summary>
        /// <param name="id">The switch number to return</param>
        /// <returns>
        /// The name of the switch
        /// </returns>
        public static string GetSwitchName(short id)
        {
            Validate("GetSwitchName", id);
            tl.LogMessage("GetSwitchName", "Acquired " + IO_Codes.SwitchNames[id]);
            return (IO_Codes.SwitchNames[id]);
        }

        /// <summary>
        /// Sets a switch name to a specified value
        /// </summary>
        /// <param name="id">The number of the switch whose name is to be set</param>
        /// <param name="name">The name of the switch</param>
        public static void SetSwitchName(short id, string name)
        {
            Validate("SetSwitchName", id);
            tl.LogMessage("SetSwitchName", string.Format("SetSwitchName({0}) = {1}", id, name));
            IO_Codes.SwitchNames[id] = name;
        }

        /// <summary>
        /// Gets the switch description.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public static string GetSwitchDescription(short id)
        {
            Validate("GetSwitchDescription", id);
            tl.LogMessage("GetSwitchDescription", "Acquired " + IO_Codes.SwitchDescription[id]);
            return (IO_Codes.SwitchDescription[id]);
        }

        /// <summary>
        /// Reports if the specified switch can be written to.
        /// This is false if the switch cannot be written to, for example a limit switch or a sensor.
        /// The default is true.
        /// </summary>
        /// <param name="id">The number of the switch whose write state is to be returned</param><returns>
        ///   <c>true</c> if the switch can be written to, otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
        /// <exception cref="InvalidValueException">If id is outside the range 0 to MaxSwitch - 1</exception>
        public static bool CanWrite(short id)
        {
            Validate("CanWrite", id);
            tl.LogMessage("CanWrite", String.Format("Reported: {0}", IO_Codes.SwitchWrite[id]));
            return (IO_Codes.SwitchWrite[id]);
        }

        #endregion

        #region boolean switch members

        /// <summary>
        /// Return the state of switch n
        /// a multi-value switch must throw a not implemented exception
        /// </summary>
        /// <param name="id">The switch number to return</param>
        /// <returns>
        /// True or false
        /// </returns>
        public static bool GetSwitch(short id)
        {
            Validate("GetSwitch", id);
            tl.LogMessage("GetSwitch", String.Format("Acquired {0}", switchState[id] ?? false));
            web_Access.AccessPage();
            return switchState[id] ?? false;
        }


        /// <summary>
        /// Sets a switch to the specified state
        /// If the switch cannot be set then throws a MethodNotImplementedException.
        /// A multi-value switch must throw a not implemented exception
        /// setting it to false will set it to its minimum value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        public static void SetSwitch(short id, bool state)
        {
            Validate("SetSwitch", id);
            if (!CanWrite(id))
            {
                var str = string.Format("SetSwitch({0}) - Cannot Write", id);
                tl.LogMessage("SetSwitch", str);
                throw new MethodNotImplementedException(str);
            }
            tl.LogMessage("SetSwitch", Convert.ToString(switchState[id]));
            if (state)
                switch (id)
                {
                    case 0:
                        web_Access.AccessPage(IO_Codes.Relay_00_On);
                        break;
                    case 1:
                        web_Access.AccessPage(IO_Codes.Relay_01_On);
                        break;
                    case 2:
                        web_Access.AccessPage(IO_Codes.Relay_02_On);
                        break;
                    case 3:
                        web_Access.AccessPage(IO_Codes.Relay_03_On);
                        break;
                    case 4:
                        web_Access.AccessPage(IO_Codes.Relay_04_On);
                        break;
                    case 5:
                        web_Access.AccessPage(IO_Codes.Relay_05_On);
                        break;
                    case 6:
                        web_Access.AccessPage(IO_Codes.Relay_06_On);
                        break;
                    case 7:
                        web_Access.AccessPage(IO_Codes.Relay_07_On);
                        break;
                    default:
                        throw new InvalidValueException(id.ToString());
                }
            else switch (id)
                {
                    case 0:
                        web_Access.AccessPage(IO_Codes.Relay_00_Off);
                        break;
                    case 1:
                        web_Access.AccessPage(IO_Codes.Relay_01_Off);
                        break;
                    case 2:
                        web_Access.AccessPage(IO_Codes.Relay_02_Off);
                        break;
                    case 3:
                        web_Access.AccessPage(IO_Codes.Relay_03_Off);
                        break;
                    case 4:
                        web_Access.AccessPage(IO_Codes.Relay_04_Off);
                        break;
                    case 5:
                        web_Access.AccessPage(IO_Codes.Relay_05_Off);
                        break;
                    case 6:
                        web_Access.AccessPage(IO_Codes.Relay_06_Off);
                        break;
                    case 7:
                        web_Access.AccessPage(IO_Codes.Relay_07_Off);
                        break;
                    default:
                        throw new InvalidValueException(id.ToString());
                }
        }

        #endregion

        #region analogue members

        /// <summary>
        /// returns the maximum value for this switch
        /// boolean switches must return 1.0
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static double MaxSwitchValue(short id)
        {
            Validate("MaxSwitchValue", id);
            // boolean switch implementation:
            return 1;
            // or
            //tl.LogMessage("MaxSwitchValue", string.Format("MaxSwitchValue({0}) - not implemented", id));
            //throw new MethodNotImplementedException("MaxSwitchValue");
        }

        /// <summary>
        /// returns the minimum value for this switch
        /// boolean switches must return 0.0
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static double MinSwitchValue(short id)
        {
            Validate("MinSwitchValue", id);
            // boolean switch implementation:
            return 0;
            // or
            //tl.LogMessage("MinSwitchValue", string.Format("MinSwitchValue({0}) - not implemented", id));
            //throw new MethodNotImplementedException("MinSwitchValue");
        }

        /// <summary>
        /// returns the step size that this switch supports. This gives the difference between
        /// successive values of the switch.
        /// The number of values is ((MaxSwitchValue - MinSwitchValue) / SwitchStep) + 1
        /// boolean switches must return 1.0, giving two states.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static double SwitchStep(short id)
        {
            Validate("SwitchStep", id);
            // boolean switch implementation:
            return 1;
            // or
            //tl.LogMessage("SwitchStep", string.Format("SwitchStep({0}) - not implemented", id));
            //throw new MethodNotImplementedException("SwitchStep");
        }

        /// <summary>
        /// returns the analogue switch value for switch id
        /// boolean switches must throw a not implemented exception
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static double GetSwitchValue(short id)
        {
            Validate("GetSwitchValue", id);
            int val;
            if (GetSwitch(id))
                val = 1;
            else
                val = 0;

            tl.LogMessage("GetSwitchValue", string.Format("Switch Value = {0} for channel {1}", val.ToString(), id));
            return val;
        }

        /// <summary>
        /// set the analogue value for this switch.
        /// If the switch cannot be set then throws a MethodNotImplementedException.
        /// If the value is not between the maximum and minimum then throws an InvalidValueException
        /// boolean switches must throw a not implemented exception except that the spec says you cannot
        /// throw a notimplementedexception unless CanWrite is false.  So... this is screwed up.
        /// I've decided on a convention where SetSwitchValue will set a 1 if any value other than 0 is set.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public static void SetSwitchValue(short id, double value)
        {
            Validate("SetSwitchValue", id, value);
            if (!CanWrite(id))
            {
                tl.LogMessage("SetSwitchValue", string.Format("SetSwitchValue({0}) - Cannot write", id));
                throw new ASCOM.MethodNotImplementedException(string.Format("SetSwitchValue({0}) - Cannot write", id));
            }
            tl.LogMessage("SetSwitchValue", string.Format("SetSwitchValue({0}) = {1} (e.g. 0 or not 0)", id, value));
            if (value != 0)
                SetSwitch(id, true);
            else
                SetSwitch(id, false);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Checks that the switch id is in range and throws an InvalidValueException if it isn't
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="id">The id.</param>
        private static void Validate(string message, short id)
        {
            if (id < 0 || id >= IO_Codes.Switch_Channels)
            {
                LogMessage(message, string.Format("Switch {0} not available, range is 0 to {1}", id, IO_Codes.Switch_Channels - 1));
                throw new InvalidValueException(message, id.ToString(), string.Format("0 to {0}", IO_Codes.Switch_Channels - 1));
            }
        }

        /// <summary>
        /// Checks that the switch id and value are in range and throws an
        /// InvalidValueException if they are not.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="id">The id.</param>
        /// <param name="value">The value.</param>
        private static void Validate(string message, short id, double value)
        {
            Validate(message, id);
            var min = MinSwitchValue(id);
            var max = MaxSwitchValue(id);
            if (value < min || value > max)
            {
                LogMessage(message, string.Format("Value {1} for Switch {0} is out of the allowed range {2} to {3}", id, value, min, max));
                throw new InvalidValueException(message, value.ToString(), string.Format("Switch({0}) range {1} to {2}", id, min, max));
            }
        }

        #endregion

        #region Private properties and methods
        // Useful methods that can be used as required to help with driver development

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private static bool IsConnected
        {
            get
            {
                if (connectedState)
                {
                    if (web_Access.CheckConnection())
                    {
                        connectedState = true;
                    }
                    else
                    {
                        connectedState = false;
                    }
                }
                return (connectedState);
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private static void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal static void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Switch";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, traceStateProfileName, string.Empty, traceStateDefault));
                web_IOIPAddress = driverProfile.GetValue(DriverProgId, Web_IOIPAddressProfileName, string.Empty, Web_IOIPAddressDefault);
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal static void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Switch";
                driverProfile.WriteValue(DriverProgId, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(DriverProgId, Web_IOIPAddressProfileName, web_IOIPAddress.ToString());
            }
        }

        /// <summary>
        /// Log helper function that takes identifier and message strings
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        internal static void LogMessage(string identifier, string message)
        {
            tl.LogMessageCrLf(identifier, message);
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            LogMessage(identifier, msg);
        }
        #endregion
    }
}