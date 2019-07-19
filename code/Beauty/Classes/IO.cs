using Sys.DataBus.Common;
using Sys.Services.Drv.Emera.Culture;
using Sys.Services.Drv.Emera.Def;
using Sys.Services.Drv.Emera.Transport;
using Sys.StorageModel;
using Sys.Types.Components;
using Sys.Types.HistoryWriter;
using Sys.Types.Om;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Services.Drv.Emera
{
    internal static class IO
    {
        #region (ReadDateTime)
        static internal OperationData<ReadDateTime> ReadDateTime(QueryInfo info)
        {
            if (DataBusSetting.StubData) return StubUtil.StubOnReadDateTime();
            DateTimeUtc dateTimeUtc;
            int timeTwoSidePathMsec;
            var res = info.Request.TryReadDateTime(
              info.DataBus,
              info.Cs.Address,
              info.Cs.Psw,
              info.NextPoint.Zone,
              out dateTimeUtc,
              out timeTwoSidePathMsec);
            return
              new OperationData<ReadDateTime>(
                  new ReadDateTime(false, dateTimeUtc, timeTwoSidePathMsec),
                  res);
        }
        #endregion

        #region (ReadPhysicalInfo)
        static internal OperationData<PhysicalInfo> ReadPhysicalInfo(QueryInfo info)
        {
            if (DataBusSetting.StubData) return StubUtil.StubOnReadPhysicalInfo();
            var pInfo = new PhysicalInfo();
            var res = info.Request.TryReadSerialNumber(
              info.DataBus,
              info.Cs.Address,
              info.Cs.Psw,
              out pInfo.SerialNumber);
            if (!res.IsGood) return OperationData<PhysicalInfo>.Bad(res);
            pInfo.SerialNumber = pInfo.SerialNumber.Trim();
            string deviceConfiguration;
            res = info.Request.TryReadDeviceConfiguration(
                info.DataBus, 
                info.Cs.Address,
                info.Cs.Psw,
                out deviceConfiguration);
            if (!res.IsGood) return OperationData<PhysicalInfo>.Bad(res);
            info.SetDeviceConfiguration(deviceConfiguration);
            pInfo.SoftVersion = info.DeviceConfiguration;
            return new OperationData<PhysicalInfo>(pInfo, res);
        }
        #endregion

        #region (WriteDateTime)
        static internal OperationData<bool> WriteDateTime(DeviceCompOn diff, QueryInfo info)
        {
            if (DataBusSetting.StubData) return StubUtil.StubOnWriteDateTime();
            var res = info.Request.TryWriteDateDate(
              info.DataBus,
              info.Cs.Address,
              info.Cs.Psw,
              info.NextPoint.Zone,
              diff);
            return new OperationData<bool>(res.IsGood, res);
        }
        #endregion

        #region (BeforeOpenSession)
        static internal OperationResult BeforeOpenSession(QueryInfo info)
        {
            if (DataBusSetting.StubData)
            {
                info.SetDeviceKoef(1);
                return StubUtil.StubOnBeforeOpenSession();
            }

            var res = info.Request.TryReadKoef(info.DataBus, info.Cs.Address, info.Cs.Psw, out double response, out bool isHalfHour);
            if (res.IsGood)
            {
                info.SetDeviceKoef(response);
                info.SetHalfHourInterval(isHalfHour);
                info.WriteLog2(SR.ReadKU, info.DeviceKoef);
            }
            return res;
        }
        #endregion

        #region (HW)
        private static HistoryData HW(Quality quality, double @value, DateTimeUtc timeWrite, TagDef tagDef)
        {
            return new HistoryData(
              tagDef,
              new TagData(quality, DateTimeUtc.Now, value),
              timeWrite,
              HistoryWriterKind.InsertUpdate);
        }

        private static HistoryData HW(Quality quality, DataDriverEvent @value, DateTimeUtc timeWrite, TagDef tagDef)
        {
            return new HistoryData(
              tagDef,
              new TagData(quality, DateTimeUtc.Now, TagValue.FromObject(DataType.Structured, value)),
              timeWrite,
              HistoryWriterKind.Insert);
        }
        #endregion

        #region (ReadInc)
        /// <summary>
        /// Чтение приращений (показаний)
        /// </summary>
        /// <param name="fromTime"></param>
        /// <param name="typeQuery"></param>
        /// <param name="typeInc"></param>
        /// <param name="aplus"></param>
        /// <param name="aminus"></param>
        /// <param name="rplus"></param>
        /// <param name="rminus"></param>
        /// <param name="depth"></param>
        /// <param name="deepSyncTime"></param>
        /// <param name="tariff">
        /// Добавлена поддержка чтения тарифов для показаний, т.к. эту величину нельзя расчитать.
        /// </param>
        static internal void ReadInc(
            QueryInfo info,
            DateTimeZone fromTime,
            TypeQuery typeQuery,
            TypeInc typeInc,
            TagDef aplus,
            TagDef aminus,
            TagDef rplus,
            TagDef rminus,
            int depth,
            DateTimeUtc deepSyncTime,
            ETariff tariff = ETariff.NoTariff)
        {
            //Если в гране нет данных то дальше этой даты записываем что нельзя востонавить
            bool fillBadNoRestore = false;
            int itemNum = 0;
            OperationResult oper = new OperationResult(Quality.Bad);

            var holes = info.GetHoles(typeInc, fromTime, depth, deepSyncTime, new[] { aplus });
            foreach (var item in holes)
            {
                itemNum++;
                Energy read = null;
                OperationResult res = OperationResult.Bad;
                if (fillBadNoRestore)
                {
                    read = new Energy(0, 0, 0, 0);
                    oper = new OperationResult(Quality.BadNoRestore);
                }
                else if (info.Session.BeginOperation())
                {
                    if (DataBusSetting.StubData) //STUBDATA->
                    {
                        read = new Energy(StubUtil.Int32(500), StubUtil.Int32(600), StubUtil.Int32(700), StubUtil.Int32(800));
                        oper = new OperationResult(Quality.Good);
                    }
                    else
                    {
                        switch (typeQuery)
                        {
                            #region (case TypeQuery.SlicesEnergy:)
                            case TypeQuery.SlicesEnergy:
                                if (!info.IsHalfHourInterval)
                                {
                                    oper = new OperationResult(Quality.Bad, "HalfHour Interval is incorrect!");
                                }
                                else
                                {
                                    // только для версий СЕ102 S7, R8, CE301M S31, R33
                                    // версии CE102 S6, R5 не поддерживают архив получасов
                                    oper = info.Request.TryReadSlicesEnergy(
                                        info.DataBus,
                                        info.Cs.Psw,
                                        info.Session.TimeDevice,
                                        SlicesQuery.From(item, info.Cs.Address),
                                        out read);
                                    read.Calc(info.DeviceKoef);
                                }
                                break;
                            #endregion
                            #region (case TypeQuery.Counter:)
                            case TypeQuery.Counter:
                                oper = info.Request.TryReadCounter(
                                    info.DataBus,
                                    info.Cs.Psw,
                                    typeInc,
                                    RequestIndex.From(item, typeInc, info.Cs.Address, true),
                                    tariff,
                                    out read);
                                read.Calc(info.DeviceKoef);
                                break;
                            #endregion
                            /*#region (case TypeQuery.Power3min:)
                            case TypeQuery.Power3min:
                              AvPowerIndex pi;
                              oper = info.Request.TryReadAvPower3min(
                                info.DataBus, 
                                info.Session.TimeDevice, 
                                info.Session.Zone, 
                                RequestIndex.From(item, typeInc, info.Cs.Address, false), 
                                item, 
                                out pi);
                              if (oper.IsGood)
                                read = pi.Power.ToEnergy(20);
                              break;
                            #endregion*/
                        }
                        //Может возникать ситуация когда на компе уже наступило время опроса а на счетчике нет и будет возвращен блок поврежден
            //TODO      if ((itemNum < 3) && (oper.Quality == Quality.BadNoRestore)) oper.Quality = Quality.Bad;
                    }

                    /*if ((!oper.IsGood) && (oper.Code == 2))
                    {
                        if (info.Session.OpenData.NewDevice != ChangeDeviceInfo.None)
                            info.LogWarn(SR.NOT_PARAM, info.DisplayName, aplus.AccountKind, aplus.Discrete, aplus.DiscreteValue);
                        info.Session.EndOperation(OperationResult.Good);
                        break; //Неизвестная функция не подерживаемая версией устройства
                    }*/

                    info.Session.EndOperation(oper);
                    if (!oper.IsGood)
                    {
                        read = new Energy(0, 0, 0, 0);
                        info.DataBus.DiscardInBuffer();
                    }
                    if (oper.Quality == Quality.BadNoRestore) fillBadNoRestore = true; //заполним все полохими не востанавливаемыми
                }
                else break;

                info.Storage.WriteTagsValue(
                    HW(oper.Quality, read.Aplus, item, aplus),
                    HW(Quality.Bad, read.Aminus, item, aminus),
                    HW(Quality.Bad, read.Rplus, item, rplus),
                    HW(Quality.Bad, read.Rminus, item, rminus));

                if (info.Log.Trace.IsOn(2))
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("Read from [{0}] {1} {2}", item, typeInc, oper.Quality);
                    if (typeQuery != TypeQuery.SlicesEnergy) sb.Append(" " + typeQuery);

                    if (!oper.IsGood)
                    {
                        sb.AppendFormat(". Error: {0}", oper.ErrorMsg);
                        info.Log.Trace.Error(2, sb.ToString());
                    }
                    else
                    {
                        sb.AppendFormat(". A+: {0}, A-: {1}, R+: {2}, R-: {3}", read.Aplus, read.Aminus, read.Rplus, read.Rminus);
                        info.Log.Trace.Info(2, sb.ToString());
                    }
                }
            }
        }
        #endregion

    #region (ReadEvents)
    /*internal static void ReadEvents(QueryInfo info, EmeraArchiveEvents typeEvent, Type[]  typeFilter)
    {
      if (DataBusSetting.StubData) return;

      var filter = new StructDataTypeFilter[typeFilter.Length];
      for (int i = 0; i < typeFilter.Length; i++) filter[i] = new StructDataTypeFilter(typeFilter[i]);

      var lastVal = info.Storage.ReadLastValue(filter, info.Events);

      DateTimeZone from = DateTimeZone.MinValue;
      if ((lastVal != null) && (lastVal[0].Error == DataError.Ok))
        from = lastVal[0].Value.Data.Time.ToDateTimeZone(info.NextPoint.Zone);

      var listEvent = new List<HistoryEvent>();
      var listEventSrc = new List<Event>();
      OperationResult res;
      //Значит записываем сообщения
      bool writeListEvent = false;

      info.Log.Trace.Write(2, (l) => l.Info("Read events from {0}", from.ToString()));

      Byte depth = 1;
      for (; depth <= EmeraRequest.DepthEvents; depth++)
      {
        if (info.Session.BeginOperation())
        {
          bool _break = false;
          Event ev;
          res = info.Request.TryGetAnyEvents(
            info.DataBus, 
            info.Cs.Address, 
            info.Cs.Psw,
            typeEvent, 
            depth, 
            out ev, 
            info.NextPoint.Zone);

          if ((res.IsGood) && (ev.EmeraEvent == null))
          {
            info.LogWarn("Error parse event {0}. {1}:{2}", info.DisplayName, typeEvent, ev.Code);
            break;
          }

          //Заканчиваем по коду 5, если по этому времени мы уже считали, достигли макс глубины
          //Думаю надо изменить from >= ev.DataEvent т.к. похоже есть какойто баг возможно в приборе
          if (((!res.IsGood) && (res.Code == 5)) || ((res.IsGood) && (from >= ev.DateEvent)))
          {
            if (listEvent.Count != 0) writeListEvent = true;
            _break = true;
            res = OperationResult.Good;
          }
          info.Session.EndOperation(res);

          if (!_break) info.Log.Trace.Info(2, "Read event from [{0}] {1}. {2}", depth, typeEvent, ((res.IsGood) && (ev != null)) ? ev.ToString() : "");

          if ((_break) || (!res.IsGood)) break;
          listEvent.Add(new HistoryEvent(info.Events, ev.EmeraEvent, ev.DateEvent, Quality.Good));
          listEventSrc.Add(ev);
          if ((depth == EmeraRequest.DepthEvents) && (listEvent.Count != 0)) writeListEvent = true;
        }
      }

      if (writeListEvent)
      {
        info.Storage.WriteEvents(listEvent.ToArray());
      }
    }*/
    #endregion

    #region (ReadAllEvents)
    /*internal static void ReadAllEvents(QueryInfo info)
    {
      IO.ReadEvents(info, EmeraArchiveEvents.EventAdjustments, Event.GetUsedTypes(EmeraArchiveEvents.EventAdjustments));
      IO.ReadEvents(info, EmeraArchiveEvents.EventPhase, Event.GetUsedTypes(EmeraArchiveEvents.EventPhase));
      IO.ReadEvents(info, EmeraArchiveEvents.EventStateEquipment, Event.GetUsedTypes(EmeraArchiveEvents.EventStateEquipment));
    }*/
    #endregion

  }
}
