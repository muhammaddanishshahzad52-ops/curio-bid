using Microsoft.AspNetCore.Mvc.Rendering;

namespace CurioBid.Models
{
    public static class ListingOptions
    {
        private static readonly (string Value, string Label)[] CategoryList =
        {
            ("Ancient Coins", "Ancient Coins"),
            ("Decorative", "Decorative Artifacts"),
            ("Relics & Jewelry", "Relics & Jewelry")
        };

        public static readonly string[] EvidenceTypes =
        {
            "Obverse/Reverse + Date Code Video",
            "360° Seamless Rotation Video",
            "Stamp & Macro Surface Details Video"
        };

        public static IEnumerable<string> Categories => CategoryList.Select(c => c.Value);

        public static IEnumerable<SelectListItem> CategoryItems =>
            CategoryList.Select(c => new SelectListItem(c.Label, c.Value));

        public static IEnumerable<SelectListItem> EvidenceTypeItems =>
            EvidenceTypes.Select(e => new SelectListItem(e, e));
    }
}