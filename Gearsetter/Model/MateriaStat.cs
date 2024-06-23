using System.Collections.Generic;
using System.Linq;
using Gearsetter.GameData;

namespace Gearsetter.Model;

internal sealed class MateriaStat(EBaseParam baseParam, short[] values)
{
    public EBaseParam BaseParam { get; } = baseParam;
    public List<short> Values { get; } = values.ToList();
}
