using System;
using System.Collections.Generic;
using System.Linq;
using Gearsetter.GameData;

namespace Gearsetter.Model;

internal sealed class MateriaStats
{
    private readonly List<(EBaseParam, short)> _list;

    public MateriaStats(IEnumerable<(MateriaStat Stat, byte Grade)> materias)
    {
        foreach (var materia in materias)
        {
            if (Values.TryGetValue(materia.Stat.BaseParam, out short value))
                Values[materia.Stat.BaseParam] = (short)(value + materia.Stat.Values[materia.Grade]);
            else
                Values[materia.Stat.BaseParam] = materia.Stat.Values[materia.Grade];

            ++Count;
        }

        _list = Enum.GetValues<EBaseParam>()
            .Select(x => (x, Values.GetValueOrDefault(x, (short)0)))
            .Where(x => x.Item2 > 0)
            .ToList();
    }

    public Dictionary<EBaseParam, short> Values { get; } = new();
    public byte Count { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not MateriaStats other)
            return false;

        return _list == other._list;
    }

    public override int GetHashCode()
    {
        int hash = 19;
        hash = hash * 31 + Count;
        foreach (var item in _list)
            hash = hash * 31 + item.GetHashCode();
        return hash;
    }
}
