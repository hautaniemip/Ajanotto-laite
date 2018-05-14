using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ajanottolaite
{
    public class SetupData
    {
        public string COMPort;
        public int baudRate;

        // Custom class for transfering data
        public SetupData(string _COMPort, int _baudRate)
        {
            COMPort = _COMPort;
            baudRate = _baudRate;
        }
    }
}
