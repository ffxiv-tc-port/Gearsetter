using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Gearsetter.GameData;

internal sealed class ItemLevelCaps
{
    private readonly ExcelSheet<ExtendedBaseParam> _baseParamSheet;
    private readonly Dictionary<(uint, EBaseParam), ushort> _caps = [];

    public ItemLevelCaps(IDataManager dataManager)
        : this(dataManager.GetExcelSheet<ItemLevel>(), dataManager.GetExcelSheet<ExtendedBaseParam>())
    {
    }

    public ItemLevelCaps(ExcelSheet<ItemLevel> itemLevelSheet, ExcelSheet<ExtendedBaseParam> baseParamSheet)
    {
        _baseParamSheet = baseParamSheet;
        foreach (var itemLevel in itemLevelSheet)
        {
            _caps[(itemLevel.RowId, EBaseParam.Strength)] = itemLevel.Strength;
            _caps[(itemLevel.RowId, EBaseParam.Dexterity)] = itemLevel.Dexterity;
            _caps[(itemLevel.RowId, EBaseParam.Vitality)] = itemLevel.Vitality;
            _caps[(itemLevel.RowId, EBaseParam.Intelligence)] = itemLevel.Intelligence;
            _caps[(itemLevel.RowId, EBaseParam.Mind)] = itemLevel.Mind;
            _caps[(itemLevel.RowId, EBaseParam.Piety)] = itemLevel.Piety;

            _caps[(itemLevel.RowId, EBaseParam.GP)] = itemLevel.GP;
            _caps[(itemLevel.RowId, EBaseParam.CP)] = itemLevel.CP;

            _caps[(itemLevel.RowId, EBaseParam.DamagePhys)] = itemLevel.PhysicalDamage;
            _caps[(itemLevel.RowId, EBaseParam.DamageMag)] = itemLevel.MagicalDamage;

            _caps[(itemLevel.RowId, EBaseParam.DefensePhys)] = itemLevel.Defense;
            _caps[(itemLevel.RowId, EBaseParam.DefenseMag)] = itemLevel.MagicDefense;

            _caps[(itemLevel.RowId, EBaseParam.Tenacity)] = itemLevel.Tenacity;
            _caps[(itemLevel.RowId, EBaseParam.Crit)] = itemLevel.CriticalHit;
            _caps[(itemLevel.RowId, EBaseParam.DirectHit)] = itemLevel.DirectHitRate;
            _caps[(itemLevel.RowId, EBaseParam.Determination)] = itemLevel.Determination;
            _caps[(itemLevel.RowId, EBaseParam.SpellSpeed)] = itemLevel.SpellSpeed;
            _caps[(itemLevel.RowId, EBaseParam.SkillSpeed)] = itemLevel.SkillSpeed;

            _caps[(itemLevel.RowId, EBaseParam.Gathering)] = itemLevel.Gathering;
            _caps[(itemLevel.RowId, EBaseParam.Perception)] = itemLevel.Perception;
            _caps[(itemLevel.RowId, EBaseParam.Craftsmanship)] = itemLevel.Craftsmanship;
            _caps[(itemLevel.RowId, EBaseParam.Control)] = itemLevel.Control;
        }
    }

    public short GetMaximum(Item item, EBaseParam baseParamValue)
    {
        var baseParam = _baseParamSheet.GetRow((uint)baseParamValue);
        return (short)Math.Round(
            _caps[(item.LevelItem.RowId, baseParamValue)] *
            (baseParam.EquipSlotCategoryPct[(int)item.EquipSlotCategory.RowId] / 1000f), MidpointRounding.AwayFromZero);
    }

    // From caraxi/SimpleTWeaks
    [Sheet("BaseParam")]
    public readonly unsafe struct ExtendedBaseParam(ExcelPage page, uint offset, uint row)
        : IExcelRow<ExtendedBaseParam>
    {
        private const int ParamCount = 23;

        public BaseParam BaseParam => new(page, offset, row);

        public Collection<ushort> EquipSlotCategoryPct =>
            new(page, offset, offset, &EquipSlotCategoryPctCtor, ParamCount);

        private static ushort EquipSlotCategoryPctCtor(ExcelPage page, uint parentOffset, uint offset, uint i) =>
            i == 0 ? (ushort)0 : page.ReadUInt16(offset + 8 + (i - 1) * 2);

        public static ExtendedBaseParam Create(ExcelPage page, uint offset, uint row) => new(page, offset, row);
        public uint RowId => row;
    }
}
