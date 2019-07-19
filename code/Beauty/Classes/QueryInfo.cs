using Sys.DataBus.Common;
using Sys.Diagnostics.Logger;
using Sys.Services.Components;
using Sys.Services.Drv.Emera.Culture;
using Sys.Services.Drv.Emera.Def;
using Sys.Services.Drv.Emera.Transport;
using Sys.Types.Components;
using Sys.Types.Components.DataDriverClient;
using Sys.Types.Om;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Services.Drv.Emera
{
    internal class QueryInfo
    {
        public TagDef Events { get { return driver.Events; } }
        public StorageDataDriver Storage { get { return driver.Storage; } }
        public LogObject Log { get { return driver.Log; } }
        public string DisplayName { get { return driver.DisplayName; } }
        //В чем брать информацию со счетчика
        public SynchRequestDataDrv RequestData { get; private set; }
        public SynchParamsDataDrv RequestParam { get; private set; }
        public string DeviceConfiguration { get; private set; }
        public double DeviceKoef { get; private set; }
        public bool IsHalfHourInterval { get; private set; }
        public EmeraRequest Request { get; private set; }
        public IIODriverClient DataBus { get; private set; }
        public EmeraSharedSetting Ss { get; private set; }
        public EmeraContentSetting Cs { get; private set; }
        public AccountNextPoint NextPoint { get; private set; }
        public DriverSetting DriverSetting { get; private set; }
        public ElectroChannel ElectroChannel { get; private set; }
        public PhysicalSession<EmeraSynchState, AccountNextPoint> Session { get; private set; }
        /// <summary>
        /// текущее время в локале прибора
        /// </summary>
        public DateTimeZone NowTimeInZone
        {
            get
            {
                return NextPoint.Zone.Now;
            }
        }

        private EmeraDriver driver;

        public QueryInfo(
          EmeraDriver driver,
          SynchRequestDataDrv requestData,
          SynchParamsDataDrv requestParam,
          EmeraSharedSetting ss,
          EmeraContentSetting cs,
          DriverSetting driverSetting)
        {
            RequestData = requestData;
            RequestParam = requestParam;
            this.driver = driver;
            Request = new EmeraRequest(driver, driverSetting, driver.ReadTimeOutRequestMSec());
            DriverSetting = driverSetting;
            DataBus = driver.Channel;
            Ss = ss;
            Cs = cs;
            NextPoint = new AccountNextPoint(
              TimeZoneMap.Local,
              timeOffset: TimeOffset.Level_1,
              useMin3: false,
              useMin30: ss.Enbl30min,
              useDay1: true,
              useMonth1: true,
              useYear1: false,
              archSync: ss.Arch.ToSch());
            ElectroChannel = driver.ElectroChannel.ByBrowseName<ElectroChannel>(ElectroChannels.BN.ElectroChannel);
        }

        internal void WriteLog2(string message, params object[] args)
        {
            driver.Log.Trace.Write(2, (l) => l.Info(message, args));
        }

        internal void SetDeviceConfiguration(string deviceConfiguration)
        {
            DeviceConfiguration = deviceConfiguration;
        }

        internal void SetDeviceKoef(double deviceKoef)
        {
            DeviceKoef = deviceKoef;
        }

        internal void SetHalfHourInterval(bool value)
        {
            IsHalfHourInterval = value;
        }


        #region (GetDiscrette)
        private void GetDiscrette(TypeInc typeInc, out Discrete disc, out int discVal)
        {
            disc = Discrete.Min;
            discVal = 3;
            if (typeInc == TypeInc.Min30) discVal = 30;
            else if (typeInc == TypeInc.Day)
            {
                disc = Discrete.Day;
                discVal = 1;
            }
            else if (typeInc == TypeInc.Month)
            {
                disc = Discrete.Month;
                discVal = 1;
            }
            else if (typeInc == TypeInc.Year)
            {
                disc = Discrete.Year;
                discVal = 1;
            }
        }
        #endregion

        internal IEnumerable<DateTimeZone> GetHoles(
          TypeInc typeInc,
          DateTimeZone fromTime,
          int depth,
          DateTimeUtc deepSyncTime,
          TagDef[] tagsWatch)
        {
            Discrete disc;
            int discVal;
            GetDiscrette(typeInc, out disc, out discVal);

            DateTimeZone beginTime = RequestParam.GetFromSync(() => fromTime, true, disc, discVal);

            var holes = driver.Storage.ReadHoles(
              RequestParam.HolesMode,
              beginTime,
              depth - 1,
              deepSyncTime,
              tagsWatch);
            this.driver.Log.Trace.Info(2, SR.ReadHoles, fromTime, depth, deepSyncTime, beginTime, holes.Count());
            return holes;
        }

        internal void SetSession(PhysicalSession<EmeraSynchState, AccountNextPoint> session)
        {
            Session = session;
        }

        internal void LogWarn(string message, params object[] args)
        {
            driver.Log.Sys.Warn(SLD.AdminNotif, message, args);
        }

    }
}
