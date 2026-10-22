using System;
using System.Linq;
using System.Collections.Generic;

namespace RSkoi_ComponentUtil.Core
{
    internal static class Paging
    {
        internal static List<T> Filter<T>(IEnumerable<T> items, string filter, Func<T, string> getName)
        {
            if (string.IsNullOrEmpty(filter))
                return [.. items];

            filter = filter.ToLower();
            return [.. items.Where(item => getName(item).ToLower().Contains(filter))];
        }

        internal static List<T> GetPage<T>(IEnumerable<T> items, string filter, Func<T, string> getName, int page, int itemsPerPage)
            => [.. Filter(items, filter, getName)
                    .Skip(page * itemsPerPage)
                    .Take(itemsPerPage)];

        internal static bool PageExists<T>(IEnumerable<T> items, string filter, Func<T, string> getName, int page, int itemsPerPage)
            => page * itemsPerPage < Filter(items, filter, getName).Count;
    }
}
