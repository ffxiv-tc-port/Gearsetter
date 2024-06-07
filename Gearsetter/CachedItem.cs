using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace Gearsetter;

internal sealed class CachedItem : IEquatable<CachedItem>
{
    public required Item Item { get; init; }
    public required uint ItemId { get; init; }
    public required bool Hq { get; init; }
    public required string Name { get; init; }
    public required byte Level { get; init; }
    public required uint ItemLevel { get; init; }
    public required byte Rarity { get; init; }
    public required uint EquipSlotCategory { get; init; }
    public required IReadOnlyList<ClassJob> ClassJobs { get; set; }

    public int CalculateScore(ClassJob classJob, short level)
    {
        var stats = new Stats(Item, Hq);

        int score = 0;
        if (classJob is >= ClassJob.Miner and <= ClassJob.Fisher)
        {
            score += stats.Get(BaseParam.Gathering) + stats.Get(BaseParam.Perception) + stats.Get(BaseParam.GP);
        }
        else if (classJob is >= ClassJob.Carpenter and <= ClassJob.Culinarian)
        {
            score += stats.Get(BaseParam.Craftsmanship) + stats.Get(BaseParam.Control) + stats.Get(BaseParam.CP);
        }
        else
        {
            if (ItemId == 41081 && level < 90)
                return int.MaxValue;
            else if (ItemId == 33648 && level < 80)
                return int.MaxValue - 1;
            else if (ItemId == 24589 && level < 70)
                return int.MaxValue - 2;
            else if (ItemId == 16039 && level < 50)
                return int.MaxValue - 3;

            if (classJob is ClassJob.Conjurer or ClassJob.WhiteMage or ClassJob.Scholar or ClassJob.Astrologian
                or ClassJob.Sage or ClassJob.Thaumaturge or ClassJob.BlackMage or ClassJob.Arcanist or ClassJob.Summoner
                or ClassJob.RedMage or ClassJob.BlueMage)
            {
                score += 1_000_000 * (Item.DamageMag + stats.Get(BaseParam.DamageMag));
            }
            else
                score += 1_000_000 * (Item.DamagePhys + stats.Get(BaseParam.DamagePhys));

            score += Item.DefensePhys + stats.Get(BaseParam.DefensePhys);
            score += Item.DefenseMag + stats.Get(BaseParam.DefenseMag);

            score += 100 * (stats.Get(BaseParam.Strength) + stats.Get(BaseParam.Dexterity) +
                            stats.Get(BaseParam.Intelligence) + stats.Get(BaseParam.Mind));

            score += Rarity;
        }

        return score;
    }

    public bool Equals(CachedItem? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ItemId == other.ItemId && Hq == other.Hq;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CachedItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ItemId, Hq);
    }

    public static bool operator ==(CachedItem? left, CachedItem? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CachedItem? left, CachedItem? right)
    {
        return !Equals(left, right);
    }

    public enum BaseParam : byte
    {
        Strength = 1,
        Dexterity = 2,
        Vitality = 3,
        Intelligence = 4,
        Mind = 5,
        Piety = 6,

        GP = 10,
        CP = 11,

        DamagePhys = 12,
        DamageMag = 13,

        DefensePhys = 21,
        DefenseMag = 24,

        Tenacity = 19,
        Crit = 27,
        DirectHit = 22,
        Determination = 44,
        SpellSpeed = 46,

        Craftsmanship = 70,
        Control = 71,
        Gathering = 72,
        Perception = 73,
    }

    private sealed class Stats
    {
        private readonly Dictionary<BaseParam, short> _values;

        public Stats(Item item, bool hq)
        {
            _values = item.UnkData59.Where(x => x.BaseParam > 0)
                .ToDictionary(x => (BaseParam)x.BaseParam, x => x.BaseParamValue);
            if (hq)
            {
                foreach (var hqstat in item.UnkData73.Select(x =>
                             ((BaseParam)x.BaseParamSpecial, x.BaseParamValueSpecial)))
                {
                    if (_values.TryGetValue(hqstat.Item1, out var stat))
                        _values[hqstat.Item1] = (short)(stat + hqstat.BaseParamValueSpecial);
                    else
                        _values[hqstat.Item1] = hqstat.BaseParamValueSpecial;
                }
            }
        }

        public short Get(BaseParam param)
        {
            _values.TryGetValue(param, out short v);
            return v;
        }
    }
}
