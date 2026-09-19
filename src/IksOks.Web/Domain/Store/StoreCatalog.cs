namespace IksOks.Web.Domain.Store;

public static class StoreCatalog
{
    public const string EmojiType = "Emoji";
    public const string BorderType = "Border";

    public static IReadOnlyList<StoreItemDefinition> Items { get; } =
        new List<StoreItemDefinition>
        {
            new(
                "emoji-rocket",
                "Raketa",
                EmojiType,
                "🚀",
                15),

            new(
                "emoji-skull",
                "Lobanja",
                EmojiType,
                "💀",
                20),

            new(
                "emoji-cold",
                "Ledeno lice",
                EmojiType,
                "🥶",
                20),

            new(
                "border-gold",
                "Zlatni okvir",
                BorderType,
                "border-gold",
                25),

            new(
                "border-neon",
                "Neon okvir",
                BorderType,
                "border-neon",
                30)
        };

    public static StoreItemDefinition? FindByKey(
        string key)
    {
        return Items.FirstOrDefault(
            item =>
                string.Equals(
                    item.Key,
                    key,
                    StringComparison.OrdinalIgnoreCase));
    }

    public static StoreItemDefinition? FindEmoji(
        string emoji)
    {
        return Items.FirstOrDefault(
            item =>
                item.Type == EmojiType &&
                item.Value == emoji);
    }
}