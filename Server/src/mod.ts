/* eslint-disable @typescript-eslint/naming-convention */
import { InstanceManager } from "./InstanceManager";
import * as data from "../config/CustomWeapons.json";

import { DependencyContainer } from "tsyringe";
import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import { LogTextColor } from "@spt-aki/models/spt/logging/LogTextColor";

class SkillsPlus implements IPreAkiLoadMod, IPostDBLoadMod
{
    private Instance: InstanceManager = new InstanceManager();

    public preAkiLoad(container: DependencyContainer): void 
    {
        this.Instance.preAkiLoad(container, "Skills Extended");
        this.registerGetCustomWeaponRoutes();
    }

    public postDBLoad(container: DependencyContainer): void 
    {
        this.Instance.postDBLoad(container);
        this.setLocales();
    }

    private setLocales(): void
    {
        this.Instance.database.locales.global["en"]["FirstAidDescription"] += "FirstAidDescriptionPattern";
        this.Instance.database.locales.global["en"]["FieldMedicineDescription"] = "FieldMedicineDescriptionPattern";
    }

    private loadCustomWeaponsForUsecSkill(): string
    {
        return JSON.stringify(data.USEC_Rifle_Carbine_Skill);
    }

    private loadCustomWeaponsForBearSkill(): string
    {
        return JSON.stringify(data.BEAR_Rifle_Carbine_Skill);
    }

    private registerGetCustomWeaponRoutes(): void
    {
        this.Instance.staticRouter.registerStaticRouter(
            "GetCustomWeaponsUsec",
            [
                {
                    url: "/skillsExtended/GetCustomWeaponsUsec",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: (url, info, sessionId, output) => 
                    {
                        const json = this.loadCustomWeaponsForUsecSkill();
                        this.Instance.logger.log(`[${this.Instance.modName}] Custom USEC weapons requested by client`, LogTextColor.GREEN);
                        return json;
                    }
                }
            ],
            ""
        );

        this.Instance.staticRouter.registerStaticRouter(
            "GetCustomWeaponsBear",
            [
                {
                    url: "/skillsExtended/GetCustomWeaponsBear",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: (url, info, sessionId, output) => 
                    {
                        const json = this.loadCustomWeaponsForBearSkill();
                        this.Instance.logger.log(`[${this.Instance.modName}] Custom BEAR weapons requested by client`, LogTextColor.GREEN);
                        return json;
                    }
                }
            ],
            ""
        );
    }
}

module.exports = { mod: new SkillsPlus() }