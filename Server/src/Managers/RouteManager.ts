/* eslint-disable @typescript-eslint/naming-convention */

import { BaseClasses } from "@spt/models/enums/BaseClasses";
import { InstanceManager } from "./InstanceManager";
import { ProgressionManager } from "./ProgressionManager";
import { IKeys } from "../Models/IKeys";

export class RouteManager
{
    private InstanceManager: InstanceManager;
    private ProgressionManager: ProgressionManager;
    private SkillsConfigRaw: any;

    public preSptLoad(
        instanceManager: InstanceManager,
        progressionManager: ProgressionManager,
        skillsConfigRaw: any
    ): void
    {
        this.InstanceManager = instanceManager;
        this.ProgressionManager = progressionManager;
        this.SkillsConfigRaw = skillsConfigRaw;

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
                        return this.SkillsConfigRaw;
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