using System;

namespace Sys.Services.Drv.Emera.Def
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

        public void Calc(double koef)
        {
            this.Aplus *= koef;
            this.Aminus *= koef;
            this.Rplus *= koef;
            this.Rminus *= koef;
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
