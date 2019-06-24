using Sys.Types.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Services.Drv.Emera
{
  public enum MesArchiveEvents : byte
  {
    //o	127 – архив включений/выключений счетчика;
    OnOff = 127,
    
    //o	128 – архив состояния фаз;
    SostFaz= 128,

    //o	129 – архив ошибок счетчика;
    ArchErrors = 129,

    //o	130 – архив программирования;
    Programs=130,

    //o	131 – архив доступа к прибору;
    Access=131,

    //o	132 – архив состояния частоты сети;
    //Отключено умышлено, не хватает типов!
    //FreqFaz=132,

    //o	133 – архив состояния реле;
    //Отключено умышлено, не хватает типов!
    //Rele = 133,

    //o	134 – архив тока нейтрали
    //Отключено умышлено, не хватает типов!
    //Tok=134
  }


  /// <summary>Описания события</summary>
  public class Event
  {

    public static Type[] GetUsedTypes(MesArchiveEvents ev)
    {
      switch (ev)
      {
        case MesArchiveEvents.Access:
          return new Type[] {
            typeof(EvOpenCoverDrv),
            typeof(EvCloseCoverDrv),
            typeof(EvAdminDrv),
          };
        case MesArchiveEvents.ArchErrors:
          return new Type[] {
            typeof(EvErrorDrv),
          };
          /*
        case MesArchiveEvents.FreqFaz:
          return new Type[] {
            typeof(EvPhaseDrv),
          };
          */
        case MesArchiveEvents.OnOff:
          return new Type[] {
            typeof(EvPowerOnDrv),
            typeof(EvPowerOffDrv),
          };
        case MesArchiveEvents.Programs:
          return new Type[] {
            typeof(EvImpactDrv),
          };
          /*
        case MesArchiveEvents.Rele: 
          return new Type[] {
            typeof(EvStateDrv),
          };
          */
        case MesArchiveEvents.SostFaz:
          return new Type[] {
            typeof(EvPhaseDrv),
          };
          /*
        case MesArchiveEvents.Tok:
          return new Type[] {
            typeof(EvLimitDrv),
          };
          */
        default:
          throw new Exception("Error type event");
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
    public DataDriverEvent MesEvent;

    public override string ToString()
    {
      return String.Format("DateTime: {0}, Event: {1}", DateEvent, MesEvent.ToString(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName));
    }
  }
}

