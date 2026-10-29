using System;
using System.Linq;
using System.Collections.Generic;

namespace RSkoi_ComponentUtil.Core
{
    internal static class Paging
    {
        internal static IEnumerable<T> Filter<T>(IEnumerable<T> items, string filter, Func<T, string> getName)
            => string.IsNullOrEmpty(filter)
                ? items
                : items.Where(item => Matches(getName(item), filter));

        internal static List<T> GetPage<T>(IEnumerable<T> items, string filter, Func<T, string> getName, int page, int itemsPerPage)
            => [.. Filter(items, filter, getName)
                    .Skip(page * itemsPerPage)
                    .Take(itemsPerPage)];

        internal static bool PageExists<T>(IEnumerable<T> items, string filter, Func<T, string> getName, int page, int itemsPerPage)
            => Filter(items, filter, getName).Skip(page * itemsPerPage).Any();

        internal static T FirstMatchOrFirst<T>(IList<T> items, string filter, Func<T, string> getName)
        {
            if (items.Count == 0)
                return default;

            filter = filter.ToLower();
            foreach (var item in items)
                if (Matches(getName(item), filter))
                    return item;
            return items[0];
        }

        static bool Matches(string name, string filter)
            => string.IsNullOrEmpty(filter) || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
