using LLib.GameData;
using Lumina.Excel.Sheets;

namespace Gearsetter.Model;

internal sealed record InventoryItem(Item Item, bool Hq, MateriaStats MateriaStats, EClassJob ClassJob)
    : BaseItem(Item, Hq, MateriaStats)
{
}
