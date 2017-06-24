using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _0525_3skeleton
{
    class SPBluetooth : System.IO.Ports.SerialPort
    {
        private static SPBluetooth instance;

        public static SPBluetooth GetInsntace(){
            if (instance == null)
                instance = new SPBluetooth();

            return instance;
        }

        public string[] GetPorts() { 
           return GetPortNames();
        }

    }
}
