using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Gearsetter.Model;

internal sealed record RecommendedItemChange(
    uint ItemId,
    InventoryType? SourceInventory,
    int? SourceInventorySlot,
    SeString Text);
