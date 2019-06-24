using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Services.Drv.Emera
{
  /// <summary>Активная и реактивная энергия прямого и обратного направления</summary>
  public class Energy
  {
    public static readonly Energy Default = new Energy(0, 0, 0, 0);

    internal Energy(double aplus, double aminus, double rplus, double rminus)
    {
      this.Aplus = aplus;
      this.Aminus = aminus;
      this.Rplus = rplus;
      this.Rminus = rminus;
    }

    public void Calc(EmeraContentSetting cs, bool ispower)
    {
      double ke = 1;
      this.Aplus = ((Aplus * cs.KI * cs.KU) * ke) ;
      this.Aminus = ((Aminus * cs.KI * cs.KU) * ke) ;
      this.Rplus = ((Rplus * cs.KI * cs.KU) * ke) ;
      this.Rminus = ((Rminus * cs.KI * cs.KU) * ke) ;
      if (ispower) {
        this.Aplus = this.Aplus / 2;
        this.Rplus = this.Rplus / 2;
        this.Aminus = this.Aminus / 2;
        this.Rminus = this.Rminus / 2;
      }
    }

    /// <summary>Активная энергия(прямая)</summary>
    public double Aplus;

    /// <summary>Активная энергия(обратная)</summary>
    public double Aminus;

    /// <summary>Реактивая энергия(прямая)</summary>
    public double Rplus;

    /// <summary>Реактивая энергия(обратная)</summary>
    public double Rminus;
  }
}
