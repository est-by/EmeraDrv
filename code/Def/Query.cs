using System;

namespace Sys.Services.Drv.Emera.Def
{
  /// <summary>Доп информация для опроса Гран Эл.</summary>
  public class Request
  {

    internal Request(byte address, byte shift, byte rate, byte refine)
    {
      this.Address = address;
      this.Shift = shift;
      this.Rate = rate;
      this.Refine = refine;
    }

    public Request()
    {
    }

    /// <summary>Сетевой адрес прибора</summary>
    public int Address;

    /// <summary>Смещение</summary>
    public byte Shift;

    /// <summary>Тариф</summary>
    public byte Rate;

    /// <summary>Уточнение</summary>
    public byte Refine;
  }

  /// <summary/>
  public class ShortRequest
  {
    internal ShortRequest(int address)
    {
      this.Address = address;
    }

    public ShortRequest()
    {
    }

    /// <summary>Сетевой адрес прибора</summary>
    public int Address;
  }

  /// <summary>Доп информация для опроса архивных значений энергии</summary>
  public class SlicesQuery : ShortRequest
  {
    public static SlicesQuery From(DateTimeZone timeSlices, int address)
    {
      SlicesQuery result = new SlicesQuery();
      timeSlices = timeSlices.AddMinutes(-30); //Это надо для правельной адрессации грана
      result.Month = (byte)timeSlices.Month;
      result.Day = (byte)timeSlices.Day;
      result.Index = (byte)(timeSlices.Hour * 2 + (int)((timeSlices.Minute == 0) ? 0 : 1));
      result.Address = address;
      result.TimeSlices = timeSlices;
      return result;
    }

    private SlicesQuery()
    {
      Month = 1;
      Day = 1;
      EmulError = false;
    }

    public static SlicesQuery GetEmulError(int address)
    {
      SlicesQuery result = new SlicesQuery();
      result.Month = 13;
      result.Day = 1;
      result.Index = 0;
      result.Address = address;
      result.EmulError = true;
      return result;
    }

    public bool EmulError { get; private set; }
    
    public DateTimeZone TimeSlices { get; private set; }

    /// <summary>
    /// Корректировка. если в гране TimeSlices на 9:30 то реально в емаксе значение положиться на 10:00 те со сдвигом в 30
    /// минут. Поэтому и введина уточнение
    /// </summary>
    /// <returns></returns>
    public DateTimeZone GetTimeSlicesFrom30()
    {
      return TimeSlices.AddMinutes(30);
    }

    /// <summary>Месяц(1..12)</summary>
    public byte Month  {get; private set;}

    /// <summary>День(1..31)</summary>
    public byte Day  {get; private set;}

    /// <summary>Номер среза(0..47)</summary>
    public byte Index  {get; private set;}
  }

  /// <summary/>
  public class RequestIndex :  ShortRequest
  {
    internal RequestIndex(int address, byte index) : base(address)
    {
      this.Address = address;
      this.Index = index;
    }

    public RequestIndex()
    {
    }

    /// <summary>Индекс запроса</summary>
    public byte Index;

    public static RequestIndex From(DateTimeZone roundTimeIndex, TypeInc typeInc, int address, bool inc)
    {
      byte index = 0;
      var now = roundTimeIndex.Now;
      if (typeInc == TypeInc.Year)
      {
        var curMonth = now.Date.AddDays(-now.Day + 1).AddMonths(-now.Month + 1);
        index = (byte)(curMonth.Year - roundTimeIndex.Year);
        if (inc) index += 1;
      }
      else if (typeInc == TypeInc.Month)
      {
        var curMonth = now.Date.AddDays(-now.Day + 1);
        index = (byte)((curMonth.Year * 12 + curMonth.Month) - (roundTimeIndex.Year * 12 + roundTimeIndex.Month));
        if (inc) index += 1;
      }
      else if (typeInc == TypeInc.Day)
      {
        var curMonth = now.Date;
        TimeSpan span = curMonth - roundTimeIndex;
        index = (byte)(span.Days);
        if (inc) index += 1;
      }
      else if (typeInc == TypeInc.Min3)
      {
        TimeSpan span = now - roundTimeIndex;
        index = (byte)((span.Minutes)/3);
        index += 1;

      }

      else throw new ArgumentException("t");
      return new RequestIndex(address, index);
    }
  }
}
