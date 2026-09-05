using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Gearsetter.IpcTest;

public class GearsetterIpcTestPlugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ICommandManager _commandManager;
    private readonly IChatGui _chatGui;

    public GearsetterIpcTestPlugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager,
        IChatGui chatGui)
    {
        _pluginInterface = pluginInterface;
        _commandManager = commandManager;
        _chatGui = chatGui;

        _commandManager.AddHandler("/recommendedgear", new CommandInfo(ProcessCommand));
    }

    private unsafe void ProcessCommand(string command, string arguments)
    {
        // RaptureGearsetModule.Instance() 走 UIModule，UI 尚未建立時回 null（CS 手寫實作）。
        RaptureGearsetModule* gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
        {
            _chatGui.Print("Gearset module is not available.");
            return;
        }

        int currentGearsetIndex = gearsetModule->CurrentGearsetIndex;
        // 🔴 元組第 3 元在提供端是 int?（Gearsetter/External/GearsetterIpc.cs 與
        //    Model/RecommendedItemChange.SourceInventorySlot 都是 int?）。這裡原本寫成 byte?，
        //    自 2024-08-17 引入 IPC 的第一顆 commit 起就對不上——只是格位索引一直沒超過 255
        //    所以 Newtonsoft 的 JSON 來回轉換剛好都成功。超過 255 就會擲 IpcTypeMismatchError。
        var recommendations = _pluginInterface
            .GetIpcSubscriber<byte, List<(uint ItemId, InventoryType? SourceInventory, int? SourceInventorySlot, RaptureGearsetModule.GearsetItemIndex TargetSlot)>>(
                "Gearsetter.GetRecommendationsForGearset").InvokeFunc((byte)currentGearsetIndex);
        if (recommendations.Count == 0)
            _chatGui.Print($"No recommendations for gearset #{currentGearsetIndex}.");
        else
        {
            foreach (var recommendation in recommendations)
                _chatGui.Print(
                    $"Recommendation: Equip item {recommendation.ItemId} from {recommendation.SourceInventory} (slot {recommendation.SourceInventorySlot}) as {recommendation.TargetSlot}");
        }
    }


    public void Dispose()
    {
        _commandManager.RemoveHandler("/recommendedgear");
    }
}
