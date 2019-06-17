using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public static class LinqExtensions
    {
        public static int LongestStreak<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return source.Aggregate(new { Longest = 0, Current = 0 },
                (agg, element) => predicate(element)
                    ? new { Longest = Math.Max(agg.Longest, agg.Current + 1), Current = agg.Current + 1 }
                    : new { agg.Longest, Current = 0 }, agg => agg.Longest);
        }
    }
}
