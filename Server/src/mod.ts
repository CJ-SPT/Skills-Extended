/* eslint-disable @typescript-eslint/naming-convention */
import { InstanceManager } from "./InstanceManager";
import { LogTextColor } from "@spt-aki/models/spt/logging/LogTextColor";

import { DependencyContainer } from "tsyringe";
import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import { IPmcData } from "@spt-aki/models/eft/common/IPmcData";
import { SkillTypes } from "@spt-aki/models/enums/SkillTypes";
import { Common } from "@spt-aki/models/eft/common/tables/IBotBase";
import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
import { BaseClasses } from  "@spt-aki/models/enums/BaseClasses";


class SkillsPlus implements IPreAkiLoadMod, IPostDBLoadMod
{
    private Instance: InstanceManager = new InstanceManager();
    private profile: IPmcData;
    private firstAidSkill: Common;


    private originalMedkitVals: Record<string, number> = {
        "544fb45d4bdc2dee738b4568": 400,    //Salewa
        "5755356824597772cb798962": 100,    //AI-2
        "590c657e86f77412b013051d": 1800,   //Grizzly
        "590c661e86f7741e566b646a": 220,    //Car     
        "590c678286f77426c9660122": 300,    //Ifak
        "5e99711486f7744bfc4af328": 3000,   //Sanitars
        "60098ad7c2240c0fe85c570a": 400     //AFAK
    }

    public preAkiLoad(container: DependencyContainer): void 
    {
        this.Instance.preAkiLoad(container, "Skills Extended");
        this.registerRoutes();
          
    }

    public postDBLoad(container: DependencyContainer): void 
    {
        this.Instance.postDBLoad(container);
        this.setLocales();
    }

    private setLocales(): void
    {
        this.Instance.database.locales.global["en"]["FirstAidDescription"] += "\n\n Increases the speed of healing items by 0.7% per level. \n\n Elite bonus 15% \n\n Increases the HP resource of medical items by 5 per level. \n\n Elite bonus: 10 per level.";
    }

    private registerRoutes(): void
    {
        this.Instance.staticRouter.registerStaticRouter(
            "OnStart",
            [
                {
                    // Route hook for start up initialization
                    url: "/singleplayer/bundles",
                    action: (url, info, sessionId, output) => 
                    {
                        this.profile = this.Instance.profileHelper.getPmcProfile(sessionId);
                        this.firstAidSkill = this.Instance.profileHelper.getSkillFromProfile(this.profile, SkillTypes.FIRST_AID);
                        
                        // Set the HP resource based on level of the first Aid Skill
                        this.setHPResource();
                        return output;
                    }
                }
            ],
            "aki"
        );

        
        this.Instance.staticRouter.registerStaticRouter(
            "OnRaidStart",
            [
                {
                    // Route hook for start up initialization
                    url: "/client/raid/configuration",
                    action: (url, info, sessionId, output) => 
                    {   
                        // Set the HP resource based on level of the first Aid Skill
                        this.setHPResource();
                        return output;
                    }
                }
            ],
            "aki"
        );

        this.Instance.staticRouter.registerStaticRouter(
            "OnRaidEnd",
            [
                {
                    // Route hook for start up initialization
                    url: "/singleplayer/settings/raid/endstate",
                    action: (url, info, sessionId, output) => 
                    {              
                        // Set the HP resource based on level of the first Aid Skill
                        this.setHPResource();
                        return output;
                    }
                }
            ],
            "aki"
        );
        
    }

    private getLevel(progress: number): number
    {
        return Math.floor(progress / 100);
    }

    private getMedkitHPBonus(level: number): number
    {
        if (level <= 50)
        {
            return level * 5;
        }
        else
        {
            return level * 10;
        }
    }

    private setHPResource(): void
    {
        const itemDB = this.Instance.database.templates.items;
        const currentLevel: number = this.getLevel(this.firstAidSkill.Progress);

        this.Instance.logger.logWithColor(`First aid level: ${currentLevel}`, LogTextColor.GREEN);
        this.Instance.logger.logWithColor(`At level ${currentLevel}, the bonus resource is: ${currentLevel * 5} \n`, LogTextColor.GREEN);

        for (const item in itemDB)
        {
            if (itemDB[item]._parent === BaseClasses.MEDKIT)
            {
                const medItem: ITemplateItem = itemDB[item];

                this.Instance.logger.logWithColor(`${this.Instance.database.locales.global["en"][`${itemDB[item]._id} Name`]}`, LogTextColor.GREEN);
                this.Instance.logger.logWithColor(`Original max resource: ${this.originalMedkitVals[item]}`, LogTextColor.GREEN);
                 
                medItem._props.MaxHpResource = this.originalMedkitVals[item] + this.getMedkitHPBonus(currentLevel);
                
                this.Instance.logger.logWithColor(`New max resource: ${medItem._props.MaxHpResource} \n\n`, LogTextColor.GREEN); 

                this.fixProfileItemsOfType(item, medItem._props.MaxHpResource);
            }
        }
    }

    private fixProfileItemsOfType(tpl: string, maxHpResource: number): void 
    {
        const profileItems = this.profile.Inventory.items;
        const currentLevel: number = this.getLevel(this.firstAidSkill.Progress);

        for (const item of profileItems)
        {
            if (item._tpl === tpl)
            {
                // Only change items that have not been changed.
                if (item?.upd?.MedKit?.HpResource === maxHpResource - this.getMedkitHPBonus(currentLevel))
                {
                    item.upd.MedKit.HpResource = maxHpResource;
                    this.Instance.logger.logWithColor(`Fixed ${this.Instance.database.locales.global["en"][`${item._tpl} Name`]} in profile \n`, LogTextColor.GREEN);
                }
            }
        }
    }
}

module.exports = { mod: new SkillsPlus() }