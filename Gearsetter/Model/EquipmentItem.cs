using LLib.GameData;
using LLib.Gear;
using Lumina.Excel.Sheets;

namespace Gearsetter.Model;

internal sealed record EquipmentItem(Item Item, bool Hq, EquipmentStats Stats) : BaseItem(Item, Hq, Stats)
{
    public override EClassJob ClassJob { get; init; } = EClassJob.Adventurer;
}
