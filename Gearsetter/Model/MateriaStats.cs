using System;
using System.Collections.Generic;
using System.Linq;
using Gearsetter.GameData;

namespace Gearsetter.Model;

internal sealed class MateriaStats : IEquatable<MateriaStats>
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
        return ReferenceEquals(this, obj) || obj is MateriaStats other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _list.GetHashCode();
    }

    public bool Equals(MateriaStats? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _list.SequenceEqual(other._list);
    }

    public static bool operator ==(MateriaStats? left, MateriaStats? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MateriaStats? left, MateriaStats? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"Materias[{string.Join(", ", _list.Select(x => $"{x.Item1}:{x.Item2}"))}]";
    }
}
