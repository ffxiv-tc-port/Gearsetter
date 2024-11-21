using LLib.GameData;
using Lumina.Excel.Sheets;

namespace Gearsetter.Model;

internal sealed record EquipmentItem(Item Item, bool Hq) : BaseItem(Item, Hq)
{
    public override EClassJob ClassJob { get; init; } = EClassJob.Adventurer;
}
