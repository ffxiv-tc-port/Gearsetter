using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gearsetter.GameData;
using Lumina.Excel.GeneratedSheets;

namespace Gearsetter.Model;

internal sealed class ItemList
{
    private static readonly ReadOnlyDictionary<uint, byte> PreferredItems = new Dictionary<uint, byte>()
    {
        { 41081, 90 },
        { 33648, 80 },
        { 24589, 70 },
        { 16039, 50 }
    }.AsReadOnly();

    public required EClassJob ClassJob { get; init; }
    public required EEquipSlotCategory EquipSlotCategory { get; init; }
    public required uint ItemUiCategory { get; init; }
    public required List<EquipmentItem> Items { get; set; }
    public IReadOnlyList<EBaseParam> SubstatPriorities { get; set; } = new List<EBaseParam>();

    public void Sort()
    {
        Items.Sort((a, b) => -Sort(a, b));
    }

    private int Sort(EquipmentItem a, EquipmentItem b)
    {
        // special items
        if (PreferredItems.ContainsKey(a.ItemId) || PreferredItems.ContainsKey(b.ItemId))
        {
            byte? levelA = null;
            byte? levelB = null;
            if (PreferredItems.TryGetValue(a.ItemId, out byte overrideA))
                levelA = overrideA;
            if (PreferredItems.TryGetValue(b.ItemId, out byte overrideB))
                levelB = overrideB;

            if (levelA != null && levelB != null)
                return levelA.Value.CompareTo(levelB.Value);
            else if (levelA != null)
            {
                if (levelA == b.Level)
                    return (a.ItemLevel - 1).CompareTo(b.ItemLevel);
                return levelA.Value.CompareTo(b.Level);
            }
            else if (levelB != null)
            {
                if (a.Level == levelB)
                    return a.ItemLevel.CompareTo(b.ItemLevel - 1);
                return a.Level.CompareTo(levelB.Value);
            }
        }

        // weapons: most damage wins
        int damageA = a.Damage;
        int damageB = b.Damage;
        if (damageA != damageB)
            return damageA.CompareTo(damageB);

        // gear: primary stat wins
        int primaryStatA = a.PrimaryStat;
        int primaryStatB = b.PrimaryStat;
        if (primaryStatA != primaryStatB)
            return primaryStatA.CompareTo(primaryStatB);

        // gear: vitality wins
        int vitalityA = a.Stats.Get(EBaseParam.Vitality);
        int vitalityB = b.Stats.Get(EBaseParam.Vitality);
        if (vitalityA != vitalityB)
            return vitalityA.CompareTo(vitalityB);

        // sum of relevant substats
        int sumOfSubstatsA = SubstatPriorities.Sum(x => a.Stats.Get(x));
        int sumOfSubstatsB = SubstatPriorities.Sum(x => b.Stats.Get(x));

        // some relics have no substats in the sheets, since they can be allocated dynamically
        // they are -generally- better/equal to any other weapon on that ilvl
        if (sumOfSubstatsA == 0 && a.IsCombatRelicWithoutSubstats())
            sumOfSubstatsA = int.MaxValue;
        if (sumOfSubstatsB == 0 && b.IsCombatRelicWithoutSubstats())
            sumOfSubstatsB = int.MaxValue;

        if (sumOfSubstatsA != sumOfSubstatsB)
            return sumOfSubstatsA.CompareTo(sumOfSubstatsB);

        // level-based sorting
        if (a.Level != b.Level)
            return a.Level.CompareTo(b.Level);
        if (a.ItemLevel != b.ItemLevel)
            return a.ItemLevel.CompareTo(b.ItemLevel);
        if (a.Rarity != b.Rarity)
            return a.Rarity.CompareTo(b.Rarity);

        // individual substats
        foreach (EBaseParam substat in SubstatPriorities)
        {
            int substatA = a.Stats.Get(substat);
            int substatB = b.Stats.Get(substat);
            if (substatA != substatB)
                return substatA.CompareTo(substatB);
        }

        // fallback
        return string.CompareOrdinal(a.Name, b.Name);
    }

    public void UpdateStats(Dictionary<EClassJob, EBaseParam> primaryStats, Configuration configuration)
    {
        if (ClassJob.IsTank())
            SubstatPriorities = configuration.StatPriorityTanks;
        else if (ClassJob.IsHealer())
            SubstatPriorities = configuration.StatPriorityHealer;
        else if (ClassJob.IsMelee())
            SubstatPriorities = configuration.StatPriorityMelee;
        else if (ClassJob.IsPhysicalRanged())
            SubstatPriorities = configuration.StatPriorityPhysicalRanged;
        else if (ClassJob.IsCaster())
            SubstatPriorities = configuration.StatPriorityCaster;
        else if (ClassJob.IsCrafter())
            SubstatPriorities = configuration.StatPriorityCrafter;
        else if (ClassJob.IsGatherer())
            SubstatPriorities = configuration.StatPriorityGatherer;
        else
            SubstatPriorities = [];

        if (primaryStats.TryGetValue(ClassJob, out EBaseParam primaryStat))
        {
            Items = Items
                .Select(x => x with { PrimaryStat = x.Stats.Get(primaryStat) })
                .ToList();
        }
    }
}
