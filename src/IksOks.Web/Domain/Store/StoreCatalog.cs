namespace IksOks.Web.Domain.Store;

public static class StoreCatalog
{
    public const string EmojiType = "Emoji";
    public const string BorderType = "Border";

    public static IReadOnlyList<StoreItemDefinition> Items { get; } =
        new List<StoreItemDefinition>
        {
            // Emoji
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
                "emoji-target",
                "Meta",
                EmojiType,
                "🎯",
                15),

            new(
                "emoji-lightning",
                "Munja",
                EmojiType,
                "⚡",
                15),

            new(
                "emoji-eyes",
                "Pogled",
                EmojiType,
                "👀",
                15),

            new(
                "emoji-brain",
                "Mozak",
                EmojiType,
                "🧠",
                20),

            new(
                "emoji-trophy",
                "Trofej",
                EmojiType,
                "🏆",
                25),

            new(
                "emoji-heart",
                "Srce",
                EmojiType,
                "❤️",
                20),

            // Borderi
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
                30),

            new(
                "border-purple",
                "Ljubičasti okvir",
                BorderType,
                "border-purple",
                25),

            new(
                "border-fire",
                "Vatreni okvir",
                BorderType,
                "border-fire",
                35),

            new(
                "border-ice",
                "Ledeni okvir",
                BorderType,
                "border-ice",
                35)
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