/* eslint-disable @typescript-eslint/naming-convention */

import { InstanceManager } from "./InstanceManager";
import * as SkillsConfig from "../config/SkillsConfig.json";

import type { DependencyContainer } from "tsyringe";
import type { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
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

        this.CloneKeysToBlanks();

        this.locale = this.Instance.database.locales.global;
    }

    private setLocales(): void
    {
        this.Instance.database.locales.global.en.FirstAidDescription += "FirstAidDescriptionPattern";
        this.Instance.database.locales.global.en.FieldMedicineDescription = "FieldMedicineDescriptionPattern";
    }

    private getKeys(): string
    {
        const items = this.Instance.database.templates.items;

        const keys: IKeys = {
            keyLocale: {}
        }

        for (const item in items)
        {
            if (items[item]._parent === "5c99f98d86f7745c314214b3" 
             || items[item]._parent === "5c164d2286f774194c5e69fa"
             || items[item]._parent === "543be5e94bdc2df1348b4568"
             || items[item]._parent === "5c164d2286f774194c5e69fa")
            {
                keys.keyLocale[item] = this.locale.en[`${items[item]._id} Name`];
            }
        }

        return JSON.stringify(keys);
    }

    private registerRoutes(): void
    {
        this.Instance.staticRouter.registerStaticRouter(
            "GetSkillsConfig",
            [
                {
                    url: "/skillsExtended/GetSkillsConfig",
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    action: (url, info, sessionId, output) => 
                    {                     
                        return JSON.stringify(SkillsConfig);
                    }
                }
            ],
            ""
        );

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
                CanSellOnRagfair: false,
                MaximumNumberOfUsage: 5
            },

            parentId: "5c99f98d86f7745c314214b3",
            newId: "LOCKPICK_SET",
            fleaPriceRoubles: 120000,
            handbookPriceRoubles: 25000,
            handbookParentId: "5c518ec986f7743b68682ce2",

            locales: {
                en: {
                    name: "Lockpick set",
                    shortName: "lockpick",
                    description: "A set of tools used for picking locks"
                }
            }
        }

        this.customItemService.createItemFromClone(blankKey);

        const mechanic = this.Instance.database.traders[TraderIDs.Mechanic];

        mechanic.assort.items.push({
            _id: "LOCKPICK_SET",
            _tpl: "LOCKPICK_SET",
            parentId: "hideout",
            slotId: "hideout",
            upd:
            {
                UnlimitedCount: false,
                StackObjectsCount: 200
            }
        });

        mechanic.assort.barter_scheme["LOCKPICK_SET"] = [
            [
                {
                    count: 100000,
                    _tpl: CurrencyIDs.Roubles
                }
            ]
        ];
        
        mechanic.assort.loyal_level_items["LOCKPICK_SET"] = 1;
    }
}

module.exports = { mod: new SkillsPlus() }