using System.Collections.Generic;
using System.Linq;

namespace AssetLibrary
{
    public static class AssetSearchFilter
    {
        public static List<AssetEntry> Apply(
            List<AssetEntry> all,
            string nameQuery,
            AssetKind? kindFilter,
            string tagFilter,
            string categoryFilter)
        {
            return all.Where(e =>
            {
                if (!string.IsNullOrEmpty(nameQuery) &&
                    !e.name.ToLower().Contains(nameQuery.ToLower())) return false;

                if (kindFilter.HasValue && e.kind != kindFilter.Value) return false;

                if (!string.IsNullOrEmpty(tagFilter) &&
                    !e.tags.Any(t => t.ToLower().Contains(tagFilter.ToLower()))) return false;

                if (!string.IsNullOrEmpty(categoryFilter) &&
                    !e.category.ToLower().Contains(categoryFilter.ToLower())) return false;

                return true;
            }).ToList();
        }
    }
}
