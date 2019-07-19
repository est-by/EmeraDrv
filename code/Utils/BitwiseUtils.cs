using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Services.Drv.Emera.Utils
{
    internal class BitwiseUtils
    {

        public static byte[] Reverse(byte[] input)
        {
            Array.Reverse(input);
            return input;
        }

        public static int BcdToDec(int bcd)
        {
            int result = 0;
            if (!int.TryParse(string.Format("{0:X}", bcd), out result))
            {
                throw new AppException("Error convert value 0x{0:X} from BCD", bcd);
            }
            return result;
        }

        public static byte DecToBCD(byte dec)
        {
            int high = dec / 10;
            int low = dec % 10;
            return (byte)((high << 4) | low);
        }

    }
}
