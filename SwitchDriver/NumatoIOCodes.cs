

namespace ASCOM.NumatoEnetGPIO.Switch
{
    class IO_Codes
    {
        //'Common constants for I/O codes, commands and channels
        //' for Numato 16 channel ethernet GPIO card
        //' The card communications protocl is Telnet

        public const string Numato_IP = "192.168.1.107";
        public const int Numato_Port = 23;
        public const string Numato_Config = "00ff"; //'Chnl 0-7 input, chnl 8-15 output (relays)
        public const int Numato_Channels = 16;

        public const string Numato_Short_IP = "147"; //Default for address

        public const string N_ver = "ver";
        public const string N_usr_get = "usr get";
        public const string N_usr_set = "usr set admin";
        public const string N_pass_get = "pass get";
        public const string N_pass_set = "pass set admin";

        public const string N_GPIO_set = "gpio set ";
        public const string N_GPIO_clear = "gpio clear ";

        //'public const string N_GPIO_set_8 = "gpio set 8";
        //'public const string N_GPIO_clear_8 = "gpio clear 8";
        //'public const string N_GPIO_read_8 = "gpio read 8";

        //'public const string N_GPIO_set_9 = "gpio clear 9";
        //'public const string N_GPIO_clear_9 = "gpio clear 9";
        //'public const string N_GPIO_read_9 = "gpio read 9";

        //'public const string N_GPIO_set_12 = "gpio set C";
        //'public const string N_GPIO_clear_12 = "gpio clear C";
        //'public const string N_GPIO_read_12 = "gpio read C";

        //'public const string N_GPIO_set_13 = "gpio set D";
        //'public const string N_GPIO_clear_13 = "gpio clear D";
        //'public const string N_GPIO_read_13 = "gpio read D";

        public const string N_GPIO_read = "gpio read ";

        public const string N_GPIO_iomask = "gpio mask ";
        public const string N_GPIO_iodir = "gpio iodir ";

        public const string N_GPIO_readall = "gpio read ";

        public const string N_GPIO_writeall = "gpio writeall ";
        public const string N_GPIO_adc = "adc read ";

        public const int IO_Channel_1 = 0;
        public const int IO_Channel_2 = 1;
        public const int IO_Channel_3 = 2;
        public const int IO_Channel_4 = 3;
        public const int IO_Channel_5 = 4;
        public const int IO_Channel_6 = 5;
        public const int IO_Channel_7 = 6;
        public const int IO_Channel_8 = 7;
    }
}