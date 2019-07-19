using System;

namespace Sys.Services.Drv.Emera.Def
{
  /// <summary>Активная и реактивная мощность прямого и обратного направления</summary>
  public class AvPower
  {
    public Energy ToEnergy(int coef)
    {
      Energy energy = new Energy(0, 0, 0, 0);
      energy.Aplus = (Pplus != 0)?Pplus / coef:0;
      energy.Aminus = (Pminus != 0) ? Pminus / coef : 0;
      energy.Rplus = (Qplus != 0) ? Qplus / coef : 0;
      energy.Rminus = (Qminus != 0) ? Qminus / coef : 0;
      return energy;
    }
    internal AvPower(double pplus, double pminus, double qplus, double qminus)
    {
      this.Pplus = pplus;
      this.Pminus = pminus;
      this.Qplus = qplus;
      this.Qminus = qminus;
    }

    public AvPower()
    {
    }

    /// <summary>Свойство Активная мощность(прямая)</summary>
    public double Pplus;

    /// <summary>Свойство Активная мощность(обратная)</summary>
    public double Pminus;

    /// <summary>Свойство Реактивая мощность(прямая)</summary>
    public double Qplus;

    /// <summary>Свойство Реактивая мощность(обратная)</summary>
    public double Qminus;
  }

  /// <summary/>
  public class AvPowerIndex
  {
    internal AvPowerIndex(AvPower power, ushort index)
    {
      this.Power = power;
      this.Index = index;
    }

    public AvPowerIndex()
    {
    }

    /// <summary>Мощность </summary>
    public AvPower Power;

    /// <summary>Индекс среза</summary>
    public ushort Index;

    /// <summary>Выщитывает реально полученное время среза</summary>
    /// <returns></returns>
    public DateTimeZone ToRealDateTime(DateTimeZone timeCut)
    {
      var result = new DateTimeZone(timeCut.Year, timeCut.Month, timeCut.Day, timeCut.TimeZoneMap);
      return result.AddMinutes(Index * 3).AddMinutes(3);
    }

    /// <summary>Проверяет реально ли получен необходимый срез с прибора</summary>
    /// <returns></returns>
    public bool Check(DateTimeZone timeCut)
    {
      var real = ToRealDateTime(timeCut);
      return real == timeCut;
    }

    /// <summary>Дата и время вычисляемая из индекса среза</summary>
    public DateTime DateTimeIndex;
  }
}
