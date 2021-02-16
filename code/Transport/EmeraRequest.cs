using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sys.Services.Drv.Emera.Def;
using Sys.Services.Drv.Emera.Utils;
using Sys.Types.Components;
using System.IO;
using Sys.Services.Drv.Emera.Culture;
using Sys.DataBus.Common;
using System.Diagnostics;
using System.Threading;
using Sys.Async;
using Sys.Services.Components;

namespace Sys.Services.Drv.Emera.Transport
{
    /// <summary>Представляет реализацию всех запросов к устройству</summary>
    public class EmeraRequest
    {
        /// <summary>Глубина хранения 3 мнуток</summary>
        public const int Depth3Min = 1;
        /// <summary>Глубина хранения получасов</summary>
        public const int Depth30MinDefault = 14 * 48;
        /// <summary>Глубина хранения суточных</summary>
        public const int DepthDay = 31;
        /// <summary>Глубина месяцев</summary>
        public const int DepthMonth = 12;

        /// <summary>Глубина хранения суточных показаний счетчиков</summary>
        public const int DepthDayCounter = 31;
        /// <summary>Глубина хранения месячных показаний счетчиков</summary>
        public const int DepthMonthCounter = 12;
        /// <summary>Глубина событий количество</summary>
        public const int DepthEvents = 20;

        private int timeOutRequestMSec;
        public int WaitAnswerMSec;
        public int nRepeatGlobal = 2;

        public const int TimeOutRequestMSecDeafult = 70;
        public const int WaitAnswerMSecDeafult = 10000;
        public const int CountRepeatDeafult = 2;

        private ICancelSync cancel;

        public EmeraRequest(ICancelSync cancel, DriverSetting drvSetting, int timeOutRequestMSec)
        {
            this.WaitAnswerMSec = drvSetting.WaitTimeout;
            this.nRepeatGlobal = drvSetting.RepeatCount;
            this.timeOutRequestMSec = timeOutRequestMSec;
            this.cancel = cancel;
        }

        public OperationResult TryReadInstantaneousValues(IIODriverClient channel, int address, string psw, out InstantaneousValues response)
        {
            OperationResult result = OperationResult.Bad;
            response = null;
            var send = CreateRequest(address, Codes.CODE_READ_POWER_CURR, psw);
            result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            if (!result.IsGood) return result;

            Array.Resize<byte>(ref answer, 4);
            try
            {
                response = new InstantaneousValues
                {
                    InsActivePower = new InstantaneousActivePower
                    {
                        TotalPowerPhases = BitConverter.ToUInt32(answer, 0),
                        InsPowerPhase = new Phase
                        {
                            Phase_A = BitConverter.ToUInt32(answer, 0),
                            Phase_B = 0,
                            Phase_C = 0
                        }
                    },
                    InsReactivePower = new InstantaneousReactivePower
                    {
                        TotalPowerPhases = 0,
                        InsPowerPhase = new Phase
                        {
                            Phase_A = 0,
                            Phase_B = 0,
                            Phase_C = 0
                        }
                    },
                    Voltage = new Phase
                    {
                        Phase_A = 0,
                        Phase_B = 0,
                        Phase_C = 0
                    },
                    Amperage = new Phase
                    {
                        Phase_A = 0,
                        Phase_B = 0,
                        Phase_C = 0
                    },
                    PowerFactor = new Phase
                    {
                        Phase_A = 0,
                        Phase_B = 0,
                        Phase_C = 0
                    },
                    Frequency = 0
                };
                result = OperationResult.Good;
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }

            return result;
        }

        public OperationResult TryReadSerialNumber(IIODriverClient channel, int address, string psw, out string response)
        {
            OperationResult result = OperationResult.Bad;
            response = null;
            var send =  CreateRequest(address, Codes.CODE_READ_SERIAL_NUMBER, psw, 0x00);
            result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            if (!result.IsGood) return result;
            try
            {
                response = Encoding.ASCII.GetString(BitwiseUtils.Reverse(answer));
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }

        public OperationResult TryReadKoef(IIODriverClient channel, int address, string psw, out double koef, out bool isHalfHour)
        {
            OperationResult result = OperationResult.Bad;
            koef = 0;
            isHalfHour = false;
            var send = CreateRequest(address, Codes.CODE_READ_KOEF_CONFIG, psw);
            result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            if (!result.IsGood) return result;

            try
            {
                int resByte = answer[0] & 0x03;
                switch (resByte)
                {
                    case 0: koef = 1; break;
                    case 1: koef = 0.1; break;
                    case 2: koef = 0.01; break;
                    case 3: koef = 0.001; break;
                }
                resByte = answer[1] & 0x03;
                isHalfHour = (resByte == 1);
                result = OperationResult.Good;
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }

            return result;
        }

        /// <summary>Запрос конфигурации устройства</summary>
        /// <param name="channel">Канал</param>
        /// <param name="address">Адрес устройства</param>
        /// <param name="deviceConfiguration">Конфигурация устройства</param>
        /// <returns></returns>
        public OperationResult TryReadDeviceConfiguration(IIODriverClient channel, int address, string psw, out string deviceConfiguration, bool full = false)
        {
            OperationResult result = OperationResult.Bad;
            deviceConfiguration = string.Empty;
            result = channel.TryWaitRequest(timeOutRequestMSec, cancel);
            if (!result.IsGood) return result;

            byte[] send = CreateRequest(address, Codes.CODE_READ_CONFIG, psw);
            result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            if (!result.IsGood) return result;
            try
            {
                StringBuilder sb = new StringBuilder();
                //byte 4 - type
                switch (answer[3])
                {
                    case 1: sb.Append("CE102 "); break;
                    case 2: sb.Append("CE301M "); break;
                    default: sb.Append("Неизвестный тип "); break;
                }
                //byte 12 - корпус
                switch (answer[11])
                {
                    case 1: sb.Append("R5 "); break;
                    case 2: sb.Append("R8 "); break;
                    case 3: sb.Append("S6 "); break;
                    case 4: sb.Append("S7 "); break;
                    case 5: sb.Append("S31 "); break;
                    case 6: sb.Append("R33 "); break;
                    default: sb.Append("Неизвестная модель "); break;
                }
                //byte 3 - version
                sb.Append("(v.").Append(answer[2]).Append(") ");

                if (full)
                {
                    if ((answer[13] & 0x40) == 0x40) sb.Append("A");
                    if ((answer[13] & 0x20) == 0x20) sb.Append("O");
                    if ((answer[13] & 0x10) == 0x10) sb.Append("K");
                    if ((answer[13] & 0x08) == 0x08) sb.Append("P");
                    if ((answer[13] & 0x04) == 0x04) sb.Append("Q");
                    if ((answer[13] & 0x02) == 0x02) sb.Append("R");
                    if ((answer[13] & 0x01) == 0x01) sb.Append("S");
                    if ((answer[12] & 0x80) == 0x80) sb.Append("V");
                    if ((answer[12] & 0x40) == 0x40) sb.Append("Z");
                    if ((answer[12] & 0x20) == 0x20) sb.Append("J");
                    if ((answer[12] & 0x10) == 0x10) sb.Append("G");
                    if ((answer[12] & 0x08) == 0x08) sb.Append("I");
                    if ((answer[13] & 0x80) == 0x80) sb.Append(" RS232");

                    sb.Append(" Класс точн.:");
                    switch (answer[5])
                    {
                        case 1: sb.Append("0.5, "); break;
                        case 2: sb.Append("1.0, "); break;
                        case 3: sb.Append("2.0, "); break;
                        default: sb.Append("Неизвестно, "); break;
                    }
                    sb.Append("Uн:");
                    switch (answer[6])
                    {
                        case 1: sb.Append("57.7 В, "); break;
                        case 2: sb.Append("100 В, "); break;
                        case 3: sb.Append("127 В, "); break;
                        case 4: sb.Append("230 В, "); break;
                        case 5: sb.Append("220 В, "); break;
                        default: sb.Append("Неизвестно, "); break;
                    }
                    sb.Append("I:");
                    switch (answer[7])
                    {
                        case 1: sb.Append("1-1.5 А"); break;
                        case 2: sb.Append("5-7.5 А"); break;
                        case 3: sb.Append("5-50 А"); break;
                        case 4: sb.Append("5-60 А"); break;
                        case 5: sb.Append("10-100 А"); break;
                        case 6: sb.Append("1-7.5 А"); break;
                        case 7: sb.Append("5-10 А"); break;
                        default: sb.Append("Неизвестно"); break;
                    }
                }
                deviceConfiguration = sb.ToString();
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }

        #region Чтение и запись даты и времени
        private DateTimeZone ParseDateTime(byte[] answer, TimeZoneMap zone)
        {
            var year = 2000 + BitwiseUtils.BcdToDec(answer[6]);
            var month = BitwiseUtils.BcdToDec(answer[5]);
            var day = BitwiseUtils.BcdToDec(answer[4]);
            int hour = BitwiseUtils.BcdToDec(answer[2]);
            int min = BitwiseUtils.BcdToDec(answer[1]);
            int sec = BitwiseUtils.BcdToDec(answer[0]);

            var ret = new DateTimeZone(year, month, day, hour, min, sec, zone);
            return ret;
        }

        public OperationResult TryReadDateTime(IIODriverClient channel, int address, string psw, TimeZoneMap zone, out DateTimeUtc response, out int timeTwoSidePathMsec)
        {
            OperationResult result = OperationResult.Bad;
            response = DateTimeUtc.MinValue;
            timeTwoSidePathMsec = 0;
            byte[] send = CreateRequest(address, Codes.CODE_READ_DATETIME, psw);

            result = WaitRequest(channel);
            if (!result.IsGood) return result;

            var span = SpanSnapshot.Now;
            result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            timeTwoSidePathMsec = (int)span.DiffNowMsec();
            if (!result.IsGood) return result;

            try
            {
                response = ParseDateTime(answer, zone);
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }

        public OperationResult TryWriteDateDate(IIODriverClient channel, int address, string psw, TimeZoneMap map, DeviceCompOn diff)
        {
            // Установка даты и времени защищена основным паролем.

            OperationResult result = WaitRequest(channel);
            if (!result.IsGood) return result;

            DateTimeZone dateTime = diff.GetServerToDeviceTime(map);
            byte[] arr = new byte[7];
            arr[0] = BitwiseUtils.DecToBCD((byte)dateTime.Second);
            arr[1] = BitwiseUtils.DecToBCD((byte)dateTime.Minute);
            arr[2] = BitwiseUtils.DecToBCD((byte)dateTime.Hour);
            arr[3] = (byte)dateTime.DayOfWeek;
            arr[4] = BitwiseUtils.DecToBCD((byte)dateTime.Day);
            arr[5] = BitwiseUtils.DecToBCD((byte)dateTime.Month);
            arr[6] = BitwiseUtils.DecToBCD((byte)(dateTime.Year - 2000));

            try
            {
                var send = CreateRequest(address, Codes.CODE_WRITE_DATETIME, psw, arr);
                result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }

        #endregion

        #region Мощность
        /*public OperationResult TryReadAvPower3min(IIODriverClient channel, string psw, DeviceCompOn deviceTime, TimeZoneMap map, RequestIndex request, DateTimeZone cutTime, out AvPowerIndex response)
        {
            response = null;
            var result =  TryReadPower(channel, psw, request, Codes.CODE_3AVPWR_INX, out response);
            if (result.IsGood) if (!response.Check(cutTime)) result = new OperationResult(Quality.Bad, SR.ERROR_CUT);
            return result;
        }

        OperationResult TryReadPower(IIODriverClient channel, string psw, RequestIndex request, byte action, out AvPowerIndex response)
        {
            OperationResult result = OperationResult.Bad;
            response = null;
            var send = CreateRequest(request.Address, action, psw, request.Index, 0, 0);
            result = WriteReadCheck(channel, 1, send, out byte[] answer);
      
            if (!result.IsGood) return result;
            var seg = new ByteArraySegment(answer, 0, answer.Length);
            try
            {
            response = new AvPowerIndex
            {
                Power = new AvPower
                {
                Pplus = seg.ReadSingle(0),
                Pminus = seg.ReadSingle(1),
                Qplus = seg.ReadSingle(2),
                Qminus = seg.ReadSingle(3),
                },
                Index = seg.ReadUInt16(0 + (4 * sizeof(Single)))
            };
            result = OperationResult.Good;
            }
            catch (Exception e)
            {
            result = OperationResult.From(e);
            }
            return result;
        }*/
        #endregion

        #region Энергия
        /// <summary>Прочитать показание счетчиков</summary>
        public OperationResult TryReadCounter(
            IIODriverClient channel,
            string psw,
            TypeInc typeInc,
            RequestIndex request,
            ETariff tariff,
            out Energy response)
        {
            response = null;
            ushort action;
            byte[] arr;
            OperationResult result = new OperationResult(Quality.BadNoRestore, "Превышение глубины опроса");
            if (typeInc == TypeInc.Day)
            {
                if ((request.Index > EmeraRequest.DepthDayCounter) || (request.Index > 45)) return result;
                if (tariff == ETariff.NoTariff)
                {
                    action = Codes.CODE_READ_ENERGY_DAY_SUM;
                    arr = new byte[1];
                    arr[0] = request.Index;
                }
                else
                {
                    action = Codes.CODE_READ_ENERGY_DAY_TARIFF;
                    arr = new byte[2];
                    arr[0] = (byte)(tariff - 1);
                    arr[1] = request.Index;
                }
            }
            else if (typeInc == TypeInc.Month)
            {
                if ((request.Index > EmeraRequest.DepthMonthCounter) || (request.Index > 13)) return result;
                if (tariff == ETariff.NoTariff)
                {
                    action = Codes.CODE_READ_ENERGY_MONTH_SUM;
                    arr = new byte[1];
                    arr[0] = request.Index;
                }
                else
                {
                    action = Codes.CODE_READ_ENERGY_MONTH_TARIFF;
                    arr = new byte[2];
                    arr[0] = (byte)(tariff - 1);
                    arr[1] = request.Index;
                }
            }
            else
            {
                return OperationResult.Bad;
            }

            var send = CreateRequest(request.Address, action, psw, arr);
            result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            if (!result.IsGood) return result;

            var seg = new ByteArraySegment(answer, 0, answer.Length);

            try
            {
                response = new Energy(seg.ReadUInt32(0), 0, 0, 0);
                result = OperationResult.Good;
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
   

        /// <summary>Показание счетчика с момента запуска</summary>
        /*private OperationResult TryReadEnergyFromLaunch(IIODriverClient channel, ShortRequest request, out Energy response)
        {
            return TryReadEnergy(channel, request, Codes.CODE_TOTAL, 0, 0, 0, out response);
        }*/
        #endregion

        #region Энергия по времени

        /// <summary>Чтение 30 минутных срезов энергии</summary>
        /// <param name="channel">Канал</param>
        /// <param name="request">Запрос</param>
        /// <param name="timeSlices">Время среза должно быть точное и выравненне</param>
        public OperationResult TryReadSlicesEnergy(IIODriverClient channel, string psw, DeviceCompOn deviceTime, SlicesQuery request, out Energy response)
        {
            response = Energy.Default;
            //log.Trace.Write(1, (l) => l.Info("Emul: {0}", request.EmulError));
            if (!request.EmulError)
            {
                //log.Trace.Write(1, (l) => l.Info("DiffMsec: {0}, DiffMsecReal: {1}, DeviceTimeUtc: {2}, IsTimeSlices: {3}, TimeOneSidePathMsec: {4}, TimeSlices: {5}",
                //  deviceTime.DiffMsec, deviceTime.DiffMsecReal, deviceTime.GetDeviceTimeUtc(),
                //  deviceTime.IsItTime(request.GetTimeSlicesFrom30()), deviceTime.TimeOneSidePathMsec, request.GetTimeSlicesFrom30()));
                if (!deviceTime.IsItTime(request.GetTimeSlicesFrom30().AddSeconds(10))) return new OperationResult(Quality.Bad, "Time has not yet come");
            }

            OperationResult result;
            try
            {
                byte[] arr = new byte[5];
                arr[0] = BitwiseUtils.DecToBCD(request.Day);
                arr[1] = BitwiseUtils.DecToBCD(request.Month);
                arr[2] = BitwiseUtils.DecToBCD((byte)(request.TimeSlices.Year - 2000));
                arr[3] = request.Index;
                arr[4] = 1;

                var send = CreateRequest(request.Address, Codes.CODE_READ_ENERGY_INTERVAL, psw, arr);
                result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);

                Array.Resize<byte>(ref answer, 4);
                var seg = new ByteArraySegment(answer, 0, answer.Length);
                var value = seg.ReadUInt32(0);
                if (value == 0xFFFFFF)
                {
                    response = new Energy(0, 0, 0, 0); //это значит что его нет в памяти и надо возвращать нули
                    return OperationResult.Bad; 
                }
                response = new Energy(value, 0, 0, 0);
                result = OperationResult.Good;
            }
            catch (Exception e)
            {
                result = OperationResult.From(e);
            }
            return result;
        }
        #endregion

        #region Архив событий

        /*public OperationResult TryGetAnyEvents(IIODriverClient channel, int address, string psw, EmeraArchiveEvents archEvts,
            byte index, out Event response, TimeZoneMap zone)
        {
            response = null;
            OperationResult result = OperationResult.Bad;
            var send = CreateRequest(address, (byte)archEvts, psw, index, 0, 0);

            result = WriteReadCheck(channel, nRepeatGlobal, send, out byte[] answer);
            try
            {
            if (result.IsGood)
            {
                var seg = new ByteArraySegment(answer, 0, answer.Length);
                DateTimeZone dtz = new DateTimeZone(
                        seg.Array[5] + 2000,
                        seg.Array[4],
                        seg.Array[3],
                        seg.Array[2],
                        seg.Array[1],
                        seg.Array[0], zone);
                response = new Event(dtz,
                (int)((((seg.Array[7] << 0x10) | (seg.Array[6] << 0x8)) | seg.Array[8])),
                archEvts);
            }
            }
            catch (Exception e)
            {
            result = OperationResult.From(e);
            }
            return result;
        }*/

        #endregion

        public OperationResult WaitRequest(IIODriverClient channel)
        {
            return channel.TryWaitRequest(timeOutRequestMSec, cancel);
        }


        //Записать и считать пакет 
        private OperationResult WriteReadCheck(IIODriverClient channel, int nRepet, byte[] sendBuf, out byte[] readBuf)
        {
            byte[] outBuf = null;
            OperationResult result = DriverTransport.TryRepeater(nRepet, WaitAnswerMSec, true, channel, cancel, TimeRange.None, () =>
            {
                var res = WriteReadCheckPrivate(channel, sendBuf, out outBuf);
                return new OperationData<bool>(true, res);
            });
            readBuf = outBuf;
            return result;
        }


        /// <summary>Записать и считать пакет</summary>
        /// <param name="channel"></param>
        /// <param name="sendBuf"></param>
        /// <param name="readBuf"></param>
        /// <returns></returns>
        OperationResult WriteReadCheckPrivate(IIODriverClient channel, byte[] sendBuf, out byte[] readPacket)
        {
            readPacket = null;
            OperationResult result = WaitRequest(channel);
            if (!result.IsGood) return result;

            try
            {
                channel.DiscardInBuffer();
                channel.DiscardOutBuffer();
                channel.Write(sendBuf, 0, sendBuf.Length);

                List<byte> read = new List<byte>();
                bool isRead = true;
                do
                {
                    int d = channel.ReadByte();
                    if (d < 0) isRead = false;
                    else
                    {
                        byte b = (byte)d;
                        read.Add(b);
                        if (b == Codes.BYTE_END && read.Count > 1)
                        {
                            isRead = false;
                        }
                    }
                } while (isRead);
                byte[] readBuf = decodePacket(read.ToArray());
                sendBuf = decodePacket(sendBuf);

                channel.DiscardInBuffer();
                channel.DiscardOutBuffer();

                if (readBuf.Length < 9 || readBuf[0] != Codes.BYTE_END || readBuf[readBuf.Length - 1] != Codes.BYTE_END) return new OperationResult(Quality.Bad, SR.MACHINE_ERROR);
                if (sendBuf[2] != readBuf[4] || sendBuf[3] != readBuf[5]) return new OperationResult(Quality.Bad, SR.NOT_COMPLIANT_ADDRESS);
                if (sendBuf[11] != readBuf[7] || sendBuf[12] != readBuf[8]) return new OperationResult(Quality.Bad, SR.NOT_COMPLIANT_FUNCTION);

                var crc = CRC.getCRC(readBuf, 2);
                if (crc != readBuf[readBuf.Length - 2]) return new OperationResult(Quality.BadCRC, SR.CRC);

                //сообщение об ошибке
                if ((readBuf[6] & 0xF0) == Codes.BYTE_ERR) return new OperationResult(Quality.Bad, ErrorsDescription.GetError(readBuf[9]));
                if ((readBuf[6] & 0xF0) != Codes.BYTE_SERV_ANSWER) return new OperationResult(Quality.Bad, SR.BLOCK_DAMAGED);
                int packetLength = readBuf.Length - 11;
                if ((readBuf[6] & 0x0F) != (byte)packetLength) return new OperationResult(Quality.Bad, SR.PACKET_LENGTH_ABNORMAL);

                readPacket = new byte[packetLength];
                int i = 0;
                while (i < packetLength)
                {
                    readPacket[i] = readBuf[i + 9];
                    i++;
                } 
                return result;
            }
            catch (Exception e)
            {
                result.Quality = e.ToQuality();
                result.ErrorMsg = e.GetFullMessage();
            }
            finally
            {
                //sw.Restart();
            }
            return result;
        }

        /// <summary>Создать запрос на счетчик Энергомера</summary>
        /// <param name="addresssDevice">Адрес устройства</param>
        /// <param name="codeCommand">Код команды</param>
        /// <param name="data">Необязательный массив параметров для комманды</param>
        byte[] CreateRequest(int addresssDevice, ushort codeCommand, string psw, params byte[] data)
        {
            var buff = new List<byte>();
            buff.Add(Codes.BYTE_END);
            buff.Add(Codes.BYTE_OPT);
            buff.AddRange(BitConverter.GetBytes((ushort)addresssDevice));
            buff.Add(0);
            buff.Add(0);

            //пакет
            for (int i = 0; i < 4; i++)
            {
                byte b = byte.Parse(psw.Substring(i,1));
                buff.Add(b);
            }
            /*buff.Add(0);
            buff.Add(0);
            buff.Add(0);
            buff.Add(0);*/
            byte serv = (byte)(Codes.BYTE_SERV | data.Length);
            buff.Add(serv);
            buff.AddRange(BitwiseUtils.Reverse(BitConverter.GetBytes(codeCommand)));
            for (int i = 0; i < data.Length; i++)
            {
                buff.Add(data[i]);
            }
            buff.Add(CRC.getCRC(buff.ToArray(), 0));
            var list = encodePacket(buff);
            list.Add(Codes.BYTE_END);
            return list.ToArray();
        }

        //замена ключевых символов после создания пакета запроса
        private List<byte> encodePacket(List<byte> byteList)
        {
            List<byte> dataByte = new List<byte>();
            dataByte.Add(byteList.First());
            byteList.RemoveAt(0);
            foreach (byte b in byteList)
            {
                if (b == Codes.BYTE_END)
                {
                    dataByte.Add(Codes.BYTE_ESC);
                    dataByte.Add(Codes.BYTE_REPLACE_END);
                }
                else if (b == Codes.BYTE_ESC)
                {
                    dataByte.Add(Codes.BYTE_ESC);
                    dataByte.Add(Codes.BYTE_REPLACE_ESC);
                }
                else
                {
                    dataByte.Add(b);
                }
            }
            return dataByte;
        }

        //замена на ключевые символы после получения ответа
        private byte[] decodePacket(byte[] byteArr)
        {
            List<byte> dataByte = new List<byte>();
            bool flag = false;
            foreach (byte b in byteArr)
            {
                if (b == Codes.BYTE_ESC)
                {
                    flag = true;
                }
                else
                {
                    if (flag)
                    {
                        flag = false;
                        if (b == Codes.BYTE_REPLACE_END) dataByte.Add(Codes.BYTE_END);
                        else if (b == Codes.BYTE_REPLACE_ESC) dataByte.Add(Codes.BYTE_ESC);
                        else
                        {
                            dataByte.Add(Codes.BYTE_ESC);
                            dataByte.Add(b);
                        }
                    }
                    else
                    {
                        dataByte.Add(b);
                    }

                }
            }
            return dataByte.ToArray();
        }
    }
}
