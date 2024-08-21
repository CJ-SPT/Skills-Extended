/* eslint-disable @typescript-eslint/naming-convention */

import { BaseClasses } from "@spt/models/enums/BaseClasses";
import type { InstanceManager } from "./InstanceManager";
import type { ProgressionManager } from "./ProgressionManager";
import type { IKeys } from "../Models/IKeys";
import type { IAdditionalWeapons, ISkillsConfig } from "../Models/ISkillsConfig";
import type { IOManager } from "./IOManager";

import path from "node:path";
import { ICustomQuestCondition } from "../Models/ICustomQuestConditions";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

export class RouteManager
{
    private InstanceManager: InstanceManager;
    private ProgressionManager: ProgressionManager;
    private SkillsConfig: ISkillsConfig;
    private IOManager: IOManager;

    public preSptLoad(
        instanceManager: InstanceManager,
        progressionManager: ProgressionManager,
        skillsConfig: ISkillsConfig,
        ioManager: IOManager
    ): void
    {
        this.InstanceManager = instanceManager;
        this.ProgressionManager = progressionManager;
        this.SkillsConfig = skillsConfig;
        this.IOManager = ioManager;

        this.registerRoutes();   
    }

    private registerRoutes(): void
    {
        const staticRouter = this.InstanceManager.staticRouter;

        staticRouter.registerStaticRouter(
            "GetSkillsConfig",
            [
                {
                    url: "/skillsExtended/GetSkillsConfig", 
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: async (url, info, sessionId, output) => 
                    {                     
                        this.ProgressionManager.getActivePmcData(sessionId);

                        const addWeaponsPath = path.join(this.IOManager.ConfigPath, "AdditionalWeapons.json");

                        if (this.InstanceManager.vfs.exists(addWeaponsPath))
                        {
                            const weapons = this.IOManager.loadJsonFile<IAdditionalWeapons>(addWeaponsPath);

                            this.SkillsConfig.EasternRifle.WEAPONS.push(...weapons.AdditionalEasternWeapons);
                            this.SkillsConfig.NatoRifle.WEAPONS.push(...weapons.AdditionalNatoWeapons);
                            
                            const logger = this.InstanceManager.logger;
                            logger.logWithColor("Skills Extended: Found and loaded additional weapons", LogTextColor.GREEN);
                        }

                        return JSON.stringify(this.SkillsConfig, null, 2);
                    }
                }
            ],
            ""
        );

        staticRouter.registerStaticRouter(
            "GetKeys",
            [
                {
                    url: "/skillsExtended/GetKeys",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: async (url, info, sessionId, output) => 
                    {                     
                        return this.getKeys();
                    }
                }
            ],
            ""
        );

        staticRouter.registerStaticRouter(
            "GetCustomQuestConditions",
            [
                {
                    url: "/skillsExtended/GetCustomQuestConditions",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: async (url, info, sessionId, output) => 
                    {           
                        const filePath = path.join(this.IOManager.CustomQuestConditions, "CustomQuestConditions.json");
                        return this.IOManager.loadJsonFile<string>(filePath, true);
                    }
                }
            ],
            ""
        );

        staticRouter.registerStaticRouter(
            "end",
            [
                {
                    url: "/client/match/offline/end",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: async (url, info, sessionId, output) => 
                    {                     
                        this.ProgressionManager.checkForPendingRewards();
                        return output;
                    }
                }
            ],
            ""
        );

        staticRouter.registerStaticRouter(
            "select",
            [
                {
                    url: "/client/game/profile/select",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: async (url, info, sessionId, output) => 
                    {                     
                        this.ProgressionManager.checkForPendingRewards();
                        return output;
                    }
                }
            ],
            ""
        );

        staticRouter.registerStaticRouter(
            "wipe",
            [
                {
                    url: "/launcher/profile/change/wipe",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: async (url, info, sessionId, output) => 
                    {                     
                        this.ProgressionManager.wipeProgressFile(sessionId);
                        return output;
                    }
                }
            ],
            ""
        );
    }

    private getKeys(): string
    {
        const items = Object.values(this.InstanceManager.database.templates.items);
        const locale = this.InstanceManager.database.locales.global["en"];

        const keys: IKeys = {
            keyLocale: {}
        }

        const itemHelper = this.InstanceManager.itemHelper;
        
        const keyItems = items.filter(x => 
            x._type === "Item" 
            && itemHelper.isOfBaseclasses(x._id, [BaseClasses.KEY, BaseClasses.KEY_MECHANICAL, BaseClasses.KEYCARD]))
            
        for (const item of keyItems)
        {
            keys.keyLocale[item._id] = locale[`${item._id} Name`];
        }

        return JSON.stringify(keys);
    }
}