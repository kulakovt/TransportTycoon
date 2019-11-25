using System;
using System.Collections.Generic;
using System.Linq;

namespace TransportTycoon
{
    internal static class Extensions
    {
        public static IReadOnlyList<T> Pop<T>(this List<T> list, int count = 1)
        {
            if (list.Any())
            {
                var items = list.Take(count).ToList();
                list.RemoveRange(0, items.Count);
                return items;
            }

            return Array.Empty<T>();
        }
    }
}