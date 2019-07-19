using Sys.Configuration;
using Sys.DataBus.Common;
using Sys.Diagnostics.Logger;
using Sys.Services.Components;
using Sys.Services.Drv.Emera.Culture;
using Sys.Services.Drv.Emera.Def;
using Sys.Services.Drv.Emera.Transport;
using Sys.StorageModel;
using Sys.Types.Components;
using Sys.Types.Components.DataDriverClient;
using Sys.Types.HistoryWriter;
using Sys.Types.NodeEditor;
using Sys.Types.Om;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sys.Services.Drv.Emera
{
    public class EmeraDriver : Sys.Types.Components.DriverElectroClient
    {
    #region (FineTune)
    static FineTune FineTune;
    static EmeraDriver()
    {
      FineTune = FineTune.TryLoad("emeraelectro");
    }

    public Int32 ReadTimeOutRequestMSec()
    {
      return FineTune.ReadValue<int>("TimeOutRequestMSec", this, (v) => Int32.Parse(v), EmeraRequest.TimeOutRequestMSecDeafult);
    }
        #endregion

        /// <summary>Идентификатор типа реализации компонента</summary>
        public const string TypeGuidImpl = "est.by:Bus.EmeraDrvClientImpl";

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

        #region (IsReadIm)
        public override ModeDataDrv IsReadIm(SynchRequestDataDrv request)
        {
            ModeDataDrv result = ModeDataDrv.Manual;
            var ss = request.GetSharedSetting<EmeraSharedSetting>(() => new EmeraSharedSetting());
            if ((ss.EnblIm) && !ss.Im.IsEmpty()) result |= ModeDataDrv.Auto;
            return result;
        }
        #endregion
        #region (IsSynch)
        public override ModeDataDrv IsSynch(SynchRequestDataDrv request)
        {
            return ModeDataDrv.All;
        }
        #endregion
        #region (IsWriteIm)
        public override ModeDataDrv IsWriteIm(SynchRequestDataDrv request)
        {
            return ModeDataDrv.None;
        }
        #endregion
        #region (WriteIm)
        public override SynchResponse WriteIm(SynchRequestDataDrv request, WriteImParamsDataDrv param)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region (ReadIm)
        public override SynchResponse ReadIm(SynchRequestDataDrv request, ReadImParamsDataDrv param)
        {
            var ss = request.GetSharedSetting<EmeraSharedSetting>(() => new EmeraSharedSetting());
            var cs = request.GetContentSetting<EmeraContentSetting>(() => new EmeraContentSetting());
            var drvSetting = request.GetDriverSetting(() => new DriverSetting());
            EmeraRequest emera = new EmeraRequest(this, drvSetting, ReadTimeOutRequestMSec());
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
                    else
                    {
                        double koef;
                        resData = emera.TryReadKoef(this.Channel, cs.Address, cs.Psw, out koef, out bool isHalfHour);
                        if (resData.IsGood)
                        {
                            resData = emera.TryReadInstantaneousValues(this.Channel, cs.Address, cs.Psw, out iv);
                            if (resData.IsGood)
                            {
                                iv.Calc(koef);
                                ElectroChannel electroChannel = this.ElectroChannel.ByIndex<ElectroChannel>(0);
                                iv.WriteTags(this.Storage, electroChannel, resData.Quality, DateTimeUtc.Now);
                            }
                        }
                    }
                    session.EndOperation(resData);
                    Log.Trace.Write(1, (l) => l.Info(SR.READ_IM, resData.ToString()));
                }
                return session;
            }
        }
        #endregion

        #region (Synch)
        public override SynchResponse Synch(SynchRequestDataDrv requestData, SynchParamsDataDrv requestParam)
        {
            Log.Trace.Write(1, (l) => l.Info(SR.SYNC_ARCH));
            var info = new QueryInfo(
                this,
                requestData,
                requestParam,
                requestData.GetSharedSetting(() => new EmeraSharedSetting()),
                requestData.GetContentSetting(() => new EmeraContentSetting()),
                requestData.GetDriverSetting(() => new DriverSetting())
            );

            //Любая работа с устройством должна начинаться с открытия сессии
            using (var session = new PhysicalSession<EmeraSynchState, AccountNextPoint>(this, requestData, info.NextPoint))
            {
                info.SetSession(session);
                //чтение времени
                session.OnReadDateTime = () => { return IO.ReadDateTime(info); };
                //чтение серийного номера и т.д.
                session.OnReadPhysicalInfo = () => { return IO.ReadPhysicalInfo(info); };
                if (info.Ss.EnblTimeCorr)
                {
                    //записть времени
                    session.OnWriteDateTime = (diff) => { return IO.WriteDateTime(diff, info); };
                }
                //произвольные операции перед открытии сессии
                session.OnBeforeOpenSession = () => { return IO.BeforeOpenSession(info); };
                session.AutoOpen = true;


                var eDef = info.ElectroChannel.Energy;
                var eDefCounter = info.ElectroChannel.Counter;                
                /*if (session.LaunchPoint(info.NextPoint.Min3))
                {
                  Log.Trace.Info(1, SR.Read3Min);
                  IO.ReadInc(
                    info,
                    TimeStep.Minute_3.Round(info.NowTimeInZone), 
                    TypeQuery.Power3min, 
                    TypeInc.Min3, 
                    eDef.Aplus.Min3, 
                    eDef.Aminus.Min3,
                    eDef.Rplus.Min3, 
                    eDef.Rminus.Min3, 
                    EmeraRequest.Depth3Min, 
                    requestParam.DeepSync);
                  if (info.Ss.EnblEvents) IO.ReadAllEvents(info);
                }*/

                #region if (session.LaunchPoint(info.NextPoint.Min30))
                if (session.LaunchPoint(info.NextPoint.Min30))
                {
                  Log.Trace.Info(1, SR.Read30Min);
                  IO.ReadInc(
                    info,
                    TimeStep.Minute_30.Round(info.NowTimeInZone), 
                    TypeQuery.SlicesEnergy, 
                    TypeInc.Min30, 
                    eDef.Aplus.Min30, 
                    eDef.Aminus.Min30,
                    eDef.Rplus.Min30, 
                    eDef.Rminus.Min30, 
                    EmeraRequest.Depth30MinDefault,
                    FineTuneUtils.ReadDeepSync30Local(FineTune, this, requestParam.DeepSync, EmeraRequest.Depth30MinDefault));
                  //if ((!info.Ss.Enbl3min) &&  (info.Ss.EnblEvents)) IO.ReadAllEvents(info);
                }
                #endregion
                #region if (session.LaunchPoint(info.NextPoint.Day1))
                if (session.LaunchPoint(info.NextPoint.Day1))
                {
                    Log.Trace.Info(1, SR.ReadDay);
                    /*IO.ReadInc(
                    info,
                    TimeStep.Day_1.Round(info.NowTimeInZone), 
                    TypeQuery.Inc, 
                    TypeInc.Day, 
                    eDef.Aplus.Day1, 
                    eDef.Aminus.Day1,
                    eDef.Rplus.Day1, 
                    eDef.Rminus.Day1, 
                    EmeraRequest.DepthDay, 
                    requestParam.DeepSync);*/

                    IO.ReadInc(
                        info,
                        TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/, 
                        TypeQuery.Counter, 
                        TypeInc.Day, 
                        eDefCounter.Aplus.Day1, 
                        eDefCounter.Aminus.Day1,
                        eDefCounter.Rplus.Day1, 
                        eDefCounter.Rminus.Day1, 
                        EmeraRequest.DepthDayCounter, 
                        requestParam.DeepSync,
                        ETariff.NoTariff);

                    #region if (info.Ss.EnblCounterTariff1)
                    if (info.Ss.EnblCounterTariff1) 
                    {
                        IO.ReadInc(
                            info, 
                            TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/, 
                            TypeQuery.Counter, 
                            TypeInc.Day, 
                            eDefCounter.Aplus.Day1Tariff1, 
                            eDefCounter.Aminus.Day1Tariff1, 
                            eDefCounter.Rplus.Day1Tariff1, 
                            eDefCounter.Rminus.Day1Tariff1, 
                            EmeraRequest.DepthDayCounter, 
                            requestParam.DeepSync, 
                            ETariff.Tariff1);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff2)
                    if (info.Ss.EnblCounterTariff2) 
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Day,
                          eDefCounter.Aplus.Day1Tariff2,
                          eDefCounter.Aminus.Day1Tariff2,
                          eDefCounter.Rplus.Day1Tariff2,
                          eDefCounter.Rminus.Day1Tariff2,
                          EmeraRequest.DepthDayCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff2);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff3)
                    if (info.Ss.EnblCounterTariff3)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Day,
                          eDefCounter.Aplus.Day1Tariff3,
                          eDefCounter.Aminus.Day1Tariff3,
                          eDefCounter.Rplus.Day1Tariff3,
                          eDefCounter.Rminus.Day1Tariff3,
                          EmeraRequest.DepthDayCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff3);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff4)
                    if (info.Ss.EnblCounterTariff4)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Day,
                          eDefCounter.Aplus.Day1Tariff4,
                          eDefCounter.Aminus.Day1Tariff4,
                          eDefCounter.Rplus.Day1Tariff4,
                          eDefCounter.Rminus.Day1Tariff4,
                          EmeraRequest.DepthDayCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff4);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff5)
                    if (info.Ss.EnblCounterTariff5)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Day,
                          eDefCounter.Aplus.Day1Tariff5,
                          eDefCounter.Aminus.Day1Tariff5,
                          eDefCounter.Rplus.Day1Tariff5,
                          eDefCounter.Rminus.Day1Tariff5,
                          EmeraRequest.DepthDayCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff5);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff6)
                    if (info.Ss.EnblCounterTariff6)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Day,
                          eDefCounter.Aplus.Day1Tariff6,
                          eDefCounter.Aminus.Day1Tariff6,
                          eDefCounter.Rplus.Day1Tariff6,
                          eDefCounter.Rminus.Day1Tariff6,
                          EmeraRequest.DepthDayCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff6);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff7)
                    if (info.Ss.EnblCounterTariff7)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Day,
                          eDefCounter.Aplus.Day1Tariff7,
                          eDefCounter.Aminus.Day1Tariff7,
                          eDefCounter.Rplus.Day1Tariff7,
                          eDefCounter.Rminus.Day1Tariff7,
                          EmeraRequest.DepthDayCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff7);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff8)
                    if (info.Ss.EnblCounterTariff8)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Day_1.Round(info.NowTimeInZone)/*.AddDays(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Day,
                          eDefCounter.Aplus.Day1Tariff8,
                          eDefCounter.Aminus.Day1Tariff8,
                          eDefCounter.Rplus.Day1Tariff8,
                          eDefCounter.Rminus.Day1Tariff8,
                          EmeraRequest.DepthDayCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff8);
                    }
                    #endregion
                }
                #endregion
                #region if (session.LaunchPoint(info.NextPoint.Month1))
                if (session.LaunchPoint(info.NextPoint.Month1))
                {
                    Log.Trace.Info(1, SR.ReadMonth);
                    /*IO.ReadInc(
                      info,
                      TimeStep.Month.Round(info.NowTimeInZone), 
                      TypeQuery.Inc,
                      TypeInc.Month, 
                      eDef.Aplus.Month1, 
                      eDef.Aminus.Month1,
                      eDef.Rplus.Month1, 
                      eDef.Rminus.Month1, 
                      EmeraRequest.DepthMonth, 
                      requestParam.DeepSync);*/

                    IO.ReadInc(
                      info,
                      TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                      TypeQuery.Counter,
                      TypeInc.Month,
                      eDefCounter.Aplus.Month1,
                      eDefCounter.Aminus.Month1,
                      eDefCounter.Rplus.Month1,
                      eDefCounter.Rminus.Month1,
                      EmeraRequest.DepthMonthCounter,
                      requestParam.DeepSync,
                      ETariff.NoTariff);

                    #region if (info.Ss.EnblCounterTariff1)
                    if (info.Ss.EnblCounterTariff1)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff1,
                          eDefCounter.Aminus.Month1Tariff1,
                          eDefCounter.Rplus.Month1Tariff1,
                          eDefCounter.Rminus.Month1Tariff1,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff1);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff2)
                    if (info.Ss.EnblCounterTariff2)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff2,
                          eDefCounter.Aminus.Month1Tariff2,
                          eDefCounter.Rplus.Month1Tariff2,
                          eDefCounter.Rminus.Month1Tariff2,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff2);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff3)
                    if (info.Ss.EnblCounterTariff3)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff3,
                          eDefCounter.Aminus.Month1Tariff3,
                          eDefCounter.Rplus.Month1Tariff3,
                          eDefCounter.Rminus.Month1Tariff3,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff3);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff4)
                    if (info.Ss.EnblCounterTariff4)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff4,
                          eDefCounter.Aminus.Month1Tariff4,
                          eDefCounter.Rplus.Month1Tariff4,
                          eDefCounter.Rminus.Month1Tariff4,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff4);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff5)
                    if (info.Ss.EnblCounterTariff5)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff5,
                          eDefCounter.Aminus.Month1Tariff5,
                          eDefCounter.Rplus.Month1Tariff5,
                          eDefCounter.Rminus.Month1Tariff5,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff5);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff6)
                    if (info.Ss.EnblCounterTariff6)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff6,
                          eDefCounter.Aminus.Month1Tariff6,
                          eDefCounter.Rplus.Month1Tariff6,
                          eDefCounter.Rminus.Month1Tariff6,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff6);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff7)
                    if (info.Ss.EnblCounterTariff7)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff7,
                          eDefCounter.Aminus.Month1Tariff7,
                          eDefCounter.Rplus.Month1Tariff7,
                          eDefCounter.Rminus.Month1Tariff7,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff7);
                    }
                    #endregion
                    #region if (info.Ss.EnblCounterTariff8)
                    if (info.Ss.EnblCounterTariff8)
                    {
                        IO.ReadInc(
                          info,
                          TimeStep.Month.Round(info.NowTimeInZone)/*.AddMonths(-1)*/,
                          TypeQuery.Counter,
                          TypeInc.Month,
                          eDefCounter.Aplus.Month1Tariff8,
                          eDefCounter.Aminus.Month1Tariff8,
                          eDefCounter.Rplus.Month1Tariff8,
                          eDefCounter.Rminus.Month1Tariff8,
                          EmeraRequest.DepthMonthCounter,
                          requestParam.DeepSync,
                          ETariff.Tariff8);
                    }
                    #endregion
                }
                #endregion

                Log.Trace.Write(1, (l) => l.Info("Next session from {0} min", Math.Round((session.Result.Next - DateTimeUtc.Now).TotalMinutes), 1));
                return session;
            }
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
        #region (Test)
        public override TestResult Test(TestRequestDataDrv request)
        {
            TestResult result = new TestResult();

            var drvSetting = request.GetDriverSetting(() => new DriverSetting());
            var cs = request.GetContentSetting<EmeraContentSetting>(() => new EmeraContentSetting());
            EmeraRequest emera = new EmeraRequest(this, drvSetting, ReadTimeOutRequestMSec());
            //var sr = new Def.ShortRequest(cs.Address);

            if ((request.TestLevel == TestLevel.Ping) || (request.TestLevel == TestLevel.Search))
            {
                DateTimeUtc dateTimeUtc;
                int timeTwoSidePathMsec;
                if (!DataBusSetting.StubData)
                {
                    var readRes = emera.TryReadDateTime(this.Channel, cs.Address, cs.Psw, TimeZoneMap.Local, out dateTimeUtc, out timeTwoSidePathMsec);
                    if (!readRes.IsGood) result.Add(new TestDriverError(false, "Error Connect. {0}", readRes.ErrorMsg));
                }
            }
            else if (request.TestLevel == TestLevel.Full)
            {
                DateTimeUtc dateTimeUtcPrb = DateTimeUtc.MinValue;
                int timeTwoSidePathMsec;
                if (!OneTest(SR.Test_DT, request, result, () =>
                    {
                        var res = emera.TryReadDateTime(this.Channel, cs.Address, cs.Psw, TimeZoneMap.Local, out dateTimeUtcPrb, out timeTwoSidePathMsec);
                        return new MsgTest(res, res.IsGood ? dateTimeUtcPrb.ToLocal().ToString() : string.Empty);
                    })) return result;
                TimeSpan diffTime = DateTimeUtc.Now - dateTimeUtcPrb;

                string sn = string.Empty;
                if (!OneTest(SR.Test_SN, request, result, () =>
                    {
                        var res = emera.TryReadSerialNumber(this.Channel, cs.Address, cs.Psw, out sn);
                        return new MsgTest(res, res.IsGood ? sn : string.Empty);
                    })) return result;

                string deviceConfiguration = string.Empty;
                if (!OneTest(SR.Test_SV, request, result, () =>
                    {
                        var res = emera.TryReadDeviceConfiguration(this.Channel, cs.Address, cs.Psw, out deviceConfiguration, true);
                        return new MsgTest(res, res.IsGood ? deviceConfiguration.ToString() : string.Empty);
                    })) return result;

                /*if (!OneTest(SR.Test_Error_Req, request, result, () =>
                {
                  Energy eRead;
                  var res = emera.TryReadSlicesEnergy(this.Channel, DeviceCompOn.Default, SlicesQuery.GetEmulError(sr.Address), TypeInfo.Imp, out eRead);
                  return new MsgTest(res, res.IsGood ? "" : "Ok");
                }, true)) return result;*/

                result.Message = String.Format("Сер.Номер: {0}, Временной разрыв: {1} sec, Конфигурация: {2}", sn, (int)diffTime.TotalSeconds, deviceConfiguration);
        }
        return result;
    }
        #endregion
    }
}
