using System.Collections.Generic;
using System.Linq;

namespace AssetLibrary
{
    public static class DuplicateDetector
    {
        /// <summary>同一MD5を持つグループを返す（2件以上のもの）</summary>
        public static List<List<AssetEntry>> Detect(List<AssetEntry> entries)
        {
            return entries
                .Where(e => !string.IsNullOrEmpty(e.md5))
                .GroupBy(e => e.md5)
                .Where(g => g.Count() > 1)
                .Select(g => g.ToList())
                .ToList();
        }
    }
}
