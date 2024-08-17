using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Plugin.Services;
using Gearsetter.GameData;
using Gearsetter.Model;
using ImGuiNET;
using LLib.GameData;
using LLib.ImGui;

namespace Gearsetter.Windows;

internal sealed class EquipmentBrowserWindow : LWindow
{
    private readonly GearsetterPlugin _plugin;
    private readonly GameDataHolder _dataHolder;
    private readonly IClientState _clientState;
    private readonly IChatGui _chatGui;
    private readonly string[] _classJobNames;
    private readonly EClassJob[] _classJobIds;

    private EClassJob _selectedClassJob = EClassJob.Paladin;
    private EEquipSlotCategory _selectedEquipmentCategory = EEquipSlotCategory.None;
    private string[] _equipmentCategoryNames = [];
    private EEquipSlotCategory[] _equipmentCategoryIds = [];

    private bool _onlyShowOwnedItems;
    private bool _onlyShowEquippableItems;
    private bool _hideNormalQualityItems = true;

    public EquipmentBrowserWindow(GearsetterPlugin plugin, GameDataHolder dataHolder, IClientState clientState,
        IChatGui chatGui)
        : base("Equipment Browser###GearsetterBrowser")
    {
        _plugin = plugin;
        _dataHolder = dataHolder;
        _clientState = clientState;
        _chatGui = chatGui;
        _classJobNames = dataHolder.ClassJobNames
            .Where(x => x.ClassJob.AsJob() == x.ClassJob)
            .Select(x => x.Name)
            .ToArray();
        _classJobIds = dataHolder.ClassJobNames
            .Where(x => x.ClassJob.AsJob() == x.ClassJob)
            .Select(x => x.ClassJob)
            .ToArray();
        UpdateEquipmentCategories();

        Size = new Vector2(800, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 500)
        };
    }

    public override void OnOpen()
    {
        if (_clientState.LocalPlayer != null)
            _selectedClassJob = ((EClassJob)_clientState.LocalPlayer.ClassJob.Id).AsJob();

        UpdateEquipmentCategories();
    }

    public override bool DrawConditions()
    {
        return _clientState.IsLoggedIn;
    }

    public override unsafe void Draw()
    {
        int currentClassJob = Array.IndexOf(_classJobIds, _selectedClassJob);
        if (currentClassJob == -1)
        {
            _selectedClassJob = EClassJob.Paladin;
            currentClassJob = Array.IndexOf(_classJobIds, _selectedClassJob);
            UpdateEquipmentCategories();
        }

        if (ImGui.Combo("Class/Job", ref currentClassJob, _classJobNames, _classJobNames.Length))
        {
            _selectedClassJob = _classJobIds[currentClassJob];
            UpdateEquipmentCategories();
        }

        int currentCategory = Array.IndexOf(_equipmentCategoryIds, _selectedEquipmentCategory);
        if (currentCategory == -1)
        {
            if (_equipmentCategoryIds.Length == 0)
                return;

            _selectedEquipmentCategory = _equipmentCategoryIds[0];
            currentCategory = 0;
        }

        if (ImGui.Combo("Category", ref currentCategory, _equipmentCategoryNames, _equipmentCategoryNames.Length))
            _selectedEquipmentCategory = _equipmentCategoryIds[currentCategory];

        var itemList = _dataHolder.GetItemList(_selectedClassJob, _selectedEquipmentCategory);
        if (itemList == null)
            return;

        ImGui.Checkbox("Only show items matching your level", ref _onlyShowEquippableItems);
        ImGui.SameLine();
        ImGui.Checkbox("Only show owned items", ref _onlyShowOwnedItems);
        ImGui.SameLine();
        ImGui.Checkbox("Hide normal quality items", ref _hideNormalQualityItems);

        Dictionary<(uint ItemId, bool Hq), List<MateriaStats>> ownedItems = _plugin.GetAllInventoryItems();
        try
        {
            itemList.ApplyFromInventory(ownedItems, _onlyShowOwnedItems);

            byte maxLevel = byte.MaxValue;
            if (_onlyShowEquippableItems)
                maxLevel = _plugin.GetLevel(_selectedClassJob);

            bool includeDamage = _selectedEquipmentCategory is EEquipSlotCategory.OneHandedMainHand
                                     or EEquipSlotCategory.Shield
                                     or EEquipSlotCategory.TwoHandedMainHand
                                 && !itemList.ClassJob.IsCrafter()
                                 && !itemList.ClassJob.IsGatherer();

            var hoverColorPtr = ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered);
            var hoverColor = new Vector4(hoverColorPtr->X, hoverColorPtr->Y, hoverColorPtr->Z, 0.3f);

            if (ImGui.BeginTable("ItemList", 2 + (includeDamage ? 1 : 0) + itemList.SubstatPriorities.Count,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 300);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 50);
                if (includeDamage)
                    ImGui.TableSetupColumn("Damage", ImGuiTableColumnFlags.WidthFixed, 50);
                foreach (var substat in itemList.SubstatPriorities)
                    ImGui.TableSetupColumn(_dataHolder.StatNames.GetValueOrDefault(substat, substat.ToString()),
                        ImGuiTableColumnFlags.WidthFixed, 50);

                ImGui.TableHeadersRow();

                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hoverColor);
                foreach (var item in itemList.Items.DistinctBy(x => new
                             { x.ItemId, x.Hq, Materia = x.MateriaStats?.GetHashCode() }))
                {
                    if (item is not InventoryItem)
                    {
                        if (_onlyShowOwnedItems)
                            continue;
                    }

                    if (item.Level > maxLevel)
                        continue;

                    if (_hideNormalQualityItems && item.CanBeHq && !item.Hq)
                        continue;

                    ImGui.TableNextRow();

                    if (ImGui.TableNextColumn())
                    {
                        Vector4? color = item.Rarity switch
                        {
                            2 => ImGuiColors.ParsedGreen,
                            3 => ImGuiColors.ParsedBlue,
                            4 => ImGuiColors.ParsedPurple,
                            7 => ImGuiColors.ParsedPink,
                            _ => null,
                        };

                        string name = item.Name;
                        if (item.Hq)
                            name += $" {SeIconChar.HighQuality.ToIconString()}";
                        if (item is InventoryItem { MateriaStats: not null } inventoryItem)
                            name +=
                                $"    {string.Join("", Enumerable.Repeat(SeIconChar.Circle.ToIconString(), inventoryItem.MateriaStats!.Count))}";

                        if (color != null)
                            ImGui.PushStyleColor(ImGuiCol.Text, color.Value);

                        ImGui.Selectable(name, false, ImGuiSelectableFlags.SpanAllColumns);

                        if (color != null)
                            ImGui.PopStyleColor();

                        if (ImGui.IsItemClicked())
                        {
                            try
                            {
                                _chatGui.Print(SeString.CreateItemLink(item.ItemId, item.Hq));
                            }
                            catch (Exception)
                            {
                                // doesn't matter, just nice-to-have
                            }
                        }
                    }

                    if (ImGui.TableNextColumn())
                    {
                        if (item.Level >= 50 && item.Level % 10 == 0)
                            ImGui.Text(string.Create(CultureInfo.InvariantCulture, $"{item.Level} ({item.ItemLevel})"));
                        else
                            ImGui.Text(item.Level.ToString(CultureInfo.InvariantCulture));
                    }

                    if (includeDamage && ImGui.TableNextColumn())
                        ImGui.Text(item.Damage.ToString(CultureInfo.CurrentCulture));

                    foreach (EBaseParam substat in itemList.SubstatPriorities)
                    {
                        if (ImGui.TableNextColumn())
                        {
                            var estat = item.Stats.GetEquipment(substat);
                            var mstat = item.Stats.GetMateria(substat);
                            if (estat == 0 && mstat == 0)
                                ImGui.Text("-");
                            else if (mstat == 0)
                                ImGui.Text(string.Create(CultureInfo.InvariantCulture, $"{estat}"));
                            else
                                ImGui.Text(string.Create(CultureInfo.InvariantCulture, $"{estat} +{mstat}"));
                        }
                    }
                }

                ImGui.PopStyleColor();
                ImGui.EndTable();
            }
        }
        finally
        {
            itemList.ClearFromInventory();
        }
    }

    private void UpdateEquipmentCategories()
    {
        var categories = _dataHolder.GetItemListsForJob(_selectedClassJob);
        _equipmentCategoryNames = categories.Select(x => x.UiCategoryName).ToArray();
        _equipmentCategoryIds = categories.Select(x => x.EquipSlotCategory).ToArray();

        if (_equipmentCategoryIds.Length > 0 && !_equipmentCategoryIds.Contains(_selectedEquipmentCategory))
            _selectedEquipmentCategory = _equipmentCategoryIds[0];
        else if (_equipmentCategoryIds.Length == 0)
            _selectedEquipmentCategory = EEquipSlotCategory.None;
    }
}
