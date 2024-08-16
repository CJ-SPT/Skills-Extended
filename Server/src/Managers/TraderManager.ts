/* eslint-disable @typescript-eslint/naming-convention */

import path from "node:path";
import type { IOManager } from "./IOManager";
import { InstanceManager } from "./InstanceManager";
import type { ITraderConfig, UpdateTime } from "@spt/models/spt/config/ITraderConfig";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";
import type { ITraderAssort, ITraderBase } from "@spt/models/eft/common/tables/ITrader";
import { Traders } from "@spt/models/enums/Traders";
import type { IRagfairConfig } from "@spt/models/spt/config/IRagfairConfig";

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
        const imageFilepath = path.join(this.IOManager.ImageRootPath, "Trader", "locksmith.jpg");
        
        // Register a route to point to the profile picture - remember to remove the .jpg from it
        imageRouter.addRoute(this.BaseJson.avatar.replace(".jpg", ""), imageFilepath);
    }

    private setTraderUpdateTime(): void
    {
        // Add refresh time in seconds to config
        const traderRefreshRecord: UpdateTime = {
            traderId: this.BaseJson._id,
            seconds: {
                min: 600,
                max: 12000
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
        // Create a blank assort object, ready to have items added
        const assortTable: ITraderAssort = {
            nextResupply: 0,
            items: [],
            barter_scheme: {},
            loyal_level_items: {},
        };

        return assortTable;
    }

    private addTraderToLocales()
    {
        const global = this.InstanceManager.database.locales.global;

        // For each language, add locale for the new trader
        const locales = Object.values(global);

        for (const locale of locales) {
            locale[`${this.BaseJson._id} FullName`] = "The Locksmith";
            locale[`${this.BaseJson._id} FirstName`] = "Locksmith";
            locale[`${this.BaseJson._id} Nickname`] = "Locksmith";
            locale[`${this.BaseJson._id} Location`] = "Unknown";
            locale[`${this.BaseJson._id} Description`] = "Once a distinqused locksmith, The Locksmith now spends his days wondering around war zones carrying out criminal activities.";
        }
    }
}