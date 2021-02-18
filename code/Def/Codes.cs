using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Services.Drv.Emera.Def
{
    public class Codes
    {
        /// <summary> Байт начала и конца пакета обмена </summary>
        public const byte BYTE_END = 0xC0;
        /// <summary> Байт символа ESC </summary>
        public const byte BYTE_ESC = 0xDB;
        /// <summary> Байт символа замены в пакете для BYTE_END </summary>
        public const byte BYTE_REPLACE_END = 0xDC;
        /// <summary> Байт символа замены в пакете для BYTE_ESC </summary>
        public const byte BYTE_REPLACE_ESC = 0xDD;
        /// <summary> Байт 16-битной адресации и 8-битного цикла </summary>
        public const byte BYTE_OPT = 0x48;
        /// <summary> Сервисное поле запроса без данных </summary>
        public const byte BYTE_SERV = 0xD0;
        /// <summary> Сервисное поле с ошибкой в ответе </summary>
        public const byte BYTE_ERR = 0x70;
        /// <summary> Сервисное поле с нормальным ответом </summary>
        public const byte BYTE_SERV_ANSWER = 0x50;

        public const ushort CODE_READ_KOEF_CONFIG = 0x0101;
        public const ushort CODE_READ_SERIAL_NUMBER = 0x011A;
        public const ushort CODE_READ_DATETIME = 0x0120;
        public const ushort CODE_WRITE_DATETIME = 0x0121;
        public const ushort CODE_READ_CONFIG = 0x0128;
        public const ushort CODE_READ_ENERGY_MONTH_TARIFF = 0x0130;
        public const ushort CODE_READ_ENERGY_MONTH_SUM = 0x0131;
        public const ushort CODE_READ_ENERGY_DAY_SUM = 0x012F;
        public const ushort CODE_READ_ENERGY_DAY_TARIFF = 0x0133;
        public const ushort CODE_READ_ENERGY_INTERVAL = 0x0134;
        public const ushort CODE_READ_POWER_CURR = 0x0132;
        public const ushort CODE_READ_POWER_3MIN = 0x012E;
    }
}