using LLib.GameData;

namespace Gearsetter.DataProcessor;

public static class Mappings
{
    public static readonly Dictionary<string, List<uint>> EquipSlotCategories = new()
    {
        { "Weapon", [1, 2, 13] },
        { "Head Gear", [3] },
        { "Chest Gear", [4] },
        { "Hand Gear", [5] },
        { "Leg Gear", [7] },
        { "Foot Gear", [8] },
        { "Earring", [9] },
        { "Necklace", [10] },
        { "Bracelet", [11] },
        { "Ring", [12] },
    };

    public static readonly Dictionary<string, string> AlternateCofferNames = new()
    {
        { "Genji Kabuto", "Genji Head Gear" },
        { "Genji Armor", "Genji Chest Gear" },
        { "Genji Kote", "Genji Hand Gear" },
        { "Genji Tsutsu-hakama", "Genji Leg Gear" },
        { "Genji Sune-ate", "Genji Foot Gear" }
    };

    public static readonly Dictionary<string, List<string>> AlternateSetNames = new()
    {
        { "Inferno", ["Ifrit's"] },
        { "Vortex", ["Garuda's"] },
        { "Tidal", ["Wave"] },
        { "Titania", ["The King's"] },
        {
            "Hades",
            [
                "Misos", "Eulabeia", "Thumosis", "Eleos", "Odune", "Menis", "Zelos", "Aischune", "Phthonos", "Deima",
                "Ekplexis", "Zelotupia", "Oknos", "Epikairekakia", "Agonia", "Kelesis", "Enochlesis", "Himeros"
            ]
        },
        { "Level 50", ["Giantsgall", "Medica Thavnaria"] },
        { "White Oak", ["Deepgold", "Smilodonskin", "Hematite", "Islewolf"] },
        { "Horse Chestnut", ["High Durium", "Gajaskin", "Darkhempen", "Ametrine"] },
        { "Mountain Chromite", ["Loboskin", "Snow Cotton", "Ginseng", "Lar"] },
        { "Queen Eternal", ["Eternal"] }
    };

    public static readonly Dictionary<uint, EClassJob> ArtifactGear = new()
    {
        // IL 290
        { 20275, EClassJob.Paladin },
        { 20276, EClassJob.Monk },
        { 20277, EClassJob.Warrior },
        { 20278, EClassJob.Bard },
        { 20279, EClassJob.WhiteMage },
        { 20280, EClassJob.BlackMage },
        { 20281, EClassJob.Summoner },
        { 20282, EClassJob.Scholar },
        { 20283, EClassJob.Ninja },
        { 20284, EClassJob.DarkKnight },
        { 20285, EClassJob.Machinist },
        { 20286, EClassJob.Astrologian },
        { 20287, EClassJob.Samurai },
        { 20288, EClassJob.RedMage },
        { 20303, EClassJob.Dragoon },

        // IL 115
        { 20620, EClassJob.Samurai },
        { 20621, EClassJob.RedMage },

        // IL 90
        { 20642, EClassJob.Paladin },
        { 20643, EClassJob.Monk },
        { 20644, EClassJob.Warrior },
        { 20645, EClassJob.Dragoon },
        { 20646, EClassJob.Bard },
        { 20647, EClassJob.Ninja },
        { 20648, EClassJob.WhiteMage },
        { 20649, EClassJob.BlackMage },
        { 20650, EClassJob.Summoner },
        { 20651, EClassJob.Scholar },
        { 20652, EClassJob.DarkKnight },
        { 20653, EClassJob.Machinist },
        { 20654, EClassJob.Astrologian },

        // IL 210
        { 20655, EClassJob.Paladin },
        { 20656, EClassJob.Monk },
        { 20657, EClassJob.Warrior },
        { 20658, EClassJob.Dragoon },
        { 20659, EClassJob.Bard },
        { 20660, EClassJob.Ninja },
        { 20661, EClassJob.WhiteMage },
        { 20662, EClassJob.BlackMage },
        { 20663, EClassJob.Summoner },
        { 20664, EClassJob.Scholar },
        { 20665, EClassJob.DarkKnight },
        { 20666, EClassJob.Machinist },
        { 20667, EClassJob.Astrologian },

        { 24587, EClassJob.BlueMage }, // 1
        { 24588, EClassJob.BlueMage }, // 130
        { 32866, EClassJob.BlueMage }, // 400
        { 40355, EClassJob.BlueMage }, // 530

        // IL 255
        { 27266, EClassJob.Gunbreaker },
        { 27267, EClassJob.Dancer },

        // IL 385
        { 35872, EClassJob.Reaper },
        { 35873, EClassJob.Sage },

        // IL 515
        { 43537, EClassJob.Viper },
        { 43538, EClassJob.Pictomancer },
    };

    public static readonly Dictionary<EClassJob, uint> ArtifactJobToCategory = new()
    {
        { EClassJob.Paladin, 20 },
        { EClassJob.Monk, 21 },
        { EClassJob.Warrior, 22 },
        { EClassJob.Dragoon, 23 },
        { EClassJob.Bard, 24 },
        { EClassJob.WhiteMage, 25 },
        { EClassJob.BlackMage, 26 },
        { EClassJob.Summoner, 28 },
        { EClassJob.Scholar, 29 },
        { EClassJob.Ninja, 92 },
        { EClassJob.Machinist, 96 },
        { EClassJob.DarkKnight, 98 },
        { EClassJob.Astrologian, 99 },
        { EClassJob.Samurai, 111 },
        { EClassJob.RedMage, 112 },
        { EClassJob.BlueMage, 129 },
        { EClassJob.Gunbreaker, 149 },
        { EClassJob.Dancer, 150 },
        { EClassJob.Reaper, 180 },
        { EClassJob.Sage, 181 },
        { EClassJob.Viper, 196 },
        { EClassJob.Pictomancer, 197 },
    };
}
