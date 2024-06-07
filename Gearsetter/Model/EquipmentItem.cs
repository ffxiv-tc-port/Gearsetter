using Gearsetter.GameData;
using Lumina.Excel.GeneratedSheets;

namespace Gearsetter.Model;

internal sealed record EquipmentItem(Item Item, bool Hq)
{
    public Item Item { get; } = Item;
    public uint ItemId { get; } = Item.RowId;
    public bool Hq { get; } = Hq;
    public bool CanBeHq { get; } = Item.CanBeHq;
    public string Name { get; } = Item.Name.ToString();
    public byte Level { get; } = Item.LevelEquip;
    public uint ItemLevel { get; } = Item.LevelItem.Row;
    public byte Rarity { get; } = Item.Rarity;
    public EEquipSlotCategory EquipSlotCategory { get; } = (EEquipSlotCategory)Item.EquipSlotCategory.Row;
    public uint ItemUiCategory { get; } = Item.ItemUICategory.Row;
    public EClassJob ClassJob { get; init; } = EClassJob.Adventurer;
    public EquipmentStats Stats { get; } = new(Item, Hq);

    public int PrimaryStat { get; init; } = -1;

    public int Damage => ClassJob.DealsMagicDamage()
        ? Item.DamageMag + Stats.Get(EBaseParam.DamageMag)
        : Item.DamagePhys + Stats.Get(EBaseParam.DamagePhys);

    public bool HasAnyStat(params EBaseParam[] substats)
    {
        foreach (EBaseParam substat in substats)
        {
            if (Stats.Get(substat) > 0)
                return true;
        }

        return false;
    }

    public bool IsCombatRelicWithoutSubstats()
    {
        return Rarity == 4
               && EquipSlotCategory is EEquipSlotCategory.OneHandedMainHand
                   or EEquipSlotCategory.TwoHandedMainHand
                   or EEquipSlotCategory.Shield
               && !ClassJob.IsCrafter()
               && !ClassJob.IsGatherer()
               && !HasAnyStat(EBaseParam.Crit,
                   EBaseParam.DirectHit, EBaseParam.Determination, EBaseParam.SkillSpeed,
                   EBaseParam.SpellSpeed, EBaseParam.Tenacity);
    }
}
