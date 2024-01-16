using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;

namespace Gearsetter;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class GearsetterPlugin : IDalamudPlugin
{
    private static readonly InventoryType[] DefaultInventoryTypes =
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
        InventoryType.ArmoryMainHand,
        InventoryType.ArmoryOffHand,
        InventoryType.ArmoryHead,
        InventoryType.ArmoryBody,
        InventoryType.ArmoryHands,
        InventoryType.ArmoryLegs,
        InventoryType.ArmoryFeets,
        InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist,
        InventoryType.ArmoryRings,
        InventoryType.EquippedItems,
    };

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ICommandManager _commandManager;
    private readonly IChatGui _chatGui;
    private readonly IDataManager _dataManager;
    private readonly IPluginLog _pluginLog;
    private readonly IClientState _clientState;

    private readonly IReadOnlyDictionary<byte, DalamudLinkPayload> _linkPayloads;
    private readonly Dictionary<uint, List<ClassJob>> _classJobCategories;
    private readonly Dictionary<byte, byte> _classJobToArrayIndex;
    private readonly Dictionary<uint, CachedItem> _cachedItems = new();

    public GearsetterPlugin(DalamudPluginInterface pluginInterface, ICommandManager commandManager, IChatGui chatGui,
        IDataManager dataManager, IPluginLog pluginLog, IClientState clientState)
    {
        _pluginInterface = pluginInterface;
        _commandManager = commandManager;
        _chatGui = chatGui;
        _dataManager = dataManager;
        _pluginLog = pluginLog;
        _clientState = clientState;

        _commandManager.AddHandler("/gup", new CommandInfo(ProcessCommand));
        _linkPayloads = Enumerable.Range(0, 100)
            .ToDictionary(x => (byte)x, x => _pluginInterface.AddChatLinkHandler((byte)x, ChangeGearset)).AsReadOnly();
        _clientState.TerritoryChanged += TerritoryChanged;

        _classJobToArrayIndex = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.ClassJob>()!
            .Where(x => x.RowId > 0)
            .ToDictionary(x => (byte)x.RowId, x => (byte)x.ExpArrayIndex);
        _classJobCategories = _dataManager.GetExcelSheet<ClassJobCategory>()!
            .ToDictionary(x => x.RowId, x =>
                new Dictionary<ClassJob, bool>
                    {
                        { ClassJob.Adventurer, x.ADV },
                        { ClassJob.Gladiator, x.GLA },
                        { ClassJob.Pugilist, x.PGL },
                        { ClassJob.Marauder, x.MRD },
                        { ClassJob.Lancer, x.LNC },
                        { ClassJob.Archer, x.ARC },
                        { ClassJob.Conjurer, x.CNJ },
                        { ClassJob.Thaumaturge, x.THM },
                        { ClassJob.Carpenter, x.CRP },
                        { ClassJob.Blacksmith, x.BSM },
                        { ClassJob.Armorer, x.ARM },
                        { ClassJob.Goldsmith, x.GSM },
                        { ClassJob.Leatherworker, x.LTW },
                        { ClassJob.Weaver, x.WVR },
                        { ClassJob.Alchemist, x.ALC },
                        { ClassJob.Culinarian, x.CUL },
                        { ClassJob.Miner, x.MIN },
                        { ClassJob.Botanist, x.BTN },
                        { ClassJob.Fisher, x.FSH },
                        { ClassJob.Paladin, x.PLD },
                        { ClassJob.Monk, x.MNK },
                        { ClassJob.Warrior, x.WAR },
                        { ClassJob.Dragoon, x.DRG },
                        { ClassJob.Bard, x.BRD },
                        { ClassJob.WhiteMage, x.WHM },
                        { ClassJob.BlackMage, x.BLM },
                        { ClassJob.Arcanist, x.ACN },
                        { ClassJob.Summoner, x.SMN },
                        { ClassJob.Scholar, x.SCH },
                        { ClassJob.Rogue, x.ROG },
                        { ClassJob.Ninja, x.NIN },
                        { ClassJob.Machinist, x.MCH },
                        { ClassJob.DarkKnight, x.DRK },
                        { ClassJob.Astrologian, x.AST },
                        { ClassJob.Samurai, x.SAM },
                        { ClassJob.RedMage, x.RDM },
                        { ClassJob.BlueMage, x.BLU },
                        { ClassJob.Gunbreaker, x.GNB },
                        { ClassJob.Dancer, x.DNC },
                        { ClassJob.Reaper, x.RPR },
                        { ClassJob.Sage, x.SGE },
                    }
                    .Where(y => y.Value)
                    .Select(y => y.Key)
                    .ToList());
    }

    private void TerritoryChanged(ushort territory)
    {
        if (territory == 128)
            ShowUpgrades();
    }

    private void ProcessCommand(string command, string arguments) => ShowUpgrades();

    private unsafe void ShowUpgrades()
    {
        var inventoryManager = InventoryManager.Instance();
        List<CachedItem> inventoryItems = new();
        foreach (var inventoryType in DefaultInventoryTypes)
        {
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            for (int i = 0; i < container->Size; ++i)
            {
                var item = container->GetInventorySlot(i);
                if (item != null && item->ItemID != 0)
                {
                    CachedItem? cachedItem = LookupItem(item->ItemID, item->Flags.HasFlag(InventoryItem.ItemFlags.HQ));
                    if (cachedItem != null)
                        inventoryItems.Add(cachedItem);
                }
            }
        }

        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
            return;

        bool anyUpgrade = false;
        for (int i = 0; i < 100; ++i)
        {
            var gearset = gearsetModule->GetGearset(i);
            if (gearset != null && gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
            {
                anyUpgrade |= HandleGearset(gearset, inventoryItems);
            }
        }

        if (!anyUpgrade)
            _chatGui.Print("All your gearsets are OK.");
    }

    private unsafe bool HandleGearset(RaptureGearsetModule.GearsetEntry* gearset, List<CachedItem> inventoryItems)
    {
        string name = GetGearsetName(gearset);
        if (name.Contains('_') || name.Contains("Eureka") || name.Contains("Bozja"))
            return false;

        List<List<SeString>> upgrades = new()
        {
            HandleGearsetItem("Main Hand", gearset, gearset->MainHand, inventoryItems),
            HandleGearsetItem("Off Hand", gearset, gearset->OffHand, inventoryItems),

            HandleGearsetItem("Head", gearset, gearset->Head, inventoryItems),
            HandleGearsetItem("Body", gearset, gearset->Body, inventoryItems),
            HandleGearsetItem("Hands", gearset, gearset->Hands, inventoryItems),
            HandleGearsetItem("Legs", gearset, gearset->Legs, inventoryItems),
            HandleGearsetItem("Feet", gearset, gearset->Feet, inventoryItems),

            HandleGearsetItem("Ears", gearset, gearset->Ears, inventoryItems),
            HandleGearsetItem("Neck", gearset, gearset->Neck, inventoryItems),
            HandleGearsetItem("Wrists", gearset, gearset->Wrists, inventoryItems),
            HandleGearsetItem("Rings", gearset, new[] { gearset->RingRight, gearset->RingLeft }, inventoryItems),
        };

        List<SeString> flatUpgrades = upgrades.SelectMany(x => x).ToList();
        if (flatUpgrades.Count == 0)
            return false;

        _chatGui.Print(
            new SeStringBuilder()
                .Append("Gearset ")
                .AddUiForeground(1)
                .Add(_linkPayloads[gearset->ID])
                .Append($"#{gearset->ID + 1}: ")
                .Append(name)
                .Add(RawPayload.LinkTerminator)
                .AddUiForegroundOff()
                .Build());

        foreach (var upgrade in flatUpgrades)
            _chatGui.Print(new SeString(new TextPayload("  - ")).Append(upgrade));

        return true;
    }

    /// <summary>
    /// This probably includes the ilvl; at the very minimum attempting to print this directly to chat will act as if
    /// the string ends after the name (and not render ANY text on the same line after the name).
    /// </summary>
    private unsafe string GetGearsetName(RaptureGearsetModule.GearsetEntry* gearset)
        => Encoding.UTF8.GetString(gearset->Name, 0x2F).Split((char)0)[0];

    private unsafe List<SeString> HandleGearsetItem(string label, RaptureGearsetModule.GearsetEntry* gearset,
        RaptureGearsetModule.GearsetItem gearsetItem, List<CachedItem> inventoryItems)
        => HandleGearsetItem(label, gearset, new[] { gearsetItem }, inventoryItems);

    private unsafe List<SeString> HandleGearsetItem(string label, RaptureGearsetModule.GearsetEntry* gearset,
        RaptureGearsetModule.GearsetItem[] gearsetItem, List<CachedItem> inventoryItems)
    {
        gearsetItem = gearsetItem.Where(x => x.ItemID != 0).ToArray();
        if (gearsetItem.Length > 0)
        {
            ClassJob classJob = (ClassJob)gearset->ClassJob;
            CachedItem[] currentItems = gearsetItem.Select(x => LookupItem(x.ItemID)).Where(x => x != null)
                .Select(x => x!).ToArray();
            if (currentItems.Length == 0)
            {
                _pluginLog.Information($"Unable to find gearset items");
                return new List<SeString>();
            }

            var level = PlayerState.Instance()->ClassJobLevelArray[
                _classJobToArrayIndex[gearset->ClassJob]];

            var bestItems = inventoryItems
                .Where(x => x.EquipSlotCategory == currentItems[0].EquipSlotCategory)
                .Where(x => x.Level <= level)
                .Where(x => x.ClassJobs.Contains(classJob))
                .Where(x => x.CalculateScore(classJob, level) > 0)
                .OrderByDescending(x => x.CalculateScore(classJob, level))
                .Take(gearsetItem.Length)
                .ToList();
            foreach (var currentItem in currentItems)
            {
                if (bestItems.Contains(currentItem))
                    bestItems.Remove(currentItem);
            }

            // don't make suggestions for equal scores
            bestItems.RemoveAll(x =>
                x.CalculateScore(classJob, level) ==
                currentItems.Select(y => y.CalculateScore(classJob, level)).Max());

            return bestItems
                .Select(x => new SeString(new TextPayload($"{label}: "))
                    .Append(SeString.CreateItemLink(x.ItemId, x.Hq))).ToList();
        }

        return new List<SeString>();
    }

    private CachedItem? LookupItem(uint itemId)
    {
        if (_cachedItems.TryGetValue(itemId, out CachedItem? cachedItem))
            return cachedItem;

        try
        {
            var item = _dataManager.GetExcelSheet<Item>()!.GetRow(itemId % 1_000_000)!;
            cachedItem = new CachedItem
            {
                Item = item,
                ItemId = item.RowId,
                Hq = itemId > 1_000_000,
                Name = item.Name.ToString(),
                Level = item.LevelEquip,
                ItemLevel = item.LevelItem.Row,
                Rarity = item.Rarity,
                EquipSlotCategory = item.EquipSlotCategory.Row,
                ClassJobs = _classJobCategories[item.ClassJobCategory.Row],
            };
            _cachedItems[itemId] = cachedItem;
            return cachedItem;
        }
        catch (Exception)
        {
            _pluginLog.Information($"Unable to lookup item {itemId}");
            return null;
        }
    }

    private CachedItem? LookupItem(uint itemId, bool hq)
        => LookupItem(itemId + (hq ? 1_000_000u : 0));

    private unsafe void ChangeGearset(uint commandId, SeString seString)
        => RaptureGearsetModule.Instance()->EquipGearset((byte)commandId);

    public void Dispose()
    {
        _clientState.TerritoryChanged -= TerritoryChanged;
        _pluginInterface.RemoveChatLinkHandler();
        _commandManager.RemoveHandler("/gup");
    }
}
