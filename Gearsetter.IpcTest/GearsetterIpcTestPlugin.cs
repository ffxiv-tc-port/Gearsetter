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
        int currentGearsetIndex = RaptureGearsetModule.Instance()->CurrentGearsetIndex;
        var recommendations = _pluginInterface
            .GetIpcSubscriber<byte, List<(uint ItemId, InventoryType? SourceInventory, byte? SourceInventorySlot, RaptureGearsetModule.GearsetItemIndex TargetSlot)>>(
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
