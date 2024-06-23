using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Gearsetter.GameData;
using Gearsetter.Model;
using Gearsetter.Windows;
using Lumina.Excel.GeneratedSheets;
using GrandCompany = FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace Gearsetter;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class GearsetterPlugin : IDalamudPlugin
{
    private readonly WindowSystem _windowSystem = new(nameof(GearsetterPlugin));
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ICommandManager _commandManager;
    private readonly IChatGui _chatGui;
    private readonly IDataManager _dataManager;
    private readonly IPluginLog _pluginLog;
    private readonly IClientState _clientState;
    private readonly Configuration _configuration;
    private readonly GameDataHolder _gameDataHolder;
    private readonly EquipmentBrowserWindow _equipmentBrowserWindow;
    private readonly ConfigWindow _configWindow;

    private readonly IReadOnlyDictionary<byte, DalamudLinkPayload> _linkPayloads;
    private readonly Dictionary<EClassJob, byte> _classJobToArrayIndex;

    public GearsetterPlugin(DalamudPluginInterface pluginInterface, ICommandManager commandManager, IChatGui chatGui,
        IDataManager dataManager, IPluginLog pluginLog, IClientState clientState)
    {
        ArgumentNullException.ThrowIfNull(dataManager);

        _pluginInterface = pluginInterface;
        _commandManager = commandManager;
        _chatGui = chatGui;
        _dataManager = dataManager;
        _pluginLog = pluginLog;
        _clientState = clientState;

        Configuration? configuration = (Configuration?)_pluginInterface.GetPluginConfig();
        if (configuration == null)
        {
            configuration = Configuration.Create();
            _pluginInterface.SavePluginConfig(configuration);
        }

        _configuration = configuration;
        _gameDataHolder = new GameDataHolder(dataManager, _configuration);
        _equipmentBrowserWindow = new EquipmentBrowserWindow(this, _gameDataHolder, _clientState, _chatGui);
        _windowSystem.AddWindow(_equipmentBrowserWindow);
        _configWindow = new ConfigWindow(_pluginInterface, _configuration);
        _windowSystem.AddWindow(_configWindow);

        _commandManager.AddHandler("/gup", new CommandInfo(ShowUpgrades)
        {
            HelpMessage = "Show possible gear upgrades for all gearsets"
        });
        _commandManager.AddHandler("/gbrowser", new CommandInfo(ToggleEquipmentBrowser)
        {
            HelpMessage = "Toggle the equipment browser window"
        });
        _linkPayloads = Enumerable.Range(0, 100)
            .ToDictionary(x => (byte)x, x => _pluginInterface.AddChatLinkHandler((byte)x, ChangeGearset)).AsReadOnly();
        _clientState.TerritoryChanged += TerritoryChanged;
        _pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        _pluginInterface.UiBuilder.OpenMainUi += _equipmentBrowserWindow.Toggle;
        _pluginInterface.UiBuilder.OpenConfigUi += _configWindow.Toggle;

        _classJobToArrayIndex = dataManager.GetExcelSheet<ClassJob>()!
            .Where(x => x.RowId > 0 && Enum.IsDefined(typeof(EClassJob), x.RowId))
            .ToDictionary(x => (EClassJob)x.RowId, x => (byte)x.ExpArrayIndex);
    }

    private unsafe void TerritoryChanged(ushort territory)
    {
        if (!_configuration.ShowRecommendationsWhenEnteringGcArea)
            return;

        try
        {
            var playerState = PlayerState.Instance();
            if (playerState == null)
                return;

            var grandCompany = (GrandCompany)playerState->GrandCompany;
            if ((grandCompany == GrandCompany.Maelstrom && territory == 128) ||
                (grandCompany == GrandCompany.TwinAdder && territory == 132) ||
                (grandCompany == GrandCompany.ImmortalFlames && territory == 130))
                ShowUpgrades();
        }
        catch (Exception e)
        {
            _pluginLog.Warning(e, "Could not show upgrades when entering Grand Company area.");
        }
    }


    private void ToggleEquipmentBrowser(string command, string arguments)
        => _equipmentBrowserWindow.Toggle();

    private void ShowUpgrades(string command, string arguments)
    {
        byte? level = null;
        if (arguments.Length > 0 && byte.TryParse(arguments, out byte parsedLevel))
            level = parsedLevel;
        ShowUpgrades(level);
    }

    private unsafe void ShowUpgrades(byte? level = null)
    {
        var inventoryItems = GetAllInventoryItems();

        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
            return;

        bool onlyCurrentJob = level != null;
        if (onlyCurrentJob)
            _chatGui.Print("Checking only gearsets for your current class/job...");

        bool anyUpgrade = false;
        for (int i = 0; i < 100; ++i)
        {
            var gearset = gearsetModule->GetGearset(i);
            if (gearset != null && gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
            {
                if (onlyCurrentJob && gearset->ClassJob != _clientState.LocalPlayer!.ClassJob.Id)
                    continue;

                anyUpgrade |= HandleGearset(gearset, inventoryItems, level);
            }
        }

        if (!anyUpgrade)
            _chatGui.Print("All your gearsets are OK.");
    }

    private unsafe bool HandleGearset(RaptureGearsetModule.GearsetEntry* gearset,
        Dictionary<(uint ItemId, bool Hq), List<MateriaStats>> inventoryItems, byte? level)
    {
        string name = GetGearsetName(gearset);
        if (name.Contains('_', StringComparison.Ordinal) ||
            name.Contains("Eureka", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Bozja", StringComparison.OrdinalIgnoreCase))
            return false;

        List<SeString> Handle(string label, int[] spanIds, EEquipSlotCategory category)
        {
            return HandleGearsetItem(label, gearset, spanIds.Select(x => gearset->ItemsSpan[x]).ToArray(),
                inventoryItems, category, level);
        }

        List<List<SeString>> upgrades = new()
        {
            Handle("Main Hand", [0], EEquipSlotCategory.None),
            HandleOffHand(gearset, inventoryItems, level),

            Handle("Head", [2], EEquipSlotCategory.Head),
            Handle("Body", [3], EEquipSlotCategory.Body),
            Handle("Hands", [4], EEquipSlotCategory.Hands),
            Handle("Legs", [6], EEquipSlotCategory.Legs),
            Handle("Feet", [7], EEquipSlotCategory.Feet),

            Handle("Ears", [8], EEquipSlotCategory.Ears),
            Handle("Neck", [9], EEquipSlotCategory.Neck),
            Handle("Wrists", [10], EEquipSlotCategory.Wrists),
            Handle("Rings", [11, 12], EEquipSlotCategory.Rings),
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
                .AddText(level != null ? $" at {level}" : "")
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
        RaptureGearsetModule.GearsetItem[] gearsetItem,
        Dictionary<(uint ItemId, bool Hq), List<MateriaStats>> inventoryItems,
        EEquipSlotCategory equipSlotCategory, byte? level)
    {
        EClassJob classJob = (EClassJob)gearset->ClassJob;
        var itemLists = _gameDataHolder.GetItemLists(classJob);

        if (gearsetItem.Any(x => x.ItemID > 0))
        {
            var firstEquippedItem = gearsetItem.First(x => x.ItemID > 0);
            equipSlotCategory = (EEquipSlotCategory)(_dataManager.GetExcelSheet<Item>()!
                .GetRow(firstEquippedItem.ItemID % 1_000_000)
                ?.EquipSlotCategory?.Row ?? 0);
        }

        if (equipSlotCategory == EEquipSlotCategory.None)
        {
            _pluginLog.Warning($"Unable to find item to determine equip slot category");
            return new List<SeString>();
        }

        BaseItem?[] currentItems = gearsetItem.Select(x => new
            {
                ItemId = x.ItemID % 1_000_000,
                Hq = x.ItemID > 1_000_000
            })
            .Select(x =>
            {
                if (x.ItemId == 0)
                    return null;

                return itemLists
                    .SelectMany(y => y.Items.Where(z => x.ItemId == z.ItemId && x.Hq == z.Hq))
                    .FirstOrDefault();
            })
            .ToArray();

        var availableList = _gameDataHolder.GetItemList(classJob, equipSlotCategory);
        if (availableList == null)
            return new List<SeString>();

        if (level == null)
            level = GetLevel(classJob);

        try
        {
            availableList.ApplyFromInventory(inventoryItems, true);

            var bestItems = availableList.Items
                .Where(x => x.Level <= level)
                .Where(x => x is Model.InventoryItem)
                .Take(gearsetItem.Length)
                .ToList();
            _pluginLog.Debug(
                $"{equipSlotCategory}: {string.Join("    ", currentItems.Select(x => $"{x?.ItemId}|{x?.Hq}"))}");
            foreach (var currentItem in currentItems)
            {
                var foundIndex = bestItems.FindIndex(x =>
                    currentItem != null && currentItem.ItemId == x.ItemId && currentItem.Hq == x.Hq);
                if (foundIndex >= 0)
                    bestItems.RemoveAt(foundIndex);
            }

            return bestItems
                .Select(x => new SeString(new TextPayload($"{label}: "))
                    .Append(SeString.CreateItemLink(x.ItemId, x.Hq))).ToList();
        }
        finally
        {
            availableList.ClearFromInventory();
        }
    }


    private unsafe List<SeString> HandleOffHand(RaptureGearsetModule.GearsetEntry* gearset,
        Dictionary<(uint ItemId, bool Hq), List<MateriaStats>> inventoryItems, byte? level)
    {
        var mainHand = gearset->ItemsSpan[0];
        if (mainHand.ItemID == 0)
            return new List<SeString>();

        // if it's a twohanded weapon, ignore it
        EEquipSlotCategory equipSlotCategory =
            (EEquipSlotCategory)(_dataManager.GetExcelSheet<Item>()!.GetRow(mainHand.ItemID % 1_000_000)?.RowId ?? 0);
        if (equipSlotCategory != EEquipSlotCategory.OneHandedMainHand)
            return new List<SeString>();

        return HandleGearsetItem("Off Hand", gearset, [gearset->ItemsSpan[1]], inventoryItems,
            EEquipSlotCategory.Shield, level);
    }

    private unsafe void ChangeGearset(uint commandId, SeString seString)
        => RaptureGearsetModule.Instance()->EquipGearset((byte)commandId);

    internal unsafe Dictionary<(uint ItemId, bool Hq), List<MateriaStats>> GetAllInventoryItems()
    {
        Dictionary<(uint, bool), List<MateriaStats>> inventoryItems = new();
        InventoryManager* inventoryManager = InventoryManager.Instance();
        foreach (var inventoryType in _gameDataHolder.DefaultInventoryTypes)
        {
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            for (int i = 0; i < container->Size; ++i)
            {
                var item = container->GetInventorySlot(i);
                if (item != null && item->ItemID != 0)
                {
                    var key = (item->ItemID, item->Flags.HasFlag(InventoryItem.ItemFlags.HQ));
                    if (!inventoryItems.TryGetValue(key, out var list))
                    {
                        list = new List<MateriaStats>();
                        inventoryItems[key] = list;
                    }


                    byte materiaCount = item->GetMateriaCount();
                    var materias = Enumerable.Range(0, materiaCount)
                        .Select<int, (MateriaStat, byte)?>(slot =>
                        {
                            if (_gameDataHolder.Materias.TryGetValue(item->GetMateriaId((byte)slot),
                                    out MateriaStat? value))
                                return (value, item->GetMateriaGrade((byte)slot));
                            else
                                return null;
                        })
                        .Where(x => x != null)
                        .Select(x => x!.Value)
                        .ToList();
                    list.Add(new MateriaStats(materias));
                }
            }
        }

        return inventoryItems;
    }

    internal unsafe byte GetLevel(EClassJob classJob)
    {
        var playerState = PlayerState.Instance();
        if (playerState == null)
            return 0;

        return (byte)playerState->ClassJobLevelArray[_classJobToArrayIndex[classJob]];
    }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.OpenConfigUi -= _configWindow.Toggle;
        _pluginInterface.UiBuilder.OpenMainUi -= _equipmentBrowserWindow.Toggle;
        _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _clientState.TerritoryChanged -= TerritoryChanged;
        _pluginInterface.RemoveChatLinkHandler();
        _commandManager.RemoveHandler("/gbrowser");
        _commandManager.RemoveHandler("/gup");
    }
}
