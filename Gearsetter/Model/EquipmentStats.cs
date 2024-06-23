using System.Collections.Generic;
using System.Linq;
using Gearsetter.GameData;
using Lumina.Excel.GeneratedSheets;

namespace Gearsetter.Model;

internal sealed class EquipmentStats
{
    private readonly Dictionary<EBaseParam, short> _equipmentValues;
    private readonly Dictionary<EBaseParam, short> _materiaValues;

    public EquipmentStats(Item item, bool hq, MateriaStats? materiaStats)
    {
        _equipmentValues = item.UnkData59.Where(x => x.BaseParam > 0)
            .ToDictionary(x => (EBaseParam)x.BaseParam, x => x.BaseParamValue);
        if (hq)
        {
            foreach (var hqstat in item.UnkData73.Select(x =>
                         ((EBaseParam)x.BaseParamSpecial, x.BaseParamValueSpecial)))
            {
                if (_equipmentValues.TryGetValue(hqstat.Item1, out var stat))
                    _equipmentValues[hqstat.Item1] = (short)(stat + hqstat.BaseParamValueSpecial);
                else
                    _equipmentValues[hqstat.Item1] = hqstat.BaseParamValueSpecial;
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

    public short Get(EBaseParam param)
    {
        return (short)(GetEquipment(param) + GetMateria(param));
    }

    public short GetEquipment(EBaseParam param)
    {
        _equipmentValues.TryGetValue(param, out short v);
        return v;
    }

    public short GetMateria(EBaseParam param)
    {
        _materiaValues.TryGetValue(param, out short v);
        return v;
    }
}
