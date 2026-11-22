namespace Replate.Desktop;

internal static class MenuCategories
{
    internal const string MainCourse = "Makanan utama";
    internal const string Bakery = "Roti & kue";
    internal const string Produce = "Buah & sayur";
    internal const string LegacyProduce = "Vegetarian";
    internal static readonly string[] Names = [MainCourse, Bakery, Produce];

    internal static string Normalize(string category) =>
        category.Trim().Equals(LegacyProduce, StringComparison.OrdinalIgnoreCase) ? Produce : category;

    internal static IEnumerable<string> SearchTerms(string category) =>
        category == Produce ? [category, LegacyProduce] : [category];
}
