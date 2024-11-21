using LLib.GameData;
using LLib.Gear;
using Lumina.Excel.Sheets;

namespace Gearsetter.Model;

internal sealed record InventoryItem(Item Item, bool Hq, EquipmentStats Stats, EClassJob ClassJob)
    : BaseItem(Item, Hq, Stats)
{
}
