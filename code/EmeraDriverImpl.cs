#define ALL_3MIN_ENABLED
#define ALL_30MIN_ENABLED
#define ALL_DAY_ENABLED
#define ALL_MONTH_ENABLED
#define ALL_YEAR_ENABLED

#define Min3
#define Min30
#define Inc

using Sys.Configuration;
using Sys.DataBus.Common;
using Sys.Diagnostics.Logger;
using Sys.Services.Components;
using Sys.Services.Drv.Emera.Culture;
using Sys.StorageModel;
using Sys.Types.Components;
using Sys.Types.Components.DataDriverClient;
using Sys.Types.HistoryWriter;
using Sys.Types.NodeEditor;
using Sys.Types.Om;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sys.Services.Drv.Emera
{
  public class EmeraDriver : Sys.Types.Components.DriverElectroClient
  {
    /// <summary>Идентификатор типа реализации компонента</summary>
    public const string TypeGuidImpl = "est.by:Bus.EmeraDrvClientImpl";

    #region (class MsgTest)
    internal class MsgTest
    {
      public OperationResult OperationResult;
      public String Message;

      public MsgTest(OperationResult operationResult, String message = "")
      {
        this.OperationResult = operationResult;
        this.Message = message;
      }
    }
    #endregion

    #region (enum TypeQuery)
    enum TypeQuery
    {
      Power3min,
      Power30min,
      Inc,
      Counter,
    }
    #endregion

    #region (enum TypeInc)
    /// <summary>Тип накопленной энергии</summary>
    public enum TypeInc
    {
      Min3,
      Min30,
      Day,
      Month,
      Year
    }
    #endregion

    #region (Static)
    static FineTune FineTune;

    #region (Constructor)
    static EmeraDriver()
    {
      FineTune = FineTune.TryLoad("meselectro");
    }
    #endregion

    #region (DesignChange)
    internal static void DesignChange(Sys.Types.Om.INodeEditorContext context, object state)
    {
      if ((context.Action != NodeEditorAction.AfterAdd) && (context.Action != NodeEditorAction.AfterEdit)) return;

      if (context.Action == NodeEditorAction.AfterEdit)
        context.DeleteChildDynNodes(context.NodeId, DriverElectroClient.BN.ElectroChannel, true);

      context.AddDynNode(context.NodeId, new Node
      {
        BrowseName = DriverElectroClient.BN.ElectroChannel,
        LinkType = LinkType.Hard,
        DisplayName = context.NodeDisplayName,
        IdComponentType = Sys.Types.Components.ElectroChannel.TypeGuidImpl,
      }, null);
    }
    #endregion

    private static HistoryData HW(Quality quality, double @value, DateTimeUtc timeWrite, TagDef tagDef)
    {
      return new HistoryData(tagDef, new TagData(quality, DateTimeUtc.Now, value), timeWrite, HistoryWriterKind.InsertUpdate);
    }

    private static HistoryData HW(Quality quality, DataDriverEvent @value, DateTimeUtc timeWrite, TagDef tagDef)
    {
      return new HistoryData(tagDef, new TagData(quality, DateTimeUtc.Now, TagValue.FromObject(DataType.Structured, value)), timeWrite, HistoryWriterKind.Insert);
    }
    #endregion

    public Int32 ReadTimeOutRequestMSec()
    {
      return FineTune.ReadValue<int>("TimeOutRequestMSec", this, (v) => int.Parse(v), Transport.TimeOutRequestMSecDeafult);
    }
    public Boolean ReadUseAnaliticInPok()
    {
      return FineTune.ReadValue<bool>("ReadUseAnaliticInPok", this, (v) => bool.Parse(v), true);
    }
    public EPokPolitic ReadPokazPolitic()
    {
      return (EPokPolitic)FineTune.ReadValue<byte>("PokPolitic", this, (v) => byte.Parse(v), 0);
    }

    /// <summary>
    /// Политика для показаний
    /// </summary>
    public enum EPokPolitic:byte {
      /// <summary>
      /// мягкая
      /// </summary>
      Soft=0,
      /// <summary>
      /// Жесткая
      /// </summary>
      Hard=1
    }

    #region (override IsReadIm)
    public override ModeDataDrv IsReadIm(SynchRequestDataDrv request)
    {
      ModeDataDrv result = ModeDataDrv.Manual;
      var ss = request.GetSharedSetting<EmeraSharedSetting>(() => new EmeraSharedSetting());
      if ((ss.EnblIm) && !ss.Im.IsEmpty()) result |= ModeDataDrv.Auto;
      return result;
    }
    #endregion

    #region (override IsSynch)
    public override ModeDataDrv IsSynch(SynchRequestDataDrv request)
    {
      return ModeDataDrv.All;
    }
    #endregion

    #region (override IsWriteIm)
    public override ModeDataDrv IsWriteIm(SynchRequestDataDrv request)
    {
      return ModeDataDrv.None;
    }
    #endregion

    #region (override WriteIm)
    public override SynchResponse WriteIm(SynchRequestDataDrv request, WriteImParamsDataDrv param)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region (override ReadIm)
    public override SynchResponse ReadIm(SynchRequestDataDrv request, ReadImParamsDataDrv param)
    {
      var ss = request.GetSharedSetting<EmeraSharedSetting>(() => new EmeraSharedSetting());
      var cs = request.GetContentSetting<EmeraContentSetting>(() => new EmeraContentSetting());
      var drvSetting = request.GetDriverSetting(() => new DriverSetting());
      Transport transport = new Transport(this.Log, this, drvSetting, ReadTimeOutRequestMSec());
      ImNextPoint nextPoint = new ImNextPoint(TimeZoneMap.Local, 0, imNextItem: new ImNextItem("im", ss.Im.ToSch()));
      using (var session = new PhysicalSessionIm<EmeraSynchState, ImNextPoint>(this, request, nextPoint))
      {
        session.Open();
        if (!session.LaunchPoint(nextPoint.GetItem("im"))) return session;
        if (session.BeginOperation())
        {
          
          InstantaneousValues iv;
          OperationResult resData = OperationResult.Good;
          if (DataBusSetting.StubData) iv = new InstantaneousValues(true);
          else resData = transport.TryReadInstantaneousValues(this.Channel, cs.Address, out iv);
          session.EndOperation(resData);
          Log.Trace.Write(1, (l) => l.Info(SR.READ_IM, resData.ToString()));
          if (resData.IsGood)
          {
            ElectroChannel electroChannel = this.ElectroChannel.ByIndex<ElectroChannel>(0);
            iv.WriteTags(this.Storage, electroChannel, resData.Quality, DateTimeUtc.Now);
          }
          
        }
        return session;
      }
    }
    #endregion

    #region (override Synch)
    public override SynchResponse Synch(SynchRequestDataDrv request, SynchParamsDataDrv param)
    {
      Version softVetsion = new Version(0, 0);
      var ss = request.GetSharedSetting<EmeraSharedSetting>(() => new EmeraSharedSetting());
      var cs = request.GetContentSetting<EmeraContentSetting>(() => new EmeraContentSetting());
      AccountNextPoint nextPoint = new AccountNextPoint(
        TimeZoneMap.Local,
        timeOffset: TimeOffset.Level_1,
        useMin3: ss.Enbl3min,
        useMin30: true,
        useDay1: true,
        useMonth1: true,
        useYear1: true,
        archSync: ss.Arch.ToSch());

      var drvSetting = request.GetDriverSetting(() => new DriverSetting());
      //Читаем тонкие настройки по драйверу по задержки между посылками
      Transport transport = new Transport(this.Log, this, drvSetting, ReadTimeOutRequestMSec());
      var nowZone = nextPoint.Zone.Now; //текущее время в локале прибора

      //Любая работа с устройством должна начинаться с открытия сессии
      using (var session = new PhysicalSession<EmeraSynchState, AccountNextPoint>(this, request, nextPoint))
      {
        #region (OnReadDateTime)
        session.OnReadDateTime = () =>  //чтение времени
          {
            if (DataBusSetting.StubData) return StubUtil.StubOnReadDateTime();

            DateTimeUtc dateTimeUtc;
            int timeTwoSidePathMsec;
            var res = transport.TryReadDateTime(this.Channel, cs.Address, nextPoint.Zone, out dateTimeUtc, out timeTwoSidePathMsec);
            return new OperationData<ReadDateTime>(new ReadDateTime(false, dateTimeUtc, timeTwoSidePathMsec), res);
          };
        #endregion

        #region (OnReadPhysicalInfo)
        session.OnReadPhysicalInfo = () => //чтение серийного времени и т.д.
          {
            if (DataBusSetting.StubData) return StubUtil.StubOnReadPhysicalInfo();

            var pInfo = new PhysicalInfo();
            var res = transport.TryReadSN(this.Channel, cs.Address, out pInfo.SerialNumber);
            if (!res.IsGood) return OperationData<PhysicalInfo>.Bad(res);
            pInfo.SerialNumber = pInfo.SerialNumber.Trim();
            Version ver;
            transport.TryReadVersion(this.Channel, cs.Address, out ver);
            pInfo.SoftVersion = ver.ToString();
            return new OperationData<PhysicalInfo>(pInfo, res);
          };
        #endregion

        if (ss.EnblTimeCorr)
        {
          #region (OnWriteDateTime)
          session.OnWriteDateTime = (DeviceCompOn diff) => //записть времени
            {
              if (DataBusSetting.StubData) return OperationData<bool>.Good(true);
              //#warning Проверить как записывается время!
              var res = transport.TryWriteDateDate(this.Channel, cs.Address, nextPoint.Zone, ss.Level, diff);
              return new OperationData<bool>(res.IsGood, res);
            };
          #endregion
        }

        #region (OnBeforeOpenSession)
        session.OnBeforeOpenSession = () => //произвольные операции перед открытии сессии
          {
            if (DataBusSetting.StubData)
            {
              cs.KU = 1;
              cs.KI = 1000;
              return StubUtil.StubOnBeforeOpenSession();
            }

            var res = transport.TryReadVersion(this.Channel, cs.Address, out softVetsion);
            if (!res.IsGood) return res;

            if (cs.KU_KI_Dev)
            {
              double ki, ku;
              res = transport.TryReadKI(this.Channel, cs.Address, out ki);
              if (!res.IsGood) return res;
              if (res.IsGood)
                cs.KI = ki;

              res = transport.TryReadKU(this.Channel, cs.Address, out ku);
              if (!res.IsGood) return res;
              if (res.IsGood)
                cs.KU = ku;
            }

            Log.Trace.Write(2, (l) => l.Info(SR.ReadKPRKIKU, cs.KI, cs.KU));
            return res;
          };
        #endregion

        session.AutoOpen = true;

        ElectroChannel electroChannel = this.ElectroChannel.ByBrowseName<ElectroChannel>(ElectroChannels.BN.ElectroChannel);
        var now = session.Zone.Now;
        var eDef = electroChannel.Energy;
        var eDefCounter = electroChannel.Counter;

#if (ALL_3MIN_ENABLED)
        if (session.LaunchPoint(nextPoint.Min3))
        {
        #region (...)
          //Запись кэфициентов
          Storage.WriteTagsValueIfChanged(session.StateStorage, new HistoryDataIf[]
          {
            new HistoryDataIf(electroChannel.CI, new TagData(Quality.Good, now.ToUtc(), (double)cs.KI), now),
            new HistoryDataIf(electroChannel.CU, new TagData(Quality.Good, now.ToUtc(), (double)cs.KU), now)
          });

          ReadEnergy(
            session,
            param,
            transport,
            cs,
            eDef,

            TimeStep.Minute_3.Round(nowZone),
            TypeQuery.Power3min,
            TypeInc.Min3,
            eDef.Aplus.Min3,
            eDef.Aminus.Min3,
            eDef.Rplus.Min3,
            eDef.Rminus.Min3,
            2,
            param.DeepSync);
            if (ss.EnblEvents) ReadAllEvents(session, nextPoint, transport, cs);
        #endregion
        }
#endif
#if (ALL_30MIN_ENABLED)
        if (session.LaunchPoint(nextPoint.Min30) || param.HolesMode == QueryHolesMode.WithoutGeneration || param.HolesMode == QueryHolesMode.AllData)
        {
        #region (...)
          //Запись кэфициентов
          Storage.WriteTagsValueIfChanged(session.StateStorage, new HistoryDataIf[]
          {
            new HistoryDataIf(electroChannel.CI, new TagData(Quality.Good, now.ToUtc(), (double)cs.KI), now),
            new HistoryDataIf(electroChannel.CU, new TagData(Quality.Good, now.ToUtc(), (double)cs.KU), now)
          });

          ReadEnergy(
            session,
            param,
            transport,
            cs,
            eDef,

            TimeStep.Minute_30.Round(nowZone),
            TypeQuery.Power30min,
            TypeInc.Min30,
            eDef.Aplus.Min30,
            eDef.Aminus.Min30,
            eDef.Rplus.Min30,
            eDef.Rminus.Min30,
            Transport.Depth30MinDefault,
            FineTuneUtils.ReadDeepSync30Local(
                FineTune,
                this,
                param.DeepSync,
                Transport.Depth30MinDefault));

          if ((!ss.Enbl3min) && (ss.EnblEvents)) ReadAllEvents(session,nextPoint,transport,cs);
        #endregion
        }
#endif
#if (ALL_DAY_ENABLED)
        if (session.LaunchPoint(nextPoint.Day1) || param.HolesMode == QueryHolesMode.WithoutGeneration || param.HolesMode == QueryHolesMode.AllData)
        {
        #region (...)
          ReadEnergy(
            session,
            param,
            transport,
            cs,
            eDef,

            TimeStep.Day_1.Round(nowZone),
            TypeQuery.Inc,
            TypeInc.Day,
            eDef.Aplus.Day1,
            eDef.Aminus.Day1,
            eDef.Rplus.Day1,
            eDef.Rminus.Day1,
            Transport.DepthDay,
            param.DeepSync);

          ReadEnergy(
            session,
            param,
            transport,
            cs,
            eDef,

            TimeStep.Day_1.Round(nowZone),
            TypeQuery.Counter,
            TypeInc.Day,
            eDefCounter.Aplus.Day1,
            eDefCounter.Aminus.Day1,
            eDefCounter.Rplus.Day1,
            eDefCounter.Rminus.Day1,
            Transport.DepthDayCounter,
            param.DeepSync);
        #endregion
        }
#endif
#if (ALL_MONTH_ENABLED)
        if (session.LaunchPoint(nextPoint.Month1) || param.HolesMode == QueryHolesMode.WithoutGeneration || param.HolesMode == QueryHolesMode.AllData)
        {
        #region (...)
          ReadEnergy(
            session,
            param,
            transport,
            cs,
            eDef,

            TimeStep.Month.Round(nowZone),
            TypeQuery.Inc,
            TypeInc.Month,
            eDef.Aplus.Month1,
            eDef.Aminus.Month1,
            eDef.Rplus.Month1,
            eDef.Rminus.Month1,
            Transport.DepthMonth,
            param.DeepSync);

          ReadEnergy(
           session,
           param,
           transport,
           cs,
           eDef,

           TimeStep.Month.Round(nowZone),
           TypeQuery.Counter,
           TypeInc.Month,
           eDefCounter.Aplus.Month1,
           eDefCounter.Aminus.Month1,
           eDefCounter.Rplus.Month1,
           eDefCounter.Rminus.Month1,
           Transport.DepthMonthCounter,
           param.DeepSync);
        #endregion
        }
#endif
#if (ALL_YEAR_ENABLED)
        if (session.LaunchPoint(nextPoint.Year1) || param.HolesMode == QueryHolesMode.WithoutGeneration || param.HolesMode == QueryHolesMode.AllData)
        {
        #region (...)
          ReadEnergy(
            session,
            param,
            transport,
            cs,
            eDef,

            TimeStep.Year_1.Round(nowZone),
            TypeQuery.Inc,
            TypeInc.Year,
            eDef.Aplus.Year1,
            eDef.Aminus.Year1,
            eDef.Rplus.Year1,
            eDef.Rminus.Year1,
            Transport.DepthYear,
            param.DeepSync);

          /*
          ReadEnergy(
           session,
           param,
           transport,
           cs,
           TimeStep.Day_1.Round(nowZone),
           TypeQuery.Counter,
           TypeInc.Year,
           eDefCounter.Aplus.Year1,
           eDefCounter.Aminus.Year1,
           eDefCounter.Rplus.Year1,
           eDefCounter.Rminus.Year1,
           Transport.DepthYearCounter,
           param.DeepSync);
          */
        #endregion
        }
#endif
        Log.Trace.Write(1, (l) => l.Info("Next session from {0} min", Math.Round((session.Result.Next - DateTimeUtc.Now).TotalMinutes), 1));
        return session;
      }
    }
    #endregion

    #region (GetDisc)
    private void GetDisc(TypeInc typeInc, out Discrete disc, out int discVal)
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

    #region (ReadEnergy)
    void ReadEnergy(
      PhysicalSession<EmeraSynchState, AccountNextPoint> session,
      SynchParamsDataDrv param,
      Transport transport,
      EmeraContentSetting cs,
      ElectroEnergy electroEn,

      DateTimeZone fromTime, 
      TypeQuery typeQuery, 
      TypeInc typeInc, 
      TagDef aplus, 
      TagDef aminus, 
      TagDef rplus, 
      TagDef rminus, 
      int depth, 
      DateTimeUtc deepSyncTime)
    {
      bool fillBadNoRestore = false; //Если в гране нет данных то дальше этой даты записываем что нельзя востонавить
      int itemNum = 0;
      OperationResult oper = new OperationResult(Quality.Bad);

      //if (deepSyncTime != DateTimeUtc.MinValue) Log.Trace.Write(1, (l) => l.Info("DeepSyncTime {0}: {1}", typeInc, LogChannel.Var(deepSyncTime.ToLocal())));

      Discrete disc;
      int discVal;
      GetDisc(typeInc, out disc, out discVal);

      var fromTimeHoles = param.GetFromSync(() => fromTime, true, disc, discVal);
      // дыры всегда проверяем по APlus (чтобы не пилить БД)
      var holes = Storage.ReadHoles(
        param.HolesMode, 
        fromTimeHoles, 
        depth - 1, 
        deepSyncTime, 
        aplus);
      
      foreach (var item in holes)
      {
        this.Log.Trace.Write(1, (l) => l.Info("Обработка дыры на время: {0}", item));
        itemNum++;
        Energy read = null;
        OperationResult res = OperationResult.Bad;
        if (fillBadNoRestore)
        {
          read = new Energy(0, 0, 0, 0);
          oper = new OperationResult(Quality.BadNoRestore);
        }
        else if (session.BeginOperation())
        {
          if (DataBusSetting.StubData) //STUBDATA->
          {
            #region (...)
            read = new Energy(
              StubUtil.Int32(500),
              StubUtil.Int32(600),
              StubUtil.Int32(700),
              StubUtil.Int32(800));
            oper = new OperationResult(Quality.Good);
            #endregion
          }
          else
          {
#if (Min3)
            if (typeQuery == TypeQuery.Power3min)
              oper = transport.TryReadSlicesEnergy3n30min(
                Transport.ESlice.Min3,
                this.Channel,
                cs.Address,
                session.TimeDevice,
                item,
                TimeZoneMap.Local,
                out read);
            else 
#else
#warning "3 мин отключены!"
            oper = new OperationResult(Quality.Good);
            read = new Energy(0, 0, 0, 0);
#endif
#if (Min30)
            if (typeQuery == TypeQuery.Power30min)
              oper = transport.TryReadSlicesEnergy(
                this.Channel,
                cs.Address,
                session.TimeDevice,
                item,
                TimeZoneMap.Local,
                out read);
            else 
#else
#warning "30 мин отключены!"
            oper = new OperationResult(Quality.Good);
            read = new Energy(0, 0, 0, 0);
#endif
#if (Inc)
            if (typeQuery == TypeQuery.Inc)
            {
              Transport.PrirEnergy prir = Transport.PrirEnergy.Day;
              switch (typeInc)
              {
                case TypeInc.Day: prir = Transport.PrirEnergy.Day; break;
                case TypeInc.Month: prir = Transport.PrirEnergy.Month; break;
                case TypeInc.Year: prir = Transport.PrirEnergy.Year; break;
                default:
                  throw new NotImplementedException();
              }
              oper = transport.TryReadEnergyPrirash(
                prir,
                this.Channel,
                cs.Address,
                session.TimeDevice,
                item,
                TimeZoneMap.Local,
                out read);
            }
            else 
#else
#warning "чтение приращений энергии отключены!"
            oper = new OperationResult(Quality.Good);
            read = new Energy(0, 0, 0, 0);
#endif
            if (typeQuery == TypeQuery.Counter)
            {
              Transport.NakopEnergy nakop = Transport.NakopEnergy.Day;
              switch (typeInc)
              {
                case TypeInc.Day: nakop = Transport.NakopEnergy.Day; break;
                case TypeInc.Month: nakop = Transport.NakopEnergy.Month; break;
                case TypeInc.Year: nakop = Transport.NakopEnergy.Year; break;
                default:
                  throw new NotImplementedException();
              }
              int indexQuery;
              oper = transport.TryReadEnergyNakop(
                  nakop,
                  this.Channel,
                  cs.Address,
                  session.TimeDevice,
                  item,
                  TimeZoneMap.Local,
                  out read,
                  out indexQuery);
              // здесь есть плавающий баг счетчика, обрабатываем его кодом
              if (oper.IsGood && indexQuery==0 && ReadUseAnaliticInPok())
              {
                // т.к. иногда счетчик врет при чтении тек.значения (index==0), 
                // то перед записью проверяем схождение данных
                // через сумму пред.значения + значение энергии за сутки/месяц

                var resVals = new [] {read.Aplus, read.Aminus, read.Rplus, read.Rminus };
                // читаем последние значения показаний.
                var lastPok = Storage.ReadLastValue(new[] { aplus, aminus, rplus, rminus });
                if (lastPok.Length == 4) {
                  TagDef[] pData = null;
                  switch (typeInc) {
                    case TypeInc.Day:
                      pData = new[] {
                        electroEn.Aplus.Day1,
                        electroEn.Aminus.Day1,
                        electroEn.Rplus.Day1,
                        electroEn.Rminus.Day1,
                      };
                      break;
                    case TypeInc.Month:
                      pData = new[] {
                        electroEn.Aplus.Month1,
                        electroEn.Aminus.Month1,
                        electroEn.Rplus.Month1,
                        electroEn.Rminus.Month1,
                      };
                      break;
                    case TypeInc.Year:
                      pData = new[] {
                        electroEn.Aplus.Year1,
                        electroEn.Aminus.Year1,
                        electroEn.Rplus.Year1,
                        electroEn.Rminus.Year1,
                      };
                      break;
                  }
                  var energyDelta = Storage.ReadLastValue(pData);
                  if (pData.Length == 4) {
                    // только если есть пред.показание и значение приращения,
                    // можно что-то сказать при анализе
                    for (int ik = 0; ik < 4; ik++)
                    {
                      var lp = lastPok[ik];
                      var ed = energyDelta[ik];
                      if (
                        lp.Error == DataError.Ok &&
                        ed.Error == DataError.Ok &&
                        lp.Value.Data.Quality == Quality.Good && 
                        ed.Value.Data.Quality == Quality.Good)
                      {
                        // проверять сумму есть смысл только если время последних тегов одинаковое
                        // и равно текущему запрашиваемому.
                        var tpok = lp.Value.Data.Time.ToDateTimeZone(item.TimeZoneMap);
                        var tdel = ed.Value.Data.Time.ToDateTimeZone(item.TimeZoneMap).AddDays(-1);
                        // показание начало суток, приращение на конец.
                        if (tpok== tdel && tpok == item.AddDays(-1))
                        {
                          // если оба значения есть, смотрим их сумму и сравниваем с полученным показанием.
                          double summ = lp.Value.Data.Value.ToDouble(CultureInfo.InvariantCulture) + ed.Value.Data.Value.ToDouble(CultureInfo.InvariantCulture);
                          if (Math.Abs(summ - resVals[ik]) > 0d)
                          {
                            // сумма не равна полученному значению, 
                            // делаем вывод, что полученная сумма это херня.
                            // вся посылка херня, делаем ее недостоверной и уходим отсюда.
                            oper = OperationResult.Bad;
                            break;
                          }
                        }
                        else
                        {
                          if (ReadPokazPolitic() == EPokPolitic.Hard)
                          {
                            // жесткая политика:
                            // если несчем сравнить то вся посылка плохая
                            oper = OperationResult.Bad;
                            break;
                          }
                          else
                          {
                            // мягкая политика:
                            // если не сошлось время, тогда по этому тегу ничего сказать нельзя, 
                            // смотрим по оставшимся
                            continue;
                          }
                        }
                      }
                      else
                      {
                        if (ReadPokazPolitic() == EPokPolitic.Hard)
                        {
                          //жесткая политика
                          oper = OperationResult.Bad;
                          break;
                        }

                      }
                    }
                  }
                }

              }
            }
            //Может возникать ситуация когда на компе уже наступило время опроса а на счетчике нет и будет возвращен блок поврежден
            if ((itemNum < 3) && (oper.Quality == Quality.BadNoRestore)) oper.Quality = Quality.Bad;
          }

          if ((!oper.IsGood) && (oper.Code == 2))
          {
            if (session.OpenData.NewDevice != ChangeDeviceInfo.None)
              Log.Sys.Warn(SLD.AdminNotif, 
                SR.NOT_PARAM, 
                this.DisplayName, 
                aplus.AccountKind, 
                aplus.Discrete, 
                aplus.DiscreteValue);

            session.EndOperation(OperationResult.Good);
            break; //Неизвестная функция не подерживаемая версией устройства
          }

          session.EndOperation(oper);
          if (oper.IsGood)
            read.Calc(cs, 
              typeQuery == TypeQuery.Power3min || typeQuery == TypeQuery.Power30min);
          if (!oper.IsGood)
          {
            read = new Energy(0, 0, 0, 0);
            this.Channel.DiscardInBuffer();
          }
          if (oper.Quality == Quality.BadNoRestore)
            fillBadNoRestore = true; //заполним все полохими не востанавливаемыми
        }
        else break;

        Storage.WriteTagsValue(
          HW(oper.Quality, read.Aplus, item, aplus),
          HW(oper.Quality, read.Aminus, item, aminus),
          HW(oper.Quality, read.Rplus, item, rplus),
          HW(oper.Quality, read.Rminus, item, rminus));

        if (Log.Trace.IsOn(2))
        {
          var sb = new StringBuilder();
          sb.AppendFormat("Read from [{0}] {1} {2}", item, typeInc, oper.Quality);
          //if (typeQuery != TypeQuery.Power30min)
            sb.Append(" " + typeQuery);

          if (!oper.IsGood)
          {
            sb.AppendFormat(". Error: {0}", oper.ErrorMsg);
            Log.Trace.Error(2, sb.ToString());
          }
          else
          {
            sb.AppendFormat(". A+: {0}, A-: {1}, R+: {2}, R-: {3}", read.Aplus, read.Aminus, read.Rplus, read.Rminus);
            Log.Trace.Info(2, sb.ToString());
          }
        }
      }

      this.Log.Trace.Write(1, (l) => l.Info("Обработано дыр времени: {0}", itemNum));
    }
    #endregion

    #region (ReadEvents)
    void ReadEvents(
      PhysicalSession<EmeraSynchState, AccountNextPoint> session,
      AccountNextPoint nextPoint,
      Transport transport,
      EmeraContentSetting cs,


      MesArchiveEvents typeEvent, 
      Type[] typeFilter) 
    {
      if (DataBusSetting.StubData) return;

      // заполняем фильтр и ищем последенне время для типов событий, с которого нужно запрашивать новую пачку.
      var filter = new StructDataTypeFilter[typeFilter.Length];
      for (int i = 0; i < typeFilter.Length; i++)
        filter[i] = new StructDataTypeFilter(typeFilter[i]);

      var lastVal = Storage.ReadLastValue(filter, this.Events);

      DateTimeZone from = DateTimeZone.MinValue;
      if ((lastVal != null) && (lastVal[0].Error == DataError.Ok))
        from = lastVal[0].Value.Data.Time.ToDateTimeZone(nextPoint.Zone);


      var listEvent = new List<HistoryEvent>();
      OperationResult res;
      Log.Trace.Write(2, (l) => l.Info("Read events from {0}", from.ToString()));
      
      int indexFrom = 0;
      while(indexFrom <= Transport.DepthEvents)
      {
        if (session.BeginOperation())
        {
          Event[] evs;
          res = transport.TryReadEvents(
            indexFrom,
            this.Channel, 
            cs.Address, 
            typeEvent, 
            nextPoint.Zone,
            out evs);

          if (res.IsGood)
          {
            foreach (var ev in evs)
            {
              if (ev.DateEvent <= from)
              {
                // достигли последней даты для типов сообщений, текущего перечисления.
                // значит более читать не нужно
                indexFrom = Transport.DepthEvents + 1;
                break;
              }
              if ((res.IsGood) && (ev.MesEvent == null))
              {
                Log.Sys.Warn(SLD.Code, "Error parse event {0}. {1}:{2}", this.DisplayName, typeEvent, ev.Code);
                continue;
              }
              listEvent.Add(new HistoryEvent(this.Events, ev.MesEvent, ev.DateEvent, Quality.Good));
              indexFrom++;
            }
          }
          session.EndOperation(res);
          if (!res.IsGood) break;
        }
      }

      if (listEvent.Count>0)
      {
        Storage.WriteEvents(listEvent.ToArray());
      }
    }
    #endregion

    #region (ReadAllEvents)
    void ReadAllEvents(
      PhysicalSession<EmeraSynchState, AccountNextPoint> session,
      AccountNextPoint nextPoint,
      Transport transport,
      EmeraContentSetting cs) 
    {
      ReadEvents(session,nextPoint,transport,cs,        
        MesArchiveEvents.Access, 
        Event.GetUsedTypes(MesArchiveEvents.Access));

      ReadEvents(session, nextPoint, transport, cs,
        MesArchiveEvents.ArchErrors,
        Event.GetUsedTypes(MesArchiveEvents.ArchErrors));
      /*
      ReadEvents(session, nextPoint, transport, cs,
        MesArchiveEvents.FreqFaz,
        Event.GetUsedTypes(MesArchiveEvents.FreqFaz));
      */
      ReadEvents(session, nextPoint, transport, cs,
        MesArchiveEvents.OnOff,
        Event.GetUsedTypes(MesArchiveEvents.OnOff));

      ReadEvents(session, nextPoint, transport, cs,
        MesArchiveEvents.Programs,
        Event.GetUsedTypes(MesArchiveEvents.Programs));
      /*
      ReadEvents(session, nextPoint, transport, cs,
        MesArchiveEvents.Rele,
        Event.GetUsedTypes(MesArchiveEvents.Rele));
      */
      ReadEvents(session, nextPoint, transport, cs,
        MesArchiveEvents.SostFaz,
        Event.GetUsedTypes(MesArchiveEvents.SostFaz));
      /*
      ReadEvents(session, nextPoint, transport, cs,
        MesArchiveEvents.Tok,
        Event.GetUsedTypes(MesArchiveEvents.Tok));
      */
    }
    #endregion

    #region (override Test)
    public override TestResult Test(TestRequestDataDrv request)
    {
      TestResult result = new TestResult();

      var drvSetting = request.GetDriverSetting(() => new DriverSetting());
      var cs = request.GetContentSetting<EmeraContentSetting>(() => new EmeraContentSetting());
      Transport mes = new Transport(this.Log, this, drvSetting, ReadTimeOutRequestMSec());
      DateTimeUtc dateTimeUtc = DateTimeUtc.Now;
      int timeTwoSidePathMsec;
      if (
        (request.TestLevel == TestLevel.Ping) ||
        (request.TestLevel == TestLevel.Search))
      {
        var readRes = mes.TryReadDateTime(
           this.Channel,
           cs.Address,
           TimeZoneMap.Local,
           out dateTimeUtc,
           out timeTwoSidePathMsec);
        if (!readRes.IsGood)
          result.Add(new TestDriverError(false, SR.ErrorReadDT, readRes.ErrorMsg));
      }

      if (request.TestLevel == TestLevel.Full)
      {
        if (!OneTest(SR.Test_DT, request, result, () =>
        {
          var res = mes.TryReadDateTime(
            this.Channel,
            cs.Address,
            TimeZoneMap.Local,
            out dateTimeUtc,
            out timeTwoSidePathMsec);
          return new MsgTest(res, res.IsGood ? dateTimeUtc.ToLocal().ToString() : "");
        })) return result;

        if (!OneTest(SR.Test_SN, request, result, () =>
        {
          string sn = "";
          var res = mes.TryReadSN(
            this.Channel,
            cs.Address,
            out sn);
          return new MsgTest(res, res.IsGood ? sn : "");
        })) return result;

        Version softVetsion = null;
        if (!OneTest(SR.Test_SV, request, result, () =>
        {
          var res = mes.TryReadVersion(this.Channel, cs.Address, out softVetsion);
          return new MsgTest(res, res.IsGood ? softVetsion.ToString() : "");
        })) return result;
      }
      return result;
    }
#endregion

    #region (OneTest)
    internal bool OneTest(string nameTest, TestRequestDataDrv request, TestResult res, Func<MsgTest> func, bool error = false)
    {
      bool result = true;
      TestDriverError testDriverError = null;
      MsgTest readResult = null;
      try
      {
        readResult = func();
        if ((!readResult.OperationResult.IsGood) && (!error))
        {
          testDriverError = new TestDriverError(false, "{0}, {1}", nameTest, readResult.OperationResult.ErrorMsg);
          result = false;
        }
      }
      catch (Exception e)
      {
        testDriverError = new TestDriverError(false, "{0}, {1}", nameTest, e.GetFullMessageDisplay());
        result = false;
      }
      if (testDriverError != null) res.Add(testDriverError);

      if (Log.Trace.IsOn(1))
      {
        var mgg = String.Format("{0}: {1}", nameTest, (result) ? readResult.Message : testDriverError.Message);
        if ((readResult.OperationResult.IsGood) || (error)) Log.Trace.Info(1, mgg);
        else Log.Trace.Error(1, mgg);
      }
      return result;
    }
#endregion
  }
}
