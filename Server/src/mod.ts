/* eslint-disable @typescript-eslint/naming-convention */
import { InstanceManager } from "./InstanceManager";

import { DependencyContainer } from "tsyringe";
import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";

class SkillsPlus implements IPreAkiLoadMod, IPostDBLoadMod
{
    private Instance: InstanceManager = new InstanceManager();

    public preAkiLoad(container: DependencyContainer): void 
    {
        this.Instance.preAkiLoad(container, "Skills Extended");
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
}

module.exports = { mod: new SkillsPlus() }