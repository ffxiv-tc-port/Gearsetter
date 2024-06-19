using Gearsetter.GameData;
using Gearsetter.Model;
using Lumina.Excel.GeneratedSheets;

namespace Gearsetter.Test;

public sealed class ItemSortingTest
{
    Lumina.GameData _lumina = new( "C:/Program Files (x86)/steam/steamapps/common/FINAL FANTASY XIV Online/game/sqpack" );

    [Fact]
    public void Test1()
    {
        var items = _lumina.GetExcelSheet<Item>()!;
        List<uint> initialItemIds =
        [
            11851,
            11853,
            14447,
            15131,
            16039,
            16240,
            17436,
            25928,
            32558,
        ];

        var itemList = new ItemList
        {
            ClassJob = EClassJob.Marauder,
            EquipSlotCategory = EEquipSlotCategory.Ears,
            ItemUiCategory = 41,
            Items = initialItemIds.Select(rowId => new EquipmentItem(items.GetRow(rowId)!, false)).ToList(),

        };

        var primaryStats = _lumina.GetExcelSheet<ClassJob>()!
            .Where(x => x.RowId > 0 && Enum.IsDefined(typeof(EClassJob), x.RowId))
            .Where(x => x.PrimaryStat > 0)
            .ToDictionary(x => (EClassJob)x.RowId, x => (EBaseParam)x.PrimaryStat);

        itemList.UpdateStats(primaryStats, new Configuration());
        itemList.Sort();

        List<uint> expectedItems =
        [
            32558,
            25928,
            11851,
            17436,
            16240,
            14447,
            11853,
            16039,
            15131, // weathered earrings benefit from having primary stats
        ];
        Assert.Equal(expectedItems, itemList.Items.Select(x => x.ItemId).ToList());
    }
}
