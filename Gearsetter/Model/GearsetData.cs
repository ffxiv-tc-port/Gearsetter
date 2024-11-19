using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using LLib.GameData;
using Lumina.Excel.Sheets;

namespace Gearsetter.Model;

internal sealed class GearsetData
{
    public unsafe GearsetData(IDataManager dataManager, RaptureGearsetModule.GearsetEntry* gearset, string name)
    {
        Id = gearset->Id;
        ClassJob = (EClassJob)gearset->ClassJob;
        Name = name;
        MainHand = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.MainHand);
        OffHand = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.OffHand);
        Head = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Head);
        Body = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Body);
        Hands = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Hands);
        Legs = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Legs);
        Feet = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Feet);
        Ears = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Ears);
        Neck = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Neck);
        Wrists = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.Wrists);
        RingLeft = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.RingLeft);
        RingRight = GetItem(dataManager, gearset, RaptureGearsetModule.GearsetItemIndex.RingRight);
    }

    private static unsafe EquipmentItem? GetItem(IDataManager dataManager, RaptureGearsetModule.GearsetEntry* gearset,
        RaptureGearsetModule.GearsetItemIndex index)
    {
        var gearsetItem = gearset->GetItem(index);
        if (gearsetItem.ItemId == 0)
            return null;

        var item = dataManager.GetExcelSheet<Item>().GetRow(gearsetItem.ItemId % 1_000_000);
        return new EquipmentItem(item, gearsetItem.ItemId > 1_000_000);
    }

    public byte Id { get; }
    public EClassJob ClassJob { get; }
    public string Name { get; }
    public EquipmentItem? MainHand { get; }
    public EquipmentItem? OffHand { get; }
    public EquipmentItem? Head { get; }
    public EquipmentItem? Body { get; }
    public EquipmentItem? Hands { get; }
    public EquipmentItem? Legs { get; }
    public EquipmentItem? Feet { get; }
    public EquipmentItem? Ears { get; }
    public EquipmentItem? Neck { get; }
    public EquipmentItem? Wrists { get; }
    public EquipmentItem? RingLeft { get; }
    public EquipmentItem? RingRight { get; }
}
