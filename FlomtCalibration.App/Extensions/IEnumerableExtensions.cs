using System;
using System.Collections.Generic;
using System.Linq;

namespace FlomtCalibration.App.Extensions
{
    public static class IEnumerableExtensions
    {
        public static (T less, T bigger) Between<T>(this IEnumerable<T> source, T value) where T : IComparable<T>
        {
            var prev = source.First();
            foreach (var item in source.Skip(1))
            {
                if (prev.CompareTo(value) < 0 && item.CompareTo(value) > 0)
                {
                    return (prev, item);
                }
                prev = item;
            }
            return default;
        }
    }
}
