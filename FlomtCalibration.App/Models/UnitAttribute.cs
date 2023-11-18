using System;

namespace FlomtCalibration.App.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class UnitAttribute : Attribute
    {
        public string Unit { get; }

        public UnitAttribute(string unit)
        {
            Unit = unit;
        }
    }
}
