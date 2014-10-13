using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxHelperUtilities
{
    public static class Extensions
    {
        public static IEnumerable<T> FindDuplicates<T>(this IEnumerable<T> enumerable)
        {
            var hashset = new HashSet<T>();
            return enumerable.Where(cur => !hashset.Add(cur));
        }
    }
}
