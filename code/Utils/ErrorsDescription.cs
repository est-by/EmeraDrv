using System;
using Sys.Services.Drv.Emera.Culture;

namespace Sys.Services.Drv.Emera.Utils
{
    internal class ErrorsDescription
    {
        public static string GetError(byte code)
        {
            switch (code)
            {
                case 0x00: return SR.FUNCTION;
                case 0x01: return SR.INVALID_FORMAT;
                case 0x02: return SR.UNAUTHORIZED;
                case 0x03: return SR.ARGUMENT;
                case 0x04: return SR.NOT_CORRESPOND_TO_CONFIG;
                case 0x05: return SR.OPTOPORT_ERROR;
                case 0x10: return SR.PARAMETER;
                case 0x40: return SR.UNACCEPTABLE_TARIFF;
                default: return string.Empty; 
            }
        }
    }
}
