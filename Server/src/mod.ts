/* eslint-disable @typescript-eslint/naming-convention */

import { InstanceManager } from "./Managers/InstanceManager";

import path from "node:path";

import type { DependencyContainer } from "tsyringe";
import type { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { CustomItemService } from "@spt/services/mod/CustomItemService";
import type { NewItemFromCloneDetails } from "@spt/models/spt/mod/NewItemDetails";
import type { ISkillsConfig } from "./Models/ISkillsConfig";

import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import { ProgressionManager } from "./Managers/ProgressionManager";
import { IOManager } from "./Managers/IOManager";
import { RouteManager } from "./Managers/RouteManager";
import { AchievementManager } from "./Managers/AchievementManager";
import { SkillsExtendedIds } from "./enums/SkillsExtendedIds";

class SkillsExtended implements IPreSptLoadMod, IPostDBLoadMod
{
    private InstanceManager: InstanceManager = new InstanceManager();
    
    private IOManager: IOManager = new IOManager(this.InstanceManager); 
    private ProgressionManager: ProgressionManager = new ProgressionManager();
    private AchievementManager: AchievementManager = new AchievementManager();
    private RouteManager: RouteManager = new RouteManager();
    
    private customItemService: CustomItemService;
    public SkillsConfig: ISkillsConfig;

    public async preSptLoad(container: DependencyContainer): Promise<void> 
    {
        this.InstanceManager.preSptLoad(container);
        await this.IOManager.preSptLoad();

        this.SkillsConfig = await this.IOManager.loadJsonFile<ISkillsConfig>(path.join(this.IOManager.ConfigPath, "SkillsConfig.json5"));

        this.RouteManager.preSptLoad(this.InstanceManager, this.ProgressionManager, this.SkillsConfig, this.IOManager);    
    }

    public async postDBLoad(container: DependencyContainer): Promise<void> 
    {
        this.InstanceManager.logger.logWithColor("Skills Extended loading", LogTextColor.GREEN);

        this.InstanceManager.postDBLoad(container);

        this.ProgressionManager.init(this.InstanceManager, this.IOManager);
        this.customItemService = this.InstanceManager.customItemService;
        await this.AchievementManager.postDbLoad(this.InstanceManager, this.IOManager);

        this.addCraftsToDatabase();
        await this.CreateItems();
        // Do this after so we dont wipe locales with create items
        this.IOManager.importData();

        this.addItemToSpecSlots(SkillsExtendedIds.Lockpick);
        this.addItemToSpecSlots(SkillsExtendedIds.Pda);
    }

    private async CreateItems(): Promise<void>
    {
        const items = await this.IOManager.loadJsonFile<NewItemFromCloneDetails[]>(path.join(this.IOManager.ItemRootPath, "Items.json"));

        for (const item of items)
        {
            this.customItemService.createItemFromClone(item);
        }

        this.InstanceManager.logger.logWithColor(`Skills Extended: Loaded ${items.length} custom items`, LogTextColor.GREEN);
    }

    private addCraftsToDatabase(): void
    {
        const crafts = this.SkillsConfig.LockPicking.CRAFTING_RECIPES;

        for (const craft of crafts)
        {
            this.InstanceManager.database.hideout.production.recipes.push(craft);
        }
    }

    private addItemToSpecSlots(itemId: string): void
    {
        // Allow in spec slot
        const items = this.InstanceManager.database.templates.items;

        for (const item in items)
        {
            const id = items[item]._id;
            
            if (id !== "627a4e6b255f7527fb05a0f6" && id !== "65e080be269cbd5c5005e529") continue;

            items[item]._props.Slots[0]._props.filters[0].Filter.push(itemId);
            items[item]._props.Slots[1]._props.filters[0].Filter.push(itemId);
            items[item]._props.Slots[2]._props.filters[0].Filter.push(itemId);
        }
    }
}

module.exports = { mod: new SkillsExtended() }