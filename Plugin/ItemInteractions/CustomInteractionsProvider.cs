using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using IcyClawz.CustomInteractions;
using SkillsExtended.Helpers;

namespace SkillsExtended.ItemInteractions;

internal sealed class CustomInteractionsProvider : ICustomInteractionsProvider
{
    private static StaticIcons StaticIcons => EFTHardSettings.Instance.StaticIcons;
    
    public IEnumerable<CustomInteraction> GetCustomInteractions(
        ItemUiContext uiContext, 
        EItemViewType viewType,
        Item item)
    {
        if (viewType is not EItemViewType.Inventory)
            yield break;

        if (!Items.BookIds.Contains(item.TemplateId)) 
            yield break;
        
        // Read book
        yield return new(ItemUiContext.Instance)
        {
            Caption = () => "Read Book",
            Icon = () => StaticIcons.GetItemTypeIcon(EItemType.Info),
            Enabled = () => ReadBookHandler.GetBuffModel(item) != null,
            Action = () => ReadBookHandler.ReadBook(item),
            Error = () => "You are incapable of reading."
        };
    }
}