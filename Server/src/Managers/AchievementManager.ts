/* eslint-disable @typescript-eslint/naming-convention */
import fs from "fs";
import path from "node:path";

import type { IOManager } from "./IOManager";
import type { InstanceManager } from "./InstanceManager";
import type { IAchievement } from "@spt/models/eft/common/tables/IAchievement";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

export class AchievementManager
{
    private InstanceManager: InstanceManager;
    private IOManager: IOManager;

    public postDbLoad(instanceManager: InstanceManager, ioManager: IOManager): void
    {
        this.InstanceManager = instanceManager;
        this.IOManager = ioManager;

        this.importAchievementData();
        this.loadAchievementLocales();
        this.loadImages();
        // this.validateData();
    }

    private importAchievementData(): void
    {
        const dataPath = this.IOManager.AchievementsRootPath;
        const files = fs.readdirSync(dataPath);
        
        const jsonFiles = files
            .filter(file => path.extname(file) === ".json")
            .map(file => path.basename(file, ".json"));

        const achievmentTable = this.InstanceManager.database.templates.achievements;

        let loadedAchievements = 0;

        for (const file of jsonFiles)
        {   
            const filePath = path.resolve(dataPath, `${file}.json`);
            const data = this.IOManager.loadJsonFile<IAchievement[]>(filePath);

            for (const achievement of data)
            {
                achievmentTable.push(achievement);
            }

            loadedAchievements += data.length;
        }   
        
        this.InstanceManager.logger
            .logWithColor(`Skills Extended: Loaded: ${loadedAchievements} achievements`, LogTextColor.GREEN);
    }

    private loadImages(): void
    {
        const imagesPath = path.join(this.IOManager.DataPath, "Images", "Achievements");
        const images = fs.readdirSync(imagesPath);

        const imageRouter = this.InstanceManager.imageRouter;

        for (const image of images)
        {
            const imagePath = path.join(imagesPath, image);
            const filenameWithoutExtension = path.basename(imagePath, path.extname(imagePath));
            
            imageRouter.addRoute(`/files/achievement/${filenameWithoutExtension}`, imagePath);
        }
    }

    private loadAchievementLocales(): void
    {
        const localesPath = path.join(this.IOManager.LocaleRootPath, "Achievements");
        const subDirs = fs.readdirSync(localesPath);

        for (const lang of subDirs)
        {
            const langDir = path.join(localesPath, lang);
            const localeFiles = fs.readdirSync(langDir);

            const logger = this.InstanceManager.logger;

            let entries = 0;

            for (const file of localeFiles)
            {
                const localeData = this.IOManager.loadJsonFile<Record<string, string>>(path.join(langDir, file));
      
                entries += this.importLocaleData(lang, localeData);
            }

            if (entries === 0) continue;

            logger.logWithColor(`Skills Extended: Loaded ${entries} achievement locales for lang '${lang}'`, LogTextColor.GREEN);
        }
    }

    private importLocaleData(lang: string, localeData: Record<string, string>): number
    {
        const globalLocales = this.InstanceManager.database.locales.global;

        for (const entry in localeData)
        {
            globalLocales[lang][entry] = localeData[entry];
        }

        return Object.keys(localeData).length;
    }

    private validateData(): void
    {
        const globalLocales = this.InstanceManager.database.locales.global;
        const achievmentTable = this.InstanceManager.database.templates.achievements;
        const logger = this.InstanceManager.logger;

        for (const achievement of achievmentTable)
        {
            if (globalLocales.en[`${achievement.id} Name`] === undefined)
            {
                logger.logWithColor(`Achievement: ${achievement.id} is missing locale data.`, LogTextColor.YELLOW);
            }
        }
    }
}