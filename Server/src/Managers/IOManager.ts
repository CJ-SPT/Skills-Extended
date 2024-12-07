/* eslint-disable @typescript-eslint/naming-convention */

import fs from "fs";
import path from "node:path";
import JSON5 from "json5";

import type { InstanceManager } from "./InstanceManager";
import type { IProgression } from "../Models/IProgression";
import type { IServerConfig } from "../Models/IServerConfig";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

export class IOManager 
{
    constructor(instanceManager: InstanceManager)
    {
        this.InstanceManager = instanceManager;
    }

    private InstanceManager: InstanceManager;
    public ServerConfig: IServerConfig;

    public RootPath: string = path.join(path.dirname(__filename), "..", "..");
    public DataPath: string = path.join(this.RootPath, "data");
    public ConfigPath: string = path.join(this.RootPath, "config");
    public ProgressPath: string = path.join(this.RootPath, "progression");

    public AchievementsRootPath: string = path.join(this.DataPath, "Achievements");
    public AssortRootPath: string = path.join(this.DataPath, "Assort");
    public CustomQuestConditions: string = path.join(this.DataPath, "CustomQuestConditions");
    public LocaleRootPath: string = path.join(this.DataPath, "Locales");
    public TraderRootPath: string = path.join(this.DataPath, "Trader");
    public QuestsRootPath: string = path.join(this.DataPath, "Quests");
    public ImageRootPath: string = path.join(this.DataPath, "Images");
    public ItemRootPath: string = path.join(this.DataPath, "Items");

    public preSptLoad(): void
    {
        const confPath = path.join(this.ConfigPath, "ServerConfig.json");
        this.ServerConfig = this.loadJsonFile<IServerConfig>(confPath);
    }

    public importData(): void
    {
        this.importAllLocaleData();
        this.importAllImages();
    }

    /**
     * Loads and parses a config file from disk
     * @param fileName File name inside of config folder to load
     */
    public loadJsonFile<T>(filePath: string, readAsText = false): T
    {
        const file = path.join(filePath);
        const string = this.InstanceManager.vfs.readFile(file);

        return readAsText 
            ? string as T
            : JSON5.parse(string) as T;
    }

    public saveProgressionFile(progressFile: IProgression, fileName: string): void
    {
        const progPath = path.join(this.ProgressPath, fileName);

        const jsonData = JSON.stringify(progressFile, null, 2);

        fs.writeFileSync(progPath, jsonData, "utf8");

        this.InstanceManager.logger
            .logWithColor(
                `Skills Extended: Progression file for ${progressFile.Id} saved.`, 
                LogTextColor.GREEN);
    }

    /**
     * Loads a locale file from disk
     */
    public loadLocaleFile(filePath: string): Record<string, string> 
    {
        try 
        {
            const data = fs.readFileSync(filePath, "utf-8");
            const jsonData = JSON5.parse(data) as Record<string, string>; // Cast to desired type
            return jsonData;
        } 
        catch (error) 
        {
            throw new Error("Failed to load dictionary");
        }
    }

    private importAllLocaleData(): void
    {
        const localesPath = this.LocaleRootPath;
        const subDirs = fs.readdirSync(localesPath);

        for (const lang of subDirs)
        {
            const langDir = path.join(localesPath, lang);
            const localeFiles = fs.readdirSync(langDir);

            const logger = this.InstanceManager.logger;

            let entries = 0;

            for (const file of localeFiles)
            {
                const localeData = this.loadJsonFile<Record<string, string>>(path.join(langDir, file));
      
                entries += this.importLocaleData(lang, localeData);
            }

            if (entries === 0) continue;

            logger.logWithColor(`Skills Extended: Loaded ${entries} locale entries for locale '${lang}'`, LogTextColor.GREEN);
        }

        this.importMissingLocalesAsEnglish();
    }

    private importLocaleData(lang: string, localeData: Record<string, string>): number
    {
        const globals = this.InstanceManager.database.locales.global;

        for (const entry in localeData)
        {
            globals[lang][entry] = localeData[entry];
        }

        return Object.keys(localeData).length;
    }

    private importMissingLocalesAsEnglish(): void
    {
        const globals = this.InstanceManager.database.locales.global;
        const logger = this.InstanceManager.logger;

        let count = 0;

        for (const entry in globals.en)
        {
            for (const lang in globals)
            {
                if (globals[lang][entry] === undefined)
                {
                    globals[lang][entry] = globals.en[entry];
                    count++;
                }
            }  
        }

        logger.logWithColor(`Skills Extended: Defaulted ${count} locale entries to english across ${Object.keys(globals).length} languages.`, LogTextColor.YELLOW);
        logger.logWithColor("Skills Extended: If you would like to provide translations, please reach out on the mod page.", LogTextColor.YELLOW);
    }

    private importAllImages(): void
    {
        const imageRouter = this.InstanceManager.imageRouter;
        const logger = this.InstanceManager.logger;

        const directories = [
            path.join(this.ImageRootPath, "Achievements"),
            path.join(this.ImageRootPath, "Quests"),
            path.join(this.ImageRootPath, "Trader")
        ];

        let images = 0;

        for (const directory of directories)
        {
            const files = fs.readdirSync(directory);

            for (const image of files)
            {
                const imagePath = path.join(directory, image);
                const filenameWithoutExtension = path.basename(imagePath, path.extname(imagePath));

                if (imagePath.includes("Trader"))
                {
                    imageRouter.addRoute(`/files/trader/avatar/${filenameWithoutExtension}`, imagePath);
                    images++;
                    continue;
                }

                if (imagePath.includes("Achivements"))
                {
                    imageRouter.addRoute(`/files/achievement/${filenameWithoutExtension}`, imagePath);
                    images++;
                    continue;
                }

                if (imagePath.includes("Quests"))
                {
                    imageRouter.addRoute(`/files/quest/icon/${filenameWithoutExtension}`, imagePath);
                    images++;
                }
            }
        }

        logger.logWithColor(`Skills Extended: Loaded ${images} images`, LogTextColor.GREEN);
    }
}