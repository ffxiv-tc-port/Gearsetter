using System.Collections.Generic;
using System.Linq;
using Gearsetter.GameData;
using Lumina.Excel.GeneratedSheets;

namespace Gearsetter.Model
{
    internal sealed class EquipmentStats
    {
        private readonly Dictionary<EBaseParam, short> _values;

        public EquipmentStats(Item item, bool hq)
        {
            _values = item.UnkData59.Where(x => x.BaseParam > 0)
                .ToDictionary(x => (EBaseParam)x.BaseParam, x => x.BaseParamValue);
            if (hq)
            {
                foreach (var hqstat in item.UnkData73.Select(x =>
                             ((EBaseParam)x.BaseParamSpecial, x.BaseParamValueSpecial)))
                {
                    if (_values.TryGetValue(hqstat.Item1, out var stat))
                        _values[hqstat.Item1] = (short)(stat + hqstat.BaseParamValueSpecial);
                    else
                        _values[hqstat.Item1] = hqstat.BaseParamValueSpecial;
                }
            }
        }

        public short Get(EBaseParam param)
        {
            _values.TryGetValue(param, out short v);
            return v;
        }
    }
}
