using System;

namespace FlomtCalibration.App.Models
{
    public class CalculationResult
    {
        public DateTime DateTime { get; init; }
        [Unit("s")]
        public double Tc { get; init; }
        [Unit("Hz")]
        public double fc { get; init; }
        [Unit("Hz")]
        public double fmin { get; init; }
        [Unit("Hz")]
        public double fmax { get; init; }
        [Unit("bar")]
        public double pc { get; init; }
        [Unit("oC")]
        public double tc { get; init; }
        [Unit("bar")]
        public double p2 { get; init; }
        [Unit("oC")]
        public double t2 { get; init; }
        [Unit("m3")]
        public double Ve { get; init; }
        [Unit("m3")]
        public double Vc { get; init; }
        [Unit("m3/h")]
        public double Qc { get; init; }
        [Unit("m/s")]
        public double Uc { get; init; }
    }
}
