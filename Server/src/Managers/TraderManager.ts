/* eslint-disable @typescript-eslint/naming-convention */

import type { IOManager } from "./IOManager";
import type { ITraderConfig, UpdateTime } from "@spt/models/spt/config/ITraderConfig";
import type { IBarterScheme, ITraderAssort, ITraderBase } from "@spt/models/eft/common/tables/ITrader";
import type { IRagfairConfig } from "@spt/models/spt/config/IRagfairConfig";
import type { Item } from "@spt/models/eft/common/tables/IItem";

import path from "node:path";
import { InstanceManager } from "./InstanceManager";
import { Traders } from "@spt/models/enums/Traders";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";

export class TraderManager
{
    private InstanceManager: InstanceManager = new InstanceManager(); 
    private IOManager: IOManager;

    private TraderConfig: ITraderConfig;
    private RagfairConfig: IRagfairConfig;

    private BaseJson: ITraderBase;

    public preSptLoad(instanceManager: InstanceManager, iOManager: IOManager): void
    {
        this.InstanceManager = instanceManager;
        this.IOManager = iOManager;

        this.TraderConfig = instanceManager.configServer.getConfig<ITraderConfig>(ConfigTypes.TRADER);
        this.RagfairConfig = instanceManager.configServer.getConfig<IRagfairConfig>(ConfigTypes.RAGFAIR);

        this.BaseJson = this.IOManager.loadJsonFile<ITraderBase>(path.join(this.IOManager.TraderRootPath, "base.json"));

        // Add to traders enum and enable on ragfair
        Traders[this.BaseJson._id] = this.BaseJson._id;
        this.RagfairConfig.traders[this.BaseJson._id] = true;

        this.registerProfileImage();
        this.setTraderUpdateTime();
    }

    public postDbLoad(): void
    {
        this.addTraderToDb();
        this.addTraderToLocales();
    }

    private registerProfileImage(): void
    {
        const imageRouter = this.InstanceManager.imageRouter;

        // Reference the mod "res" folder
        const imageFilepath = path.join(this.IOManager.ImageRootPath, "Trader", "66bf1f65e1f3b83ea069a271.jpg");
        
        // Register a route to point to the profile picture - remember to remove the .jpg from it
        imageRouter.addRoute(this.BaseJson.avatar.replace(".jpg", ""), imageFilepath);
    }

    private setTraderUpdateTime(): void
    {
        // Add refresh time in seconds to config
        const traderRefreshRecord: UpdateTime = {
            traderId: this.BaseJson._id,
            seconds: {
                min: 1800,
                max: 3600
            },
        };

        this.TraderConfig.updateTime.push(traderRefreshRecord);
    }

    private addTraderToDb(): void
    {
        const database = this.InstanceManager.database;

        // Add trader to trader table, key is the traders id
        database.traders[this.BaseJson._id] = {
            assort: this.createAssortTable(),
            base: this.BaseJson,
            questassort: {
                started: {},
                success: {},
                fail: {}
            }, // questassort is empty as trader has no assorts unlocked by quests
        };
    }

    /**
     * Create basic data for trader + add empty assorts table for trader
     * @returns ITraderAssort
     */
    private createAssortTable(): ITraderAssort
    {
        const ioMgr = this.IOManager;
        const items = ioMgr.loadJsonFile<Item[]>(path.join(ioMgr.AssortRootPath, "Items.json"));
        const barterScheme = ioMgr.loadJsonFile<Record<string, IBarterScheme[][]>>(path.join(ioMgr.AssortRootPath, "BarterScheme.json"));
        const loyalLevelItems = ioMgr.loadJsonFile<Record<string, number>>(path.join(ioMgr.AssortRootPath, "LoyalLevelItems.json"));

        // Create a blank assort object, ready to have items added
        const assortTable: ITraderAssort = {
            nextResupply: 0,
            items: items,
            barter_scheme: barterScheme,
            loyal_level_items: loyalLevelItems
        };

        return assortTable;
    }

    private addTraderToLocales()
    {
        const global = this.InstanceManager.database.locales.global;

        // For each language, add locale for the new trader
        const locales = Object.values(global);

        for (const locale of locales) {
            locale[`${this.BaseJson._id} FullName`] = "Scholars Society";
            locale[`${this.BaseJson._id} FirstName`] = "Scholars Society";
            locale[`${this.BaseJson._id} Nickname`] = "Scholars Society";
            locale[`${this.BaseJson._id} Location`] = "Undisclosed";
            locale[`${this.BaseJson._id} Description`] = "From the outside, Scholars Society appears to be a haven for bookworms, its shelves lined with an eclectic collection of titles. However, beneath the facade lies a secret: a discreet operation dealing in high-quality weaponry for high-end mercenaries.";
        }
    }
}