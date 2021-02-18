using Sys.DataBus.Common;
using Sys.Types.Components;
using Sys.Types.HistoryWriter;
using System;
using System.Collections.Generic;

namespace Sys.Services.Drv.Emera.Def
{
    /// <summary>Значения по трем фазам</summary>
    public class Phase
    {
        public static Phase Stub(int min, int max)
        {
            Random r = new Random();
            return new Phase(r.Next(min, max), r.Next(min, max), r.Next(min, max));
        }

        internal Phase(double phase_A, double phase_B, double phase_C)
        {
            this.Phase_A = phase_A;
            this.Phase_B = phase_B;
            this.Phase_C = phase_C;
        }

        public Phase()
        {
        }

        /// <summary>Значение по фазе A</summary>
        public double Phase_A;

        /// <summary>Значение по фазе B</summary>
        public double Phase_B;

        /// <summary>Значение по фазе C</summary>
        public double Phase_C;

        internal void Calc(double koef)
        {
            this.Phase_A = Phase_A * koef;
            this.Phase_B = Phase_B * koef;
            this.Phase_C = Phase_C * koef;
        }

        internal double Sum()
        {
            return this.Phase_A + this.Phase_B + this.Phase_C;
        }
    }

    /// <summary>Мгновенная активная мощность</summary>
    public class InstantaneousActivePower
    {
        internal InstantaneousActivePower(double totalPowerPhases, Phase insPowerPhase)
        {
            this.TotalPowerPhases = totalPowerPhases;
            this.InsPowerPhase = insPowerPhase;
        }

        public InstantaneousActivePower()
        {
        }

        /// <summary>Суммарное значение мощности по трем фазам</summary>
        public double TotalPowerPhases;

        /// <summary>Активная мощность по трем фазам</summary>
        public Phase InsPowerPhase;

        internal void Calc(double koef)
        {
            InsPowerPhase.Calc(koef);
        }
    }

    /// <summary>Мгновенная реактивная мощность</summary>
    public class InstantaneousReactivePower : InstantaneousActivePower
    {
    }

    /// <summary>Мгновенные значения</summary>
    public class InstantaneousValues
    {

        public void WriteTags(StorageDataDriver storage, ElectroChannel eChannel, Quality quality, DateTimeUtc time)
        {
            var eid = new ElectroIMData(quality, time,
              this.InsActivePower.InsPowerPhase.Phase_A,
              this.InsActivePower.InsPowerPhase.Phase_B,
              this.InsActivePower.InsPowerPhase.Phase_C,
              this.InsReactivePower.InsPowerPhase.Phase_A,
              this.InsReactivePower.InsPowerPhase.Phase_B,
              this.InsReactivePower.InsPowerPhase.Phase_C,
              this.Voltage.Phase_A,
              this.Voltage.Phase_B,
              this.Voltage.Phase_C,
              this.Amperage.Phase_A,
              this.Amperage.Phase_B,
              this.Amperage.Phase_C,
              this.Frequency,
              this.PowerFactor.Phase_A,
              this.PowerFactor.Phase_B,
              this.PowerFactor.Phase_C);
            eid.WriteTags(eChannel);
        }

        internal InstantaneousValues(InstantaneousActivePower insActivePower, InstantaneousReactivePower insReactivePower, Phase voltage, Phase amperage, Phase powerFactor, double frequency)
        {
            this.InsActivePower = insActivePower;
            this.InsReactivePower = insReactivePower;
            this.Voltage = voltage;
            this.Amperage = amperage;
            this.PowerFactor = powerFactor;
            this.Frequency = frequency;
        }

        public InstantaneousValues(bool stub = false)
        {
            if (stub)
            {
                InsActivePower = new InstantaneousActivePower();
                InsActivePower.InsPowerPhase = Phase.Stub(100, 120);
                InsActivePower.TotalPowerPhases = InsActivePower.InsPowerPhase.Sum();

                InsReactivePower = new InstantaneousReactivePower();
                InsReactivePower.InsPowerPhase = Phase.Stub(25, 48);
                InsReactivePower.TotalPowerPhases = InsActivePower.InsPowerPhase.Sum();

                Voltage = Phase.Stub(190, 240);
                Amperage = Phase.Stub(10, 50);
                PowerFactor = Phase.Stub(0, 1);
                Frequency = StubUtil.Double(48, 52);
            }
        }

        /// <summary>Мгновенная активная мощность</summary>
        public InstantaneousActivePower InsActivePower;

        /// <summary>Мгновенная реактивная мощность</summary>
        public InstantaneousReactivePower InsReactivePower;

        /// <summary>Напряжение</summary>
        public Phase Voltage;

        /// <summary>Ток</summary>
        public Phase Amperage;

        /// <summary>Коэффициент мощности - косинус фи</summary>
        public Phase PowerFactor;

        /// <summary>Частота сети</summary>
        public double Frequency;

        internal void Calc(double koef)
        {
            InsActivePower.Calc(koef);
            InsReactivePower.Calc(koef);
        }
    }

}
