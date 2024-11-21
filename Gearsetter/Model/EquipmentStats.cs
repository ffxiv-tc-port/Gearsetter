using System;
using System.Collections.Generic;
using System.Linq;
using Gearsetter.GameData;
using Lumina.Excel.Sheets;

namespace Gearsetter.Model;

internal sealed class EquipmentStats
{
    private readonly Item _item;
    private readonly Dictionary<EBaseParam, short> _equipmentValues;
    private readonly Dictionary<EBaseParam, short> _materiaValues;

    public EquipmentStats(Item item, bool hq, MateriaStats? materiaStats)
    {
        _item = item;
        _equipmentValues = Enumerable.Range(0, item.BaseParam.Count)
            .Where(i => item.BaseParam[i].RowId > 0)
            .ToDictionary(i => (EBaseParam)item.BaseParam[i].RowId, i => item.BaseParamValue[i]);
        if (hq)
        {
            for (int i = 0; i < item.BaseParamSpecial.Count; ++i)
            {
                EBaseParam baseParam = (EBaseParam)item.BaseParamSpecial[i].RowId;
                if (baseParam == EBaseParam.None)
                    continue;

                var baseParamValue = item.BaseParamValueSpecial[i];
                if (_equipmentValues.TryGetValue(baseParam, out var stat))
                    _equipmentValues[baseParam] = (short)(stat + baseParamValue);
                else
                    _equipmentValues[baseParam] = baseParamValue;
            }
        }

        _materiaValues = new();
        if (materiaStats != null)
        {
            foreach (var materiaStat in materiaStats.Values)
            {
                if (_materiaValues.TryGetValue(materiaStat.Key, out var stat))
                    _materiaValues[materiaStat.Key] = (short)(stat + materiaStat.Value);
                else
                    _materiaValues[materiaStat.Key] = materiaStat.Value;
            }
        }
    }

    public short Get(EBaseParam param, ItemLevelCaps? itemLevelCaps)
    {
        return (short)(GetEquipment(param) + GetMateria(param, itemLevelCaps));
    }

    public short GetEquipment(EBaseParam param)
    {
        _equipmentValues.TryGetValue(param, out short v);
        return v;
    }

    public short GetMateria(EBaseParam param, ItemLevelCaps? itemLevelCaps)
    {
        _materiaValues.TryGetValue(param, out short v);
        if (v == 0)
            return v;

        if (param != EBaseParam.DamagePhys && param != EBaseParam.DamageMag)
        {
            // This isn't necessary accurate for Eureka relics, which can (in theory) have +1000 critical hit, but
            // they're both outdated and the limits aren't relevant for a decision of 'which gear piece is better';
            // worst case it'll suggest the i405 savage weapon instead.
            ArgumentNullException.ThrowIfNull(itemLevelCaps);
            short max = itemLevelCaps.GetMaximum(_item, param);
            short equipped = _equipmentValues.GetValueOrDefault(param);
            if (v + equipped > max)
                return (short)(max - equipped);
        }

        return v;
    }

    public bool Has(EBaseParam substat) => _equipmentValues.ContainsKey(substat) || _materiaValues.ContainsKey(substat);
}
