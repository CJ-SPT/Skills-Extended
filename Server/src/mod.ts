/* eslint-disable @typescript-eslint/naming-convention */

import { InstanceManager } from "./InstanceManager";
import * as customWeapons from "../config/CustomWeapons.json";
import * as blankKeyMapping from "../config/KeyBlankMapping.json";

import type { DependencyContainer } from "tsyringe";
import type { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import { LogTextColor } from "@spt-aki/models/spt/logging/LogTextColor";
import type { CustomItemService } from "@spt-aki/services/mod/CustomItemService";
import type { NewItemFromCloneDetails } from "@spt-aki/models/spt/mod/NewItemDetails";
import type { IKeys } from "./Models/IKeys";

enum TraderIDs {
    Mechanic = "5a7c2eca46aef81a7ca2145d",
    Skier = "58330581ace78e27b8b10cee",
    Peacekeeper = "5935c25fb3acc3127c3d8cd9",
    Therapist = "54cb57776803fa99248b456e",
    Prapor = "54cb50c76803fa8b248b4571",
    Jaeger = "5c0647fdd443bc2504c2d371",
    Ragman = "5ac3b934156ae10c4430e83c"
}

enum CurrencyIDs {
    Roubles = "5449016a4bdc2d6f028b456f",
    Euros = "569668774bdc2da2298b4568",
    Dollars = "5696686a4bdc2da3298b456a"
}

class SkillsPlus implements IPreAkiLoadMod, IPostDBLoadMod
{
    private Instance: InstanceManager = new InstanceManager();
    private locale: Record<string, Record<string, string>>; 
    private customItemService: CustomItemService;

    public preAkiLoad(container: DependencyContainer): void 
    {
        this.Instance.preAkiLoad(container, "Skills Extended");
        this.registerRoutes();
    }

    public postDBLoad(container: DependencyContainer): void 
    {
        this.Instance.postDBLoad(container);
        this.customItemService = container.resolve<CustomItemService>("CustomItemService");

        this.setLocales();

        //this.CloneKeysToBlanks();

        this.locale = this.Instance.database.locales.global;
    }

    private setLocales(): void
    {
        this.Instance.database.locales.global.en.FirstAidDescription += "FirstAidDescriptionPattern";
        this.Instance.database.locales.global.en.FieldMedicineDescription = "FieldMedicineDescriptionPattern";
    }

    private loadCustomWeaponsForUsecSkill(): string
    {
        return JSON.stringify(customWeapons.USEC_Rifle_Carbine_Skill);
    }

    private loadCustomWeaponsForBearSkill(): string
    {
        return JSON.stringify(customWeapons.BEAR_Rifle_Carbine_Skill);
    }

    private getKeys(): string
    {
        const items = this.Instance.database.templates.items;

        const keys: IKeys = {
            keyLocale: {},
            keyBlankMapping: {}
        }

        for (const item in items)
        {
            if (items[item]._parent === "5c99f98d86f7745c314214b3" || items[item]._parent === "5c164d2286f774194c5e69fa")
            {
                keys.keyLocale[item] = this.locale.en[`${items[item]._id} Name`];
                this.Instance.logger.logWithColor(this.locale.en[`${items[item]._id} Name`], LogTextColor.GREEN);
            }
        }

        keys.keyBlankMapping = blankKeyMapping;

        return JSON.stringify(keys);
    }

    private registerRoutes(): void
    {
        this.Instance.staticRouter.registerStaticRouter(
            "GetKeys",
            [
                {
                    url: "/skillsExtended/GetKeys",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: (url, info, sessionId, output) => 
                    {                     
                        return this.getKeys();
                    }
                }
            ],
            ""
        );

        this.Instance.staticRouter.registerStaticRouter(
            "GetCustomWeaponsUsec",
            [
                {
                    url: "/skillsExtended/GetCustomWeaponsUsec",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: (url, info, sessionId, output) => 
                    {
                        return this.loadCustomWeaponsForUsecSkill();
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
                        return this.loadCustomWeaponsForBearSkill();
                    }
                }
            ],
            ""
        );
    }

    private CloneKeysToBlanks()
    {
        this.cloneIndustrialKeyToBlank();
    }

    // Clones factory key to be used as a blank for bump lock picking
    private cloneIndustrialKeyToBlank()
    {
        const blankKey: NewItemFromCloneDetails = {
            itemTplToClone: "5448ba0b4bdc2d02308b456c",
            overrideProperties: {
                MaximumNumberOfUsage: 5,
                CanSellOnRagfair: false
            },

            parentId: "543be5e94bdc2df1348b4568",
            newId: "Industrial_Blank_Key",
            fleaPriceRoubles: 120000,
            handbookPriceRoubles: 100000,
            handbookParentId: "5b47574386f77428ca22b342",

            locales: {
                en: {
                    name: "Industrial key blank",
                    shortName: "Industrial Key blank",
                    description: "An industrial blank key used for bump picking locks"
                }
            }
        }

        this.customItemService.createItemFromClone(blankKey);

        const mechanic = this.Instance.database.traders[TraderIDs.Mechanic];

        mechanic.assort.items.push({
            _id: "Industrial_Blank_Key",
            _tpl: "Industrial_Blank_Key",
            parentId: "hideout",
            slotId: "hideout",
            upd:
            {
                UnlimitedCount: false,
                StackObjectsCount: 2
            }
        });

        mechanic.assort.barter_scheme.Industrial_Blank_Key = [
            [
                {
                    count: 100000,
                    _tpl: CurrencyIDs.Roubles
                }
            ]
        ];
        
        mechanic.assort.loyal_level_items.Industrial_Blank_Key = 2;
    }
}

module.exports = { mod: new SkillsPlus() }