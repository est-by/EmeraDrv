using System;
using Sys.Services.Drv.Emera.Culture;
using Sys.Types.Components;

namespace Sys.Services.Drv.Emera.Def
{
  public enum EmeraArchiveEvents : byte
  {
    EventPhase = 14,
    EventStateEquipment = 15,
    EventAdjustments = 16
  }


  /// <summary>Описания события</summary>
  public class Event
  {
    private bool SetPhase(ref string line, string phaseLetter, int code, int bit)
    {
      bool result = false;
      if (((code >> bit) & 1) == 0)
      {
        result = true;
        line += phaseLetter;
      }
      else line += "-";
      return result;
    }

    public static Type[] GetUsedTypes(EmeraArchiveEvents ev)
    {
      switch (ev)
      {
        case EmeraArchiveEvents.EventPhase:
          return new Type[] { typeof(EvPhaseDrv) };
        case EmeraArchiveEvents.EventStateEquipment:
          return new Type[] { typeof(EvErrorDrv), };
        case EmeraArchiveEvents.EventAdjustments:
          return new Type[] { typeof(EvOpenCoverDrv), typeof(EvCloseCoverDrv), typeof(EvAdjustTime), typeof(EvAdminDrv) };
        default:
          throw new Exception("Error type event");
      }
    }

    internal Event(Sys.DateTimeZone dateEvent, int code, EmeraArchiveEvents evt)
    {
      this.DateEvent = dateEvent;
      this.Code = code;
      this.EmeraEvent = null;

      switch (evt)
      {
        case EmeraArchiveEvents.EventPhase:
          var ev = new EvPhaseDrv();
          EventSource = "U";
          ev.UA_On = SetPhase(ref EventSource, "A", code, 8);
          ev.UB_On = SetPhase(ref EventSource, "B", code, 9);
          ev.UC_On = SetPhase(ref EventSource, "C", code, 10);

          EventSource += ", I";
          ev.IA_On = SetPhase(ref EventSource, "A", code, 12);
          ev.IB_On = SetPhase(ref EventSource, "B", code, 13);
          ev.IC_On = SetPhase(ref EventSource, "C", code, 14);

          EventSource = SR.CHAIN_TERM + " " + EventSource;
          this.EmeraEvent = ev;
          break;

        case EmeraArchiveEvents.EventStateEquipment:
          var evDev = new EvErrorDrv();
          int icode = (code >> 8);
          if ((icode & 1) == 1)
          {
            evDev.Param1 = 0;
            EventSource = SR.MACHINE_ERROR;

            if ((icode >> 8 & 1) == 1)
            {
              evDev.Param1 = 8;
              EventSource += SR.ERROR_EXCHANGE_DSP;
            } else if ((icode >> 9 & 1) == 1)
            {
              evDev.Param1 = 9;
              EventSource += SR.ERROR_DSP;
            } else  if ((icode >> 11 & 1) == 1)
            {
              evDev.Param1 = 11;
              EventSource += SR.DEFECTIVE_EEPROM_1;
            } else if ((icode >> 12 & 1) == 1)
            {
              evDev.Param1 = 12;
              EventSource += SR.DEFECTIVE_EEPROM_2;
            } else if ((icode >> 14 & 1) == 1)
            {
              evDev.Param1 = 14;
              EventSource += SR.DEFECTIVE_PZU;
            } else if ((icode >> 15 & 1) == 1)
            {
              evDev.Param1 = 15;
              EventSource += SR.DEFECTIVE_OZU;
            }
            this.EmeraEvent = evDev;
          }
          if ((icode >> 1 & 1) == 1)
          {
            evDev.Param1 = 1;
            EventSource = SR.TIME_CLOCK_FAILTURE;
          } else if ((icode >> 2 & 1) == 1)
          {
            evDev.Param1 = 2;
            EventSource = SR.DAMAGE_CALIBRATION_FILE;
          } else if ((icode >>3& 1) == 1) 
          {
            evDev.Param1 = 3;
            EventSource = SR.DAMAGE_CALIBRATION_FILE;
          } else if ((icode >>4& 1) == 1) 
          {
            evDev.Param1 = 4;
            EventSource = SR.NETWORK_PROBLEMS;
          }
          EmeraEvent = evDev;
          break;

        case EmeraArchiveEvents.EventAdjustments:
          int ecode = (code >> 8);
          if ((ecode & 1) == 1)
          {
            EmeraEvent = new EvOpenCoverDrv();
            EventSource = SR.OPENING_COUNTER;
          }
          else if ((ecode >> 1 & 1) == 1)
          {
            EmeraEvent = new EvCloseCoverDrv();
            EventSource = SR.CLOSING_COUNTER;
          }
          else if ((ecode >> 2 & 1) == 1)
          {
            EmeraEvent = new EvAdjustTime();
            EventSource = SR.ADJUSTMENT_TIME_BUTTONS;
          }
          else if ((ecode >> 3 & 1) == 1)
          {
            EmeraEvent = new EvAdjustTime();
            EventSource = SR.ADJUSTMENT_TIME_CHANNELS;
          }
          else if ((ecode >> 4 & 1) == 1)
          {
            EmeraEvent = new EvAdminDrv(EvAdminDrvEn.Tariff);
            EventSource = SR.TARIFF_SCHEDULE_CHANGE;
            EventSource += (code & 1) == 1 ? SR.WORKING_DAYS : SR.WEEKEND_DAYS;
          }
          else if ((ecode >> 5 & 1) == 1)
          {
            EmeraEvent = new EvAdminDrv(EvAdminDrvEn.Holday);
            EventSource = SR.RESCHEDULE_HOLIDAYS;
          }
          else if ((ecode >> 6 & 1) == 1)
          {
            var evChangingSeasons = new EvAdminDrv(EvAdminDrvEn.Tariff);
            EventSource = SR.CHANGING_DATE_CHANGE_SEASONS;
            if ((code & 1) == 1)
            {
              evChangingSeasons.Param1 = 0;
              EventSource += SR.SEASON_SUMMER;
            }
            else if ((code >> 1 & 1) == 1)
            {
              evChangingSeasons.Param1 = 1;
              EventSource += SR.SEASON_WINTER;
            }
            else if ((code >> 2 & 1) == 1)
            {
              evChangingSeasons.Param1 = 2;
              EventSource += SR.AUTO_CALC_SUMMER_SEASON;
            }
            else if ((code >> 3 & 1) == 1)
            {
              evChangingSeasons.Param1 = 3;
              EventSource += SR.AUTO_CALC_WINTER_SEASON;
            }
            EmeraEvent = evChangingSeasons;
          }
          else if ((ecode >> 7 & 1) == 1)
          {
            var evEvChangingConst = new EvAdminDrv(EvAdminDrvEn.Const);

            EventSource = SR.CHANGING_CONSTANTS;
            if ((code & 1) == 1)
            {
              EventSource += SR.CHG_TYPE_EQUIPMENT;
              evEvChangingConst.Param1 = 0;
            }
            else if ((code >> 1 & 1) == 1)
            {
              EventSource += SR.CHG_ID;
              evEvChangingConst.Param1 = 1;
            }
            else if ((code >> 2 & 1) == 1)
            {
              EventSource += SR.CHG_NET_ID;
              evEvChangingConst.Param1 = 2;
            }
            else if ((code >> 3 & 1) == 1)
            {
              EventSource += SR.CHG_ID_USER;
              evEvChangingConst.Param1 = 3;
            }
            else if ((code >> 4 & 1) == 1)
            {
              EventSource += SR.CHG_PORT_SETT;
              evEvChangingConst.Param1 = 4;
            }
            else if ((code >> 5 & 1) == 1)
            {
              EventSource += SR.CHG_KI_KU_FORMAT;
              evEvChangingConst.Param1 = 5;
            }
            else if ((code >> 6 & 1) == 1)
            {
              EventSource += SR.CHG_MASK_VIEW;
              evEvChangingConst.Param1 = 6;
            }
            EmeraEvent = evEvChangingConst;
          }
          else if ((ecode >> 8 & 1) == 1)
          {
            EmeraEvent =  new EvAdminDrv(EvAdminDrvEn.Telem);
            EventSource = SR.CHANGE_SET_TELEMETRY;
          }
          else if ((ecode >> 9 & 1) == 1)
          {
            var evEvModeChange = new EvAdminDrv(EvAdminDrvEn.Mode);
            EventSource = SR.MODE_CHANGE;
            if ((code & 1) == 1)
            {
              evEvModeChange.Param1 = 0;
              EventSource += SR.M_NORM;
            }
            else if ((code >> 1 & 1) == 1)
            {
              evEvModeChange.Param1 = 1;
              EventSource += SR.M_CALIBRATE;
            }
            else if ((code >> 2 & 1) == 1)
            {
              evEvModeChange.Param1 = 2;
              EventSource += SR.M_VERIFICATION;
            }
            EmeraEvent = evEvModeChange;
          }
          else if ((ecode >> 10 & 1) == 1)
          {
            EmeraEvent = new EvAdminDrv(EvAdminDrvEn.Pass);
            EventSource = SR.CHANGE_PASSWORD;
          }
          else if ((ecode >> 11 & 1) == 1)
          {
            var evEvZeroingEnergy = new EvAdminDrv(EvAdminDrvEn.ResPow);
            EventSource = SR.POWER_RESET;

            if ((code & 1) == 1)
            {
              evEvZeroingEnergy.Param1 = 0;
              EventSource += SR.PR_ACCUM;
            }
            else if ((code >> 1 & 1) == 1)
            {
              evEvZeroingEnergy.Param1 = 1;
              EventSource += SR.PR_DAY;
            }
            else if ((code >> 2 & 1) == 1)
            {
              evEvZeroingEnergy.Param1 = 2;
              EventSource += SR.PR_MONTH;
            }
            else if ((code >> 3 & 1) == 1)
            {
              evEvZeroingEnergy.Param1 = 3;
              EventSource += SR.PR_YEAR;
            }
            EmeraEvent = evEvZeroingEnergy;
          }
          else if ((ecode >> 12 & 1) == 1)
          {
            EmeraEvent = new EvAdminDrv(EvAdminDrvEn.ResPow);
            EventSource = SR.MAX_POWER_RESET;
          }
          else if ((ecode >> 13 & 1) == 1)
          {
            EmeraEvent = new EvAdminDrv(EvAdminDrvEn.ResCut);
            EventSource = SR.SEGMENTS_RESET;
          }
          else if ((ecode >> 14 & 1) == 1)
          {
            EmeraEvent = new EvAdminDrv(EvAdminDrvEn.Admin);
            EventSource = SR.ADMIN_RESET;
          }
          else if ((ecode >> 15 & 1) == 1)
          {
            EmeraEvent = new EvAdminDrv(EvAdminDrvEn.ScanPsw);
            EventSource = SR.SCAN_PASSWORD;
          }
          break;
        default:
          break;
      }

    }

    public Event() { }

    /// <summary>Дата и время события</summary>
    public Sys.DateTimeZone DateEvent;

    /// <summary>Код события</summary>
    public int Code;

    /// <summary>Текстовое описания события</summary>
    public string EventSource;
    /// <summary>Событие в виде TagValue</summary>
    public DataDriverEvent EmeraEvent;

    public override string ToString()
    {
      return String.Format("DateTime: {0}, Event: {1}", DateEvent, EmeraEvent.ToString(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName));
    }
  }
}
