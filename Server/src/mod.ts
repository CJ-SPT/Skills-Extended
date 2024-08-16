/* eslint-disable @typescript-eslint/naming-convention */

import { InstanceManager } from "./Managers/InstanceManager";

import fs from "fs";
import path from "node:path";

import type { DependencyContainer } from "tsyringe";
import type { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { CustomItemService } from "@spt/services/mod/CustomItemService";
import type { NewItemFromCloneDetails } from "@spt/models/spt/mod/NewItemDetails";

import { Money } from "@spt/models/enums/Money";
import { Traders } from "@spt/models/enums/Traders";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import { ProgressionManager } from "./Managers/ProgressionManager";
import { IOManager } from "./Managers/IOManager";
import { CustomItemIds } from "./enums/CustomItemIds";
import { RouteManager } from "./Managers/RouteManager";
import type { ISkillsConfig } from "./Models/ISkillsConfig";
import { AchievementManager } from "./Managers/AchievementManager";
import { TraderManager } from "./Managers/TraderManager";

class SkillsExtended implements IPreSptLoadMod, IPostDBLoadMod
{
    private InstanceManager: InstanceManager = new InstanceManager();
    
    private IOManager: IOManager = new IOManager(this.InstanceManager); 
    private ProgressionManager: ProgressionManager = new ProgressionManager();
    private AchievementManager: AchievementManager = new AchievementManager();
    private TraderManager: TraderManager = new TraderManager();  
    private RouteManager: RouteManager = new RouteManager();
    
    private customItemService: CustomItemService;
    public SkillsConfig: ISkillsConfig; // TODO: Type this

    public preSptLoad(container: DependencyContainer): void 
    {
        this.InstanceManager.preSptLoad(container);

        this.SkillsConfig = this.IOManager.loadJsonFile<ISkillsConfig>(path.join(this.IOManager.ConfigPath, "SkillsConfig.json5"));

        this.RouteManager.preSptLoad(this.InstanceManager, this.ProgressionManager, this.SkillsConfig);
        this.TraderManager.preSptLoad(this.InstanceManager, this.IOManager);

        this.InstanceManager.logger.logWithColor("Skills Extended loading", LogTextColor.GREEN);    
    }

    public postDBLoad(container: DependencyContainer): void 
    {
        this.InstanceManager.postDBLoad(container);
        this.ProgressionManager.init(this.InstanceManager, this.IOManager);
        this.customItemService = this.InstanceManager.customItemService;
        this.AchievementManager.postDbLoad(this.InstanceManager, this.IOManager);
        this.TraderManager.postDbLoad();

        this.loadSkillLocales();
        this.CreateItems();
        this.addCraftsToDatabase();
    }

    private loadSkillLocales(): void
    {
        const skillLocalePath = path.join(this.IOManager.LocaleRootPath, "Skills");

        const files = fs.readdirSync(skillLocalePath);
        
        const jsonFiles = files
            .filter(file => path.extname(file) === ".json")
            .map(file => path.basename(file, ".json"));

        for (const file of jsonFiles)
        {
            // Portugeese locale uses `po` not `pt`
            // Skip because I originally set it as pt by mistake.
            // Dont trust users to delete my mistake /BONK
            if (file === "pt") continue;

            const filePath = path.join(skillLocalePath, `${file}.json`);
            const localeFile = this.IOManager.loadJsonFile<Record<string, string>>(filePath);
            const global = this.InstanceManager.database.locales.global[file];

            if (Object.keys(localeFile).length > 0)
            {
                this.InstanceManager.logger.logWithColor(`Skills Extended: Loading Skill locale '${file}'`, LogTextColor.GREEN);
            }
            
            for (const locale in localeFile)
            {
                global[locale] = localeFile[locale];
            }
        }

        this.InstanceManager.logger.logWithColor("Skills Extended: Locales dynamically loaded. Select your locale in-game on the settings page!", LogTextColor.GREEN);
    }

    private CreateItems(): void
    {
        this.CreateLockpick();
        this.CreatePDA();
    }

    // Clones factory key to be used as a blank for bump lock picking
    private CreateLockpick(): void
    {
        const lockPick: NewItemFromCloneDetails = {
            itemTplToClone: "5448ba0b4bdc2d02308b456c",
            overrideProperties: {
                CanSellOnRagfair: false,
                MaximumNumberOfUsage: 5,
                Unlootable: true,
                UnlootableFromSlot: "SpecialSlot",
                UnlootableFromSide: [
                    "Bear",
                    "Usec",
                    "Savage"
                ],
                Prefab: {
                    path: "lockpick.bundle",
                    rcid: ""
                },
                BackgroundColor: "orange"
                
            },

            parentId: "5c99f98d86f7745c314214b3",
            newId: CustomItemIds.Lockpick,
            fleaPriceRoubles: 120000,
            handbookPriceRoubles: 75000,
            handbookParentId: "5c518ec986f7743b68682ce2",

            locales: {
                en: {
                    name: "Lockpick set",
                    shortName: "Lockpick",
                    description: "A set of tools used for picking locks"
                }
            }
        }

        this.customItemService.createItemFromClone(lockPick);

        const mechanic = this.InstanceManager.database.traders[Traders.MECHANIC];
        
        mechanic.assort.items.push({
            _id: CustomItemIds.Lockpick,
            _tpl: CustomItemIds.Lockpick,
            parentId: "hideout",
            slotId: "hideout",
            upd:
            {
                UnlimitedCount: false,
                StackObjectsCount: 10
            }
        });

        mechanic.assort.barter_scheme[CustomItemIds.Lockpick] = [
            [
                {
                    count: 75000,
                    _tpl: Money.ROUBLES
                }
            ]
        ];
        
        mechanic.assort.loyal_level_items[CustomItemIds.Lockpick] = 2;

        this.addItemToSpecSlots(CustomItemIds.Lockpick);
    }

    private CreatePDA(): void
    {
        const Pda: NewItemFromCloneDetails = {
            itemTplToClone: "5bc9b720d4351e450201234b",
            overrideProperties: {
                CanSellOnRagfair: false,
                Unlootable: true,
                UnlootableFromSlot: "SpecialSlot",
                UnlootableFromSide: [
                    "Bear",
                    "Usec",
                    "Savage"
                ],
                Prefab: {
                    path: "pda.bundle",
                    rcid: ""
                }
            },

            parentId: "5c164d2286f774194c5e69fa",
            newId: CustomItemIds.Pda,
            fleaPriceRoubles: 3650000,
            handbookPriceRoubles: 7560000,
            handbookParentId: "5c164d2286f774194c5e69fa",

            locales: {
                en: {
                    name: "Flipper zero",
                    shortName: "Flipper",
                    description: "A hacking device used for gaining access to key card doors. Requires Lockpicking level 25 to use."
                }
            }
        }

        this.customItemService.createItemFromClone(Pda);
        
        const peaceKeeper = this.InstanceManager.database.traders[Traders.PEACEKEEPER];

        peaceKeeper.assort.items.push({
            _id: CustomItemIds.Pda,
            _tpl: CustomItemIds.Pda,
            parentId: "hideout",
            slotId: "hideout",
            upd:
            {
                UnlimitedCount: false,
                StackObjectsCount: 1
            }
        });

        peaceKeeper.assort.barter_scheme[CustomItemIds.Pda] = [
            [
                {
                    count: 12600,
                    _tpl: Money.DOLLARS
                }
            ]
        ];
        
        peaceKeeper.assort.loyal_level_items[CustomItemIds.Pda] = 3;

        this.addItemToSpecSlots(CustomItemIds.Pda);
    }

    private addCraftsToDatabase(): void
    {
        const crafts = this.SkillsConfig.LockPicking.CRAFTING_RECIPES;

        for (const craft of crafts)
        {
            this.InstanceManager.database.hideout.production.push(craft)
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