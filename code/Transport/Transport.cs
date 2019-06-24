#define LogTransmit
//#define TestDepth
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sys.Types.Components;
using System.IO;
using Sys.Services.Drv.Emera.Culture;
using Sys.DataBus.Common;
using System.Diagnostics;
using System.Threading;
using Sys.Async;
using Sys.Services.Components;
using Sys.Diagnostics.Logger;

namespace Sys.Services.Drv.Emera
{
    /// <summary>Представляет реализацию всех запросов к устройству</summary>
    public class Transport
    {
        #region (Const)
        /// <summary>Глубина хранения 3 мнуток</summary>
        public static int GetDepth3Min(Version softVetsion)
        {
            return 1;
        }

#if (TestDepth)
    /// <summary>Глубина хранения получасов</summary>
    public const int Depth30MinDefault = 1*48;
    /// <summary>Глубина хранения суточных</summary>
    public const int DepthDay = 5;
    /// <summary>Глубина месяцев</summary>
    public const int DepthMonth = 5;
    /// <summary>Глубина лет</summary>
    public const int DepthYear = 3;

    /// <summary>Глубина хранения суточных показаний счетчиков</summary>
    public const int DepthDayCounter = 5;
    /// <summary>Глубина хранения месячных показаний счетчиков</summary>
    public const int DepthMonthCounter = 5;
    /// <summary>Глубина хранения годовых показаний счетчиков</summary>
    public const int DepthYearCounter = 5;

    /// <summary>Глубина событий количество</summary>
    public const int DepthEvents = 5;
#else
        /// <summary>Глубина хранения получасов</summary>
        public const int Depth30MinDefault = 5904;
        /// <summary>Глубина хранения суточных</summary>
        public const int DepthDay = 155;
        /// <summary>Глубина месяцев</summary>
        public const int DepthMonth = 36;
        /// <summary>Глубина лет</summary>
        public const int DepthYear = 8;

        /// <summary>Глубина хранения суточных показаний счетчиков</summary>
        public const int DepthDayCounter = 154;
        /// <summary>Глубина хранения месячных показаний счетчиков</summary>
        public const int DepthMonthCounter = 35;
        /// <summary>Глубина хранения годовых показаний счетчиков</summary>
        public const int DepthYearCounter = 7;

        /// <summary>Глубина событий количество</summary>
        public const int DepthEvents = 99;
#endif

        /// <summary>
        /// Таймаут между запросами
        /// </summary>
        public const int TimeOutRequestMSecDeafult = 250;
        /// <summary>
        /// Таймаут ожидания ответа
        /// </summary>
        public const int WaitAnswerMSecDeafult = 10000;
        /// <summary>
        /// Кол-во повторов
        /// </summary>
        public const int CountRepeatDeafult = 2;
        #endregion

        #region (Static)

        #region (Ascii)
        static byte[] AddressToAscii(int address)
        {
            return TextToAscii(address.ToString());
        }

        static byte[] TextToAscii(string text)
        {
            List<byte> result = new List<byte>();
            for (var c = 0; c < text.Length; c++)
            {
                char ch = text[c];
                if (ch == '0') result.Add(ASCIIChars.Zero.ToByte());
                else if (ch == '1') result.Add(ASCIIChars.One.ToByte());
                else if (ch == '2') result.Add(ASCIIChars.Two.ToByte());
                else if (ch == '3') result.Add(ASCIIChars.Three.ToByte());
                else if (ch == '4') result.Add(ASCIIChars.Four.ToByte());
                else if (ch == '5') result.Add(ASCIIChars.Five.ToByte());
                else if (ch == '6') result.Add(ASCIIChars.Six.ToByte());
                else if (ch == '7') result.Add(ASCIIChars.Seven.ToByte());
                else if (ch == '8') result.Add(ASCIIChars.Eight.ToByte());
                else if (ch == '9') result.Add(ASCIIChars.Nine.ToByte());
                else if (ch == '-') result.Add(ASCIIChars.MinusSign.ToByte());
                else if (ch == '.') result.Add(ASCIIChars.FullStop.ToByte());
                else if (ch == ':') result.Add(ASCIIChars.Colon.ToByte());
                else if (ch == '(') result.Add(ASCIIChars.OpenParentheses.ToByte());
                else if (ch == ')') result.Add(ASCIIChars.CloseParentheses.ToByte());
                else if (ch == '*') result.Add(ASCIIChars.Asterisk.ToByte());
                else throw new NotImplementedException();
            }
            return result.ToArray();
        }
        #endregion

        #region (Crc)
        static byte Crc(byte[] bytes, int startIndex, int count)
        {
            byte crc = 0;
            for (int i = startIndex; i < startIndex + count; i++)
            {
                byte item = bytes[i];
                crc = (byte)(crc ^ item);
            }
            return (byte)(crc & 0x7f);
        }
        #endregion

        #endregion

        #region (Fields)
        private ICancelSync cancel;
        private int timeOutRequestMSec;
        public int WaitAnswerMSec;
        public int nRepeatGlobal = 2;
        LogObject log;
        #endregion

        #region (Constructor)
        public Transport(LogObject log, ICancelSync cancel, DriverSetting drvSetting, int timeOutRequestMSec)
        {
            this.log = log;
            this.WaitAnswerMSec = drvSetting.WaitTimeout;
            this.nRepeatGlobal = drvSetting.RepeatCount;
            this.timeOutRequestMSec = timeOutRequestMSec;
            this.cancel = cancel;
        }
        #endregion

        double DoubleConverter(string s)
        {
            double result = double.NaN;
            if (double.TryParse(s,
              System.Globalization.NumberStyles.AllowDecimalPoint
              | System.Globalization.NumberStyles.AllowLeadingSign
              | System.Globalization.NumberStyles.AllowThousands
              | System.Globalization.NumberStyles.AllowTrailingSign
              | System.Globalization.NumberStyles.Float
              | System.Globalization.NumberStyles.Number
              | System.Globalization.NumberStyles.Integer,
              System.Globalization.CultureInfo.InvariantCulture,
              out result))
                return result;
            return double.NaN;
        }

        #region (Чтение событий)
        /// <summary>
        /// Чтение событий
        /// </summary>
        public OperationResult TryReadEvents(
          int indexFrom,
          IIODriverClient channel,
          int address,
          MesArchiveEvents archEvts,
          TimeZoneMap zone,
          out Event[] resultEvents)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение событий. Адрес: {0}", address));
#endif
            OperationResult result = OperationResult.Bad;
            resultEvents = null;
            List<Event> evList = new List<Event>();

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;

            var query = string.Format("0-0:99.98.{0}*{1}(4)", (byte)archEvts, indexFrom);
            // запрос одним запросом 
            byte[] send = BuildPacketNoSession(
              PacketMode.Read,
              address,
              query);
            byte[] answer;
            result = WriteReadCheck(
              channel,
              nRepeatGlobal,
              send,
              BuildPacketAnswerHeader(query),
              out answer);
            if (!result.IsGood) return result;
            try
            {
                var parse = ParseAnswerBraces(answer);
                for (int u = 0; u < parse.Length; u += 2)
                {
                    var key = int.Parse(parse[u]);
                    var time = ParseDateTime(parse[u + 1], zone);
#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получено событие {0}:{1}", key, time));
#endif
                    var ev = new Event
                    {
                        DateEvent = time,
                    };
                    switch (archEvts)
                    {
                        case MesArchiveEvents.Access:
                            {
                                #region (...)
                                switch (key)
                                {
                                    case 1: ev.MesEvent = new EvOpenCoverDrv(); ev.EventSource = SR.EvAccess1; break;
                                    case 2: ev.MesEvent = new EvCloseCoverDrv(); ev.EventSource = SR.EvAccess2; break;
                                    case 3: ev.MesEvent = new EvOpenCoverDrv(); ev.EventSource = SR.EvAccess3; break;
                                    case 4: ev.MesEvent = new EvCloseCoverDrv(); ev.EventSource = SR.EvAccess4; break;
                                    case 5: ev.MesEvent = new EvAdminDrv(); ev.EventSource = SR.EvAccess5; break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                #endregion
                            }
                            break;
                        case MesArchiveEvents.Programs:
                            {
                                ev.MesEvent = new EvImpactDrv();
                                #region (...)
                                switch (key)
                                {
                                    case 1: ev.EventSource = SR.EvProgr1; break;
                                    case 2: ev.EventSource = SR.EvProgr2; break;
                                    case 3: ev.EventSource = SR.EvProgr3; break;
                                    case 4: ev.EventSource = SR.EvProgr4; break;
                                    case 5: ev.EventSource = SR.EvProgr5; break;
                                    case 6: ev.EventSource = SR.EvProgr6; break;
                                    case 7: ev.EventSource = SR.EvProgr7; break;
                                    case 8: ev.EventSource = SR.EvProgr8; break;
                                    case 9: ev.EventSource = SR.EvProgr9; break;
                                    case 10: ev.EventSource = SR.EvProgr10; break;
                                    case 11: ev.EventSource = SR.EvProgr11; break;
                                    case 12: ev.EventSource = SR.EvProgr12; break;
                                    case 13: ev.EventSource = SR.EvProgr13; break;
                                    case 14: ev.EventSource = SR.EvProgr14; break;
                                    case 15: ev.EventSource = SR.EvProgr15; break;
                                    case 16: ev.EventSource = SR.EvProgr16; break;
                                    case 17: ev.EventSource = SR.EvProgr17; break;
                                    case 18: ev.EventSource = SR.EvProgr18; break;
                                    case 19: ev.EventSource = SR.EvProgr19; break;
                                    case 20: ev.EventSource = SR.EvProgr20; break;
                                    case 21: ev.EventSource = SR.EvProgr21; break;
                                    case 22: ev.EventSource = SR.EvProgr22; break;
                                    case 23: ev.EventSource = SR.EvProgr23; break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                #endregion
                            }
                            break;
                        case MesArchiveEvents.ArchErrors:
                            {
                                ev.MesEvent = new EvErrorDrv();
                                #region (...)
                                switch (key)
                                {
                                    case 1: ev.EventSource = SR.EvArchErr1; break;
                                    case 2: ev.EventSource = SR.EvArchErr2; break;
                                    case 3: ev.EventSource = SR.EvArchErr3; break;
                                    case 4: ev.EventSource = SR.EvArchErr4; break;
                                    case 5: ev.EventSource = SR.EvArchErr5; break;
                                    case 6: ev.EventSource = SR.EvArchErr6; break;
                                    case 7: ev.EventSource = SR.EvArchErr7; break;
                                    case 8: ev.EventSource = SR.EvArchErr8; break;
                                    case 9: ev.EventSource = SR.EvArchErr9; break;
                                    case 10: ev.EventSource = SR.EvArchErr10; break;
                                    case 11: ev.EventSource = SR.EvArchErr11; break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                #endregion
                            }
                            break;
                        case MesArchiveEvents.SostFaz:
                            {
                                ev.MesEvent = new EvPhaseDrv();
                                #region (...)
                                switch (key)
                                {
                                    case 1: ev.EventSource = SR.EvSostFaz1; break;
                                    case 2: ev.EventSource = SR.EvSostFaz2; break;
                                    case 3: ev.EventSource = SR.EvSostFaz3; break;
                                    case 4: ev.EventSource = SR.EvSostFaz4; break;
                                    case 5: ev.EventSource = SR.EvSostFaz5; break;
                                    case 6: ev.EventSource = SR.EvSostFaz6; break;
                                    case 7: ev.EventSource = SR.EvSostFaz7; break;
                                    case 8: ev.EventSource = SR.EvSostFaz8; break;
                                    case 9: ev.EventSource = SR.EvSostFaz9; break;
                                    case 10: ev.EventSource = SR.EvSostFaz10; break;
                                    case 11: ev.EventSource = SR.EvSostFaz11; break;
                                    case 12: ev.EventSource = SR.EvSostFaz12; break;
                                    case 13: ev.EventSource = SR.EvSostFaz13; break;
                                    case 14: ev.EventSource = SR.EvSostFaz14; break;
                                    case 15: ev.EventSource = SR.EvSostFaz15; break;
                                    case 16: ev.EventSource = SR.EvSostFaz16; break;
                                    case 17: ev.EventSource = SR.EvSostFaz17; break;
                                    case 18: ev.EventSource = SR.EvSostFaz18; break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                #endregion
                            }
                            break;
                        case MesArchiveEvents.OnOff:
                            {
                                #region (...)
                                switch (key)
                                {
                                    case 1:
                                        ev.MesEvent = new EvPowerOffDrv();
                                        ev.EventSource = SR.EvDevOff;
                                        break;
                                    case 2:
                                        ev.MesEvent = new EvPowerOnDrv();
                                        ev.EventSource = SR.EvDevOn;
                                        break;
                                    case 3:
                                        ev.MesEvent = new EvPowerOffDrv();
                                        ev.EventSource = SR.EvDevPit;
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                #endregion
                            }
                            break;
                    }
                    evList.Add(ev);
                    result = OperationResult.Good;
                }
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            resultEvents = evList.ToArray();
            return result;
        }
        #endregion

        #region (Чтение мгновенок)

        #region (enum ImVal)
        enum ImVal
        {
            PowerAp,
            PowerBp,
            PowerCp,

            PowerAm,
            PowerBm,
            PowerCm,

            KPowerA,
            KPowerB,
            KPowerC,

            NapA,
            NapB,
            NapC,

            TokA,
            TokB,
            TokC,

            Freq,
        }
        #endregion

        #region (class ImDesc)
        class ImDesc
        {
            /// <summary>
            /// Тип параметра
            /// </summary>
            public ImVal ImVal { get; set; }
            /// <summary>
            /// Номер параметра
            /// </summary>
            public int Num { get; set; }
            /// <summary>
            /// Описание
            /// </summary>
            public string Desc { get; set; }
            /// <summary>
            /// True=обязательный,false= что параметра может и не быть (однофазный)
            /// </summary>
            public bool IsRequred { get; set; }
        }
        #endregion

        /// <summary>
        /// Чтение мгновенок
        /// </summary>
        public OperationResult TryReadInstantaneousValues(
          IIODriverClient channel,
          int address,
          out InstantaneousValues values)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение мгновенок. Адрес: {0}", address));
#endif
            OperationResult result = OperationResult.Bad;
            values = new InstantaneousValues()
            {
                InsActivePower = new InstantaneousActivePower(),
                InsReactivePower = new InstantaneousReactivePower(),
                Amperage = new Phase(),
                Frequency = new double(),
                PowerFactor = new Phase(),
                Voltage = new Phase()
            };

            List<ImDesc> descs = new List<ImDesc>();
            descs.Add(new ImDesc { ImVal = ImVal.PowerAp, IsRequred = true, Num = 21, Desc = "прямая активная мощность по фазе A (Pa+)" });
            descs.Add(new ImDesc { ImVal = ImVal.PowerBp, IsRequred = false, Num = 41, Desc = "прямая активная мощность по фазе B (Pb+)" });
            descs.Add(new ImDesc { ImVal = ImVal.PowerCp, IsRequred = false, Num = 61, Desc = "прямая активная мощность по фазе C (Pc+)" });

            descs.Add(new ImDesc { ImVal = ImVal.PowerAm, IsRequred = true, Num = 22, Desc = "обратная активная мощность по фазе A (Pa-)" });
            descs.Add(new ImDesc { ImVal = ImVal.PowerBm, IsRequred = false, Num = 42, Desc = "обратная активная мощность по фазе B (Pb-)" });
            descs.Add(new ImDesc { ImVal = ImVal.PowerCm, IsRequred = false, Num = 62, Desc = "обратная активная мощность по фазе C (Pc-)" });

            descs.Add(new ImDesc { ImVal = ImVal.KPowerA, IsRequred = true, Num = 33, Desc = "коэффициент мощности по фазе A" });
            descs.Add(new ImDesc { ImVal = ImVal.KPowerB, IsRequred = false, Num = 53, Desc = "коэффициент мощности по фазе B" });
            descs.Add(new ImDesc { ImVal = ImVal.KPowerC, IsRequred = false, Num = 73, Desc = "коэффициент мощности по фазе C" });

            descs.Add(new ImDesc { ImVal = ImVal.TokA, IsRequred = true, Num = 31, Desc = "ток по фазе A" });
            descs.Add(new ImDesc { ImVal = ImVal.TokB, IsRequred = false, Num = 51, Desc = "ток по фазе B" });
            descs.Add(new ImDesc { ImVal = ImVal.TokC, IsRequred = false, Num = 71, Desc = "ток по фазе C" });

            descs.Add(new ImDesc { ImVal = ImVal.NapA, IsRequred = true, Num = 32, Desc = "напряжение по фазе A" });
            descs.Add(new ImDesc { ImVal = ImVal.NapB, IsRequred = false, Num = 52, Desc = "напряжение по фазе B" });
            descs.Add(new ImDesc { ImVal = ImVal.NapC, IsRequred = false, Num = 72, Desc = "напряжение по фазе C" });

            descs.Add(new ImDesc { ImVal = ImVal.Freq, IsRequred = true, Num = 0, Desc = "Частота сети" });

            for (int i = 0; i < descs.Count; i++)
            {
                var item = descs[i];

                result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
                if (!result.IsGood) return result;

                string query = string.Empty;
                switch (item.ImVal)
                {
                    case ImVal.PowerAp:
                    case ImVal.PowerBp:
                    case ImVal.PowerCp:
                    case ImVal.PowerAm:
                    case ImVal.PowerBm:
                    case ImVal.PowerCm:
                        query = string.Format("1-0:{0}.7.0", item.Num);
                        break;
                    case ImVal.KPowerA:
                    case ImVal.KPowerB:
                    case ImVal.KPowerC:
                        query = string.Format("1-0:{0}.7.0", item.Num);
                        break;
                    case ImVal.TokA:
                    case ImVal.TokB:
                    case ImVal.TokC:
                    case ImVal.NapA:
                    case ImVal.NapB:
                    case ImVal.NapC:
                        query = string.Format("1-0:{0}.7.0", item.Num);
                        break;
                    case ImVal.Freq:
                        query = string.Format("1-0:14.7.0");
                        break;
                }
                byte[] send = BuildPacketNoSession(
                  PacketMode.Read,
                  address,
                  // Здесь в заводской программе при чтении используется С=15, при С=0 или 1 идет ошибка от счетчика!
                  query);
                byte[] answer;
                var tmpResult = WriteReadCheck(
                  channel,
                  nRepeatGlobal,
                  send,
                  BuildPacketAnswerHeader(query),
                  out answer);
                if (!tmpResult.IsGood)
                {
                    if (item.IsRequred) return tmpResult;
                    else continue;
                }
                try
                {
                    var parse = ParseAnswerBraces(answer);
                    if (parse.Length == 1)
                    {
                        var val = DoubleConverter(parse[0]);
                        switch (item.ImVal)
                        {
                            case ImVal.PowerAp: values.InsActivePower.InsPowerPhase.Phase_A = val / 1000d; break;
                            case ImVal.PowerBp: values.InsActivePower.InsPowerPhase.Phase_B = val / 1000d; break;
                            case ImVal.PowerCp: values.InsActivePower.InsPowerPhase.Phase_C = val / 1000d; break;
                            case ImVal.PowerAm: values.InsReactivePower.InsPowerPhase.Phase_A = val / 1000d; break;
                            case ImVal.PowerBm: values.InsReactivePower.InsPowerPhase.Phase_B = val / 1000d; break;
                            case ImVal.PowerCm: values.InsReactivePower.InsPowerPhase.Phase_C = val / 1000d; break;

                            case ImVal.KPowerA: values.PowerFactor.Phase_A = val; break;
                            case ImVal.KPowerB: values.PowerFactor.Phase_B = val; break;
                            case ImVal.KPowerC: values.PowerFactor.Phase_C = val; break;

                            case ImVal.NapA: values.Voltage.Phase_A = val; break;
                            case ImVal.NapB: values.Voltage.Phase_B = val; break;
                            case ImVal.NapC: values.Voltage.Phase_C = val; break;

                            case ImVal.TokA: values.Amperage.Phase_A = val; break;
                            case ImVal.TokB: values.Amperage.Phase_B = val; break;
                            case ImVal.TokC: values.Amperage.Phase_C = val; break;

                            case ImVal.Freq: values.Frequency = val; break;
                        }

#if (LogTransmit)
                        log.Trace.Write(1, x => x.Info("Получен {0}:{1}", item.ImVal, val));
#endif
                        result = OperationResult.Good;
                    }
                    else
                    {
#if (LogTransmit)
                        log.Trace.Write(1, x => x.Info("Получен parse:{0}", string.Join(",", parse)));
#endif
                        result.Quality = Quality.Bad;
                        result.ErrorMsg = SR.NoLast;
                    }
                }
                catch (Exception e)
                {
                    result = OperationResult.From(e);
                    break;
                }
            }
            return result;
        }
        #endregion

        #region (Чтение накопленной суточной энергии)
        public enum NakopEnergy
        {
            Day = 128,
            Month = 129,
            Year = 130
        }
        /// <summary>
        /// Чтение накопленной суточной энергии
        /// </summary>
        public OperationResult TryReadEnergyNakop(
          NakopEnergy mode,
          IIODriverClient channel,
          int address,
          DeviceCompOn deviceTime,
          DateTimeZone timeOfHole,
          TimeZoneMap zone,
          out Energy energyData,
          out int index)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение накопленной суточной энергии. Адрес: {0}", address));
#endif
            OperationResult result = OperationResult.Bad;
            energyData = new Energy(0, 0, 0, 0);

            var calcedDeviceTime = deviceTime.GetDeviceTime(zone);
            // счетчик в начале 00:00 отдает лажу как достоверное, поэтому не берем 
            // в начале суток показания, будем брать потом. 
            //if (calcedDeviceTime.Hour < 1) return result;
            var holeCompare = timeOfHole;
            index = 0;
            switch (mode)
            {
                case NakopEnergy.Day:
                    // 0 = тек.день назад, а в 30 мин мощности 0=тек.день.
                    index = (int)Math.Floor((calcedDeviceTime - timeOfHole).TotalDays);
                    break;
                case NakopEnergy.Month:
                    // если сейчас август, то 0=август, 1=июль и т.д.
                    holeCompare = new DateTimeZone(holeCompare.Year, holeCompare.Month, 1, holeCompare.TimeZoneMap);
                    index = Math.Abs(calcedDeviceTime.Month - timeOfHole.Month)
                      // учитываем возможность разницы лет
                      * (calcedDeviceTime.Year - holeCompare.Year + 1);
                    break;
                case NakopEnergy.Year:
                    index = Math.Abs(calcedDeviceTime.Year - timeOfHole.Year);
                    break;
            }

            for (int i = 1; i <= 4; i++)
            {
                result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
                if (!result.IsGood) return result;

                // Здесь в заводской программе при чтении используется С=15, при С=0 или 1 идет ошибка от счетчика!
                var queryHeader = string.Format("1-1:{0}.{1}.0*{2}(1)", i == 1 ? 15 : i, (byte)mode, index);
                // запрос одним запросом 
                byte[] send = BuildPacketNoSession(
                  PacketMode.Read,
                  address,
                  queryHeader);
                byte[] answer;
                result = WriteReadCheck(
                  channel,
                  nRepeatGlobal,
                  send,
                  BuildPacketAnswerHeader(queryHeader),
                  out answer);
                if (!result.IsGood) return result;
                try
                {
                    var parse = ParseAnswerBraces(answer);
                    if (parse.Length >= 2)
                    {
                        var val = DoubleConverter(parse[0]);
                        var time = ParseDateTime(parse[1], zone);
#if (LogTransmit)
                        var arr = new string[] { "A+", "A-", "R+", "R-" };
                        log.Trace.Write(1, x => x.Info("Получен {0}:{1} Time:{2}", arr[i - 1], val, time));
#endif
                        // сравниваем с holeCompare, т.к. в устройстве данные на начало интервала, а у нас на конец.
                        if (time != holeCompare)
                        {
                            if (time.Month != holeCompare.Month || time.Year != holeCompare.Year)
                            {
                                result.Quality = Quality.BadNoRestore;
#if (LogTransmit)
                                log.Trace.Write(1, x => x.Info("Получено устаревшее время:{0}. Больше в глубину не опрашиваем.", arr[i], val, time));
#endif
                                return result;
                            }
                            throw new Exception(string.Format(SR.SliceTimeNotEqual, timeOfHole, time));
                        }
                        switch (i)
                        {
                            case 1: energyData.Aplus = val / 1000; break;
                            case 2: energyData.Aminus = val / 1000; break;
                            case 3: energyData.Rplus = val / 1000; break;
                            case 4: energyData.Rminus = val / 1000; break;
                        }
                        result = OperationResult.Good;
                    }
                    else
                    {
#if (LogTransmit)
                        log.Trace.Write(1, x => x.Info("Получен parse:{0}", string.Join(",", parse)));
#endif
                        result.Quality = Quality.Bad;
                        result.ErrorMsg = SR.NoLast;
                    }
                }
                catch (Exception e)
                {
                    result = OperationResult.From(e);
                    break;
                }
            }
            return result;
        }
        #endregion

        #region (Чтение приращения энергии)
        public enum PrirEnergy
        {
            Day = 131,
            Month = 9,
            Year = 132
        }
        /// <summary>
        /// Чтение приращения суточной энергии
        /// </summary>
        public OperationResult TryReadEnergyPrirash(
          PrirEnergy mode,
          IIODriverClient channel,
          int address,
          DeviceCompOn deviceTime,
          DateTimeZone timeOfHole,
          TimeZoneMap zone,
          out Energy energyData)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение приращения энергии. Адрес: {0}", address));
#endif
            OperationResult result = OperationResult.Bad;
            energyData = new Energy(0, 0, 0, 0);

            var calcedDeviceTime = deviceTime.GetDeviceTime(zone);
            var holeCompare = timeOfHole;
            int index = 0;
            switch (mode)
            {
                case PrirEnergy.Day:
                    // 0 = день назад, а в 30 мин мощности 0=тек.день.
                    holeCompare = holeCompare.AddDays(-1);
                    index = (int)Math.Floor((calcedDeviceTime - timeOfHole).TotalDays);
                    break;
                case PrirEnergy.Month:
                    // если сейчас август, то 0=июль, 1=июннь и т.д.
                    holeCompare = holeCompare.AddMonths(-1);
                    holeCompare = new DateTimeZone(holeCompare.Year, holeCompare.Month, 1, holeCompare.TimeZoneMap);
                    index = Math.Abs(calcedDeviceTime.Month - timeOfHole.Month)
                      // учитываем возможность разницы лет
                      * (calcedDeviceTime.Year - holeCompare.Year + 1);
                    break;
                case PrirEnergy.Year:
                    holeCompare = holeCompare.AddYears(-1);
                    index = Math.Abs(calcedDeviceTime.Year - timeOfHole.Year);
                    break;
            }

            for (int i = 1; i <= 4; i++)
            {
                result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
                if (!result.IsGood) return result;

                // Здесь в заводской программе при чтении используется С=15, при С=0 или 1 идет ошибка от счетчика!
                var queryHeader = string.Format("1-1:{0}.{1}.0*{2}(1)", i == 1 ? 15 : i, (byte)mode, index);
                // запрос одним запросом 
                byte[] send = BuildPacketNoSession(
                  PacketMode.Read,
                  address,
                  queryHeader);
                byte[] answer;
                result = WriteReadCheck(
                  channel,
                  nRepeatGlobal,
                  send,
                  BuildPacketAnswerHeader(queryHeader),
                  out answer);
                if (!result.IsGood) return result;
                try
                {
                    var parse = ParseAnswerBraces(answer);
                    if (parse.Length >= 2)
                    {
                        var val = DoubleConverter(parse[0]);
                        var time = ParseDateTime(parse[1], zone);
#if (LogTransmit)
                        var arr = new string[] { "A+", "A-", "R+", "R-" };
                        log.Trace.Write(1, x => x.Info("Получен {0}:{1} Time:{2}", arr[i - 1], val, time));
#endif
                        // сравниваем с holeCompare, т.к. в устройстве данные на начало интервала, а у нас на конец.
                        if (time != holeCompare)
                        {
                            if (time.Month != holeCompare.Month || time.Year != holeCompare.Year)
                            {
                                result.Quality = Quality.BadNoRestore;
#if (LogTransmit)
                                log.Trace.Write(1, x => x.Info("Получено устаревшее время:{0}. Больше в глубину не опрашиваем.", arr[i], val, time));
#endif
                                return result;
                            }
                            throw new Exception(string.Format(SR.SliceTimeNotEqual, timeOfHole, time));
                        }
                        switch (i)
                        {
                            case 1: energyData.Aplus = val / 1000; break;
                            case 2: energyData.Aminus = val / 1000; break;
                            case 3: energyData.Rplus = val / 1000; break;
                            case 4: energyData.Rminus = val / 1000; break;
                        }
                        result = OperationResult.Good;
                    }
                    else
                    {
#if (LogTransmit)
                        log.Trace.Write(1, x => x.Info("Получен parse:{0}", string.Join(",", parse)));
#endif
                        result.Quality = Quality.Bad;
                        result.ErrorMsg = SR.NoLast;
                    }
                }
                catch (Exception e)
                {
                    result = OperationResult.From(e);
                    break;
                }
            }
            return result;
        }
        #endregion

        #region (Чтение 3/30 мин срезов за прошлый интервал)

        #region (enum ESlice)
        public enum ESlice : byte
        {
            /// <summary>
            /// Предыдущая 3-х минутка
            /// </summary>
            Min3 = 5,
            /// <summary>
            /// Предыдущая 30-и минутка
            /// </summary>
            Min30 = 15
        }
        #endregion

        /// <summary>
        /// Забрать все виды энергии A+..R-
        /// </summary>
        public OperationResult TryReadSlicesEnergy3n30min(
          ESlice mode,
          IIODriverClient channel,
          int address,
          DeviceCompOn deviceTime,
          DateTimeZone timeOfHole,
          TimeZoneMap zone,
          out Energy energyData)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение срезов энергии 3/30 минут. Адрес: {0}. Режим:{1}", address, mode));
#endif
            OperationResult result = OperationResult.Bad;
            energyData = new Energy(0, 0, 0, 0);

            var calcedDeviceTime = deviceTime.GetDeviceTime(zone);
            var diff = (calcedDeviceTime - timeOfHole).TotalMinutes;
            int n = mode == ESlice.Min3 ? 3 : 30;
            if (diff > 3)
            {
                result.Quality = Quality.BadNoRestore;
                result.ErrorMsg = SR.DepthNotSupported;
                return result;
            }

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;

            for (int c = 1; c <= 4; c++)
            {

                var queryHeader = string.Format("1-1:{0}.{1}.0",
                  // так запрашивает завдоская программа!
                  c == 1 ? 15 : c,
                  (byte)mode);
                // запрос одним запросом 
                byte[] send = BuildPacketNoSession(
                  PacketMode.Read,
                  address,
                  queryHeader);
                byte[] answer;
                result = WriteReadCheck(
                  channel,
                  nRepeatGlobal,
                  send,
                  BuildPacketAnswerHeader(queryHeader),
                  out answer);
                if (!result.IsGood) return result;
                try
                {
                    var parse = ParseAnswerBraces(answer);
                    if (parse.Length == 2)
                    {
                        var val = DoubleConverter(parse[0]) / 1000d;
                        var time = ParseDateTime(parse[1], zone);
#if (LogTransmit)
                        log.Trace.Write(1, x => x.Info("Получено пред.значение ({0}):{1} Time:{2}", mode, val, time));
#endif
                        if (time != timeOfHole.AddMinutes(-n))
                        {
                            throw new Exception(string.Format(SR.SliceTimeNotEqual, timeOfHole, time));
                        }
                        switch (c)
                        {
                            case 1: energyData.Aplus = val; break;
                            case 2: energyData.Aminus = val; break;
                            case 3: energyData.Rplus = val; break;
                            case 4: energyData.Rminus = val; break;
                        }
                        result = OperationResult.Good;
                    }
                    else
                    {
#if (LogTransmit)
                        log.Trace.Write(1, x => x.Info("Получен parse:{0}", string.Join(",", parse)));
#endif
                        result.Quality = Quality.Bad;
                        result.ErrorMsg = SR.NoLast;
                    }
                }
                catch (Exception e)
                {
                    result = OperationResult.From(e);
                    return result;
                }
            }
            return result;
        }
        #endregion

        #region (Чтение 30 мин срезов)
        /// <summary>
        /// Забрать все виды энергии A+..R-
        /// </summary>
        public OperationResult TryReadSlicesEnergy(
          IIODriverClient channel,
          int address,
          DeviceCompOn deviceTime,
          DateTimeZone timeOfHole,
          TimeZoneMap zone,
          out Energy energyData)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение 30 мин срезов. Адрес: {0}.", address));
#endif
            OperationResult result = OperationResult.Bad;
            energyData = new Energy(0, 0, 0, 0);

            var calcedDeviceTime = deviceTime.GetDeviceTime(zone);
            int index = (int)Math.Floor((calcedDeviceTime - timeOfHole).TotalMinutes / 30d);

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;

            var queryHeader =
              // Здесь в заводской программе при чтении используется С=15, при С=0 или 1 идет ошибка от счетчика!
              string.Format("1-1:15.29.0*{0}(4)", index);
            // запрос одним запросом 
            byte[] send = BuildPacketNoSession(
              PacketMode.Read,
              address,
              queryHeader);
            byte[] answer;
            result = WriteReadCheck(
              channel,
              nRepeatGlobal,
              send,
              BuildPacketAnswerHeader(queryHeader),
              out answer);
            if (!result.IsGood) return result;
            try
            {
                var parse = ParseAnswerBraces(answer);
                if (parse.Length >= 5)
                {
                    var sAplus = DoubleConverter(parse[0]);
                    var sAminus = DoubleConverter(parse[1]);
                    var sRplus = DoubleConverter(parse[2]);
                    var sRminus = DoubleConverter(parse[3]);
                    var time = ParseDateTime(parse[4], zone);
#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получен A+:{0} A-:{1} R+:{2} R-:{3} Time:{4}", parse));
#endif
                    // сравниваем с -30 мин, т.к. в устройстве данные на начало интервала, а у нас на конец.
                    if (time != timeOfHole.AddMinutes(-30))
                    {
                        throw new Exception(string.Format(SR.SliceTimeNotEqual, timeOfHole, time));
                    }
                    energyData.Aplus = sAplus / 1000;
                    energyData.Aminus = sAminus / 1000;
                    energyData.Rplus = sRplus / 1000;
                    energyData.Rminus = sRminus / 1000;
                    result = OperationResult.Good;
                }
                else
                {
#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получен parse:{0}", string.Join(",", parse)));
#endif
                    result.Quality = Quality.Bad;
                    result.ErrorMsg = SR.NoLast;
                }
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
        #endregion

        #region (Чтение KI)
        /// <summary>
        /// Чтение KI
        /// </summary>
        public OperationResult TryReadKI(
          IIODriverClient channel,
          int address,
          out double ki)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение KI. Адрес: {0}.", address));
#endif
            OperationResult result = OperationResult.Bad;
            ki = 0;

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;
            var queryHeader = "0-0:96.68.0";
            byte[] send = BuildPacketNoSession(PacketMode.Read, address, queryHeader);
            byte[] answer;
            result = WriteReadCheck(
              channel,
              nRepeatGlobal,
              send,
              BuildPacketAnswerHeader(queryHeader),
              out answer);
            if (!result.IsGood) return result;
            try
            {
                var parse = ParseAnswerBraces(answer);
                if (parse.Length > 0)
                {
                    var kis = parse[0];
#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получен Ki:{0}", kis));
#endif
                    ki = DoubleConverter(kis);
                    result = OperationResult.Good;
                }
                else
                {
                    result.Quality = Quality.Bad;
                    result.ErrorMsg = SR.NoKI;
                }
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
        #endregion

        #region (Чтение KU)
        /// <summary>
        /// Чтение KU
        /// </summary>
        public OperationResult TryReadKU(
          IIODriverClient channel,
          int address,
          out double ku)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение KU. Адрес: {0}.", address));
#endif
            OperationResult result = OperationResult.Bad;
            ku = 0;

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;
            var queryHeader = "0-0:96.68.1";
            byte[] send = BuildPacketNoSession(PacketMode.Read, address, queryHeader);
            byte[] answer;
            result = WriteReadCheck(
              channel,
              nRepeatGlobal,
              send,
              BuildPacketAnswerHeader(queryHeader),
              out answer);
            if (!result.IsGood) return result;
            try
            {
                var parse = ParseAnswerBraces(answer);
                if (parse.Length > 0)
                {
                    var kus = parse[0];
#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получен Ku:{0}", kus));
#endif
                    ku = DoubleConverter(kus);
                    result = OperationResult.Good;
                }
                else
                {
                    result.Quality = Quality.Bad;
                    result.ErrorMsg = SR.NoKI;
                }
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
        #endregion

        #region (Чтение SN)
        /// <summary>
        /// Чтение SN
        /// </summary>
        public OperationResult TryReadSN(
          IIODriverClient channel,
          int address,
          out string sn)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение SN. Адрес: {0}.", address));
#endif
            OperationResult result = OperationResult.Bad;
            sn = string.Empty;

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;
            var queryHeader = "0-0:96.1.0";
            byte[] send = BuildPacketNoSession(PacketMode.Read, address, queryHeader);
            byte[] answer;
            result = WriteReadCheck(
              channel,
              nRepeatGlobal,
              send,
              BuildPacketAnswerHeader(queryHeader),
              out answer);
            if (!result.IsGood) return result;
            try
            {
                var parse = ParseAnswerBraces(answer);
                if (parse.Length > 0)
                {
                    var serial = parse[0];
#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получен серийный номер:{0}", serial));
#endif
                    var time = parse[1];
#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получено время сборки:{0}", time));
#endif
                    sn = string.Format("({0})({1})", serial, parse[1]);
                    result = OperationResult.Good;
                }
                else
                {
                    result.Quality = Quality.Bad;
                    result.ErrorMsg = SR.NoSN;
                }
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
        #endregion

        #region (Чтение Version)
        /// <summary>
        /// Чтение Version
        /// </summary>
        public OperationResult TryReadVersion(
          IIODriverClient channel,
          int address,
          out Version sn)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение Version. Адрес: {0}.", address));
#endif
            OperationResult result = OperationResult.Bad;
            sn = new Version(0, 0);

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;
            var queryHeader = "1-0:0.2.0";
            byte[] send = BuildPacketNoSession(PacketMode.Read, address, queryHeader);
            byte[] answer;
            result = WriteReadCheck(
              channel,
              nRepeatGlobal,
              send,
              BuildPacketAnswerHeader(queryHeader),
              out answer);
            if (!result.IsGood) return result;
            try
            {
                var parse = ParseAnswerBraces(answer);
                if (parse.Length > 0)
                {
                    var serial = parse[0];
                    sn = new Version(serial);
#if (LogTransmit)
#endif
                    result = OperationResult.Good;
                }
                else
                {
                    result.Quality = Quality.Bad;
                    result.ErrorMsg = SR.NoSN;
                }
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
        #endregion

        #region (Создание и разбор пакетов)
        enum PacketMode
        {
            Read,
            Write
        }
        byte[] BuildPacketAnswerHeader(string command)
        {
            var buff = new List<byte>();
            var idx = command.IndexOf("(");
            if (idx > -1)
            {
                command = command.Substring(0, idx);
            }
            buff.Add(ASCIIChars.StartOfText.ToByte());
            buff.AddRange(TextToAscii(command));
            return buff.ToArray();
        }
        /// <summary>
        /// Создание пакета без сессии
        /// </summary>
        byte[] BuildPacketNoSession(PacketMode mode, int address, string command)
        {
            var buff = new List<byte>();
            buff.Add(ASCIIChars.FrontSlash.ToByte());
            buff.Add(ASCIIChars.QuestionMark.ToByte());
            buff.AddRange(AddressToAscii(address));
            buff.Add(ASCIIChars.ExclamationMark.ToByte());
            int idx = buff.Count;
            switch (mode)
            {
                case PacketMode.Read:
                    buff.Add(ASCIIChars.R.ToByte());
                    break;
                case PacketMode.Write:
                    buff.Add(ASCIIChars.W.ToByte());
                    break;
            }
            buff.Add(ASCIIChars.One.ToByte());
            buff.Add(ASCIIChars.StartOfText.ToByte());
            buff.AddRange(TextToAscii(command));
            buff.Add(ASCIIChars.EndOfText.ToByte());
            buff.Add(Crc(buff.ToArray(), idx, buff.Count - idx));
            return buff.ToArray();
        }

        /// <summary>
        /// Разобрать в комманде ответ в (), и вернуть содержимое таких скобок
        /// </summary>
        /// <returns></returns>
        string[] ParseAnswerBraces(byte[] answer)
        {

            List<string> result = new List<string>();
            int startPos = -1;
            string msg = string.Empty;
            for (int i = 0; i < answer.Length; i++)
            {
                var item = answer[i];
                if (startPos == -1 && item == ASCIIChars.OpenParentheses)
                {
                    startPos = i + 1;
                    msg = string.Empty;
                }
                if (startPos > -1 && item == ASCIIChars.CloseParentheses)
                {
                    result.Add(msg);
                    startPos = -1;
                }
                if (startPos == i)
                {
                    msg += new ASCIIChar(item).ToString();
                    startPos++;
                }
            }
            return result.ToArray();
        }

        DateTimeZone ParseDateTime(string time, TimeZoneMap zone)
        {
            if (string.IsNullOrEmpty(time))
                throw new ArgumentNullException();
            if (time.IndexOf(" ") > -1)
            {
                var sp = time.Split(' ');
                var dt = sp[0].Split('-');
                var tm = sp[1].Split(':');
                var year = 2000 + int.Parse(dt[0]);
                var month = int.Parse(dt[1]);
                var day = int.Parse(dt[2]);

                int hour = tm.Length > 0 ? int.Parse(tm[0]) : 0;
                int min = tm.Length > 1 ? int.Parse(tm[1]) : 0;
                int sec = tm.Length > 2 ? int.Parse(tm[2]) : 0;

                var ret = new DateTimeZone(year, month, day, hour, min, sec, zone);
                return ret;
            }
            else if (time.IndexOf("-") > -1)
            {
                var dt = time.Split('-');
                var ret = new DateTimeZone(
                  2000 + int.Parse(dt[0]),
                  int.Parse(dt[1]),
                  int.Parse(dt[2]),
                  zone);
                return ret;
            }
            else if (time.IndexOf(":") > -1)
            {
                var dt = time.Split(':');
                var now = DateTimeZone.Today(zone);
                var ret = new DateTimeZone(
                  now.Year,
                  now.Month,
                  now.Day,
                  int.Parse(dt[0]),
                  int.Parse(dt[1]),
                  int.Parse(dt[2]),
                  zone);
                return ret;
            }
            throw new NotImplementedException();
        }
        #endregion

        #region (Чтение и запись даты и времени)
        public OperationResult TryWriteDateDate(
          IIODriverClient channel,
          int address,
          TimeZoneMap map,
          EmeraProtectLevel level,
          DeviceCompOn diff)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Запись даты. Адрес: {0}.", address));
#endif
            OperationResult result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;

            /*
            DateTimeZone dateTime = diff.GetServerToDeviceTime(map);
            byte fine = 0;
            //Установка даты и времени (поле ‘уточнение’ = 2) . Поле ‘данные’ должно содержать 6 
            //байт новой даты и времени. По этому варианту контроль выполняется только на до-
            //пустимость значений. Установка даты и времени защищена основным паролем.
            if (level == EmeraProtectLevel.Base) fine = 2;
            */
            try
            {
                var queryHeader = string.Format("0-0:96.51.0({0})", (int)(diff.DiffMsec / 1000));
                byte[] send = BuildPacketNoSession(
                  PacketMode.Write,
                  address,
                  queryHeader);
                byte[] answer;
                result = WriteReadCheck(
                  channel,
                  nRepeatGlobal,
                  send,
                  null,
                  out answer);
                if (!result.IsGood) return result;

                //var parse = ParseAnswerBraces(answer);
                if (answer[0] == ASCIIChars.Acknowledge)
                    result = new OperationResult(Quality.Good);
            }
            catch { }
            return result;
        }

        public OperationResult TryReadDateTime(
          IIODriverClient channel,
          int address,
          TimeZoneMap zone,
          out DateTimeUtc response,
          out int timeTwoSidePathMsec)
        {
#if (LogTransmit)
            log.Trace.Write(1, x => x.Info("Чтение даты. Адрес: {0}.", address));
#endif
            OperationResult result = OperationResult.Bad;
            response = DateTimeUtc.MinValue;
            timeTwoSidePathMsec = 0;

            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;
            var queryHeader = "1-0:0.9.1";
            byte[] send = BuildPacketNoSession(PacketMode.Read, address, queryHeader);
            byte[] answer;
            var span = SpanSnapshot.Now;
            result = WriteReadCheck(
              channel,
              nRepeatGlobal,
              send,
              BuildPacketAnswerHeader(queryHeader),
              out answer);

            timeTwoSidePathMsec = (int)span.DiffNowMsec();
            if (!result.IsGood) return result;
            try
            {
                var time = string.Empty;
                var parse = ParseAnswerBraces(answer);
                if (parse.Length > 0)
                {
                    time = parse[0];
                    var ret = ParseDateTime(time, zone);
                    //#warning Подкручено время для проверки записи
                    //ret= ret.AddSeconds(+10);

                    response = ret;

#if (LogTransmit)
                    log.Trace.Write(1, x => x.Info("Получена дата-время:{0}", ret));
#endif
                    result = OperationResult.Good;
                }
                else
                {
                    result = new OperationResult(Quality.Bad);
                }
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
        #endregion

        #region (IO)
        private OperationResult WriteReadCheck(
          IIODriverClient channel,
          int nRepet,
          byte[] sendBuf,
          byte[] answerHeaderToCheck,
          out byte[] readBuf
          )
        {
            readBuf = null;
            byte[] outBuf = null;
            OperationResult result = DriverTransport.TryRepeater(nRepet, WaitAnswerMSec, true, channel, cancel, TimeRange.None, () =>
            {
                var res = WriteReadCheckPrivate(channel, sendBuf, answerHeaderToCheck, out outBuf);
                return new OperationData<bool>(true, res);
            });
            readBuf = outBuf;
            return result;
        }

        /// <summary>Записать и считать определенное количество байт</summary>
        OperationResult WriteReadCheckPrivate(
          IIODriverClient channel,
          byte[] sendBuf,
          byte[] answerHeaderToCheck,
          out byte[] readBuf)
        {
            readBuf = null;
            OperationResult result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;
            try
            {
                channel.DiscardInBuffer();
                channel.DiscardOutBuffer();

#if (LogTransmit)
                var ss = new ASCIIString(sendBuf.ToArray(), 0, sendBuf.Length).ToString();
                log.Trace.Write(1, (l) => l.Info("Отправка пакета: {0}", ss));
#endif

                channel.Write(sendBuf, 0, sendBuf.Length);
                List<byte> read = new List<byte>();
                bool isRead = true;
                do
                {
                    var d = channel.ReadByte();
                    if (d < 0) isRead = false;
                    else
                    {
                        byte b = (byte)d;
                        read.Add(b);
                        if (b == ASCIIChars.Acknowledge && read.Count == 1)
                        {
                            isRead = true;
                            break;
                        }
                        if (b == ASCIIChars.EndOfText)
                        {
                            d = channel.ReadByte();
                            read.Add((byte)d);
                            isRead = false;
                        }
                    }
                } while (isRead);
                readBuf = read.ToArray();

                channel.DiscardInBuffer();
                channel.DiscardOutBuffer();

#if (LogTransmit)
                ss = new ASCIIString(readBuf.ToArray(), 0, readBuf.Length).ToString();
                log.Trace.Write(1, (l) => l.Info("Получен пакет: {0}", ss));
#endif
                if (readBuf.Length == 1 && readBuf[0] == ASCIIChars.Acknowledge)
                    return result;

                Error err;
                if (Error.IsError(readBuf, out err))
                {
                    result.Quality = Quality.Bad;
                    if (err.Operand == "ERROR06" || err.Operand == "ERROR08")
                        // если функция не поддерживается, то и говорим так!
                        result.Quality = Quality.BadNoRestore;
                    result.ErrorMsg = string.Format("{0}{1}{2}", err.Description, Environment.NewLine, err.Info);
                    log.Trace.Error(1, err.Description);
                    return result;
                }

                var crc = Crc(readBuf, 1, readBuf.Length - 1 - 1);
                if (crc != readBuf[readBuf.Length - 1])
                    throw new Exception(SR.CrcError);

                // проверяем заголовок ответа
                if (answerHeaderToCheck != null)
                {
                    // null = не проверять заголовок
                    if (readBuf.Length >= answerHeaderToCheck.Length)
                    {
                        for (int i = 0; i < answerHeaderToCheck.Length; i++)
                        {
                            if (readBuf[i] != answerHeaderToCheck[i])
                                throw new Exception(SR.NotEqualAnswerPacket);
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                log.Trace.Error(1, e.Message);
                result.Quality = e.ToQuality();
                result.ErrorMsg = e.GetFullMessage();
            }
            finally
            {
                //sw.Restart();
            }
            return result;
        }
        #endregion
    }
}

