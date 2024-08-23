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
    public DataPath: string = path.join(path.dirname(__filename), "..", "..", "data");
    public ConfigPath: string = path.join(path.dirname(__filename), "..", "..", "config");
    public ProgressPath: string = path.join(path.dirname(__filename), "..", "..", "progression");

    public AchievementsRootPath: string = path.join(path.dirname(__filename), "..", "..", "data", "Achievements");
    public AssortRootPath: string = path.join(path.dirname(__filename), "..", "..", "data", "Assort");
    public CustomQuestConditions: string = path.join(path.dirname(__filename), "..", "..", "data", "CustomQuestConditions");
    public LocaleRootPath: string = path.join(path.dirname(__filename), "..", "..", "data", "Locales");
    public TraderRootPath: string = path.join(path.dirname(__filename), "..", "..", "data", "Trader");
    public QuestsRootPath: string = path.join(path.dirname(__filename), "..", "..", "data", "Quests");
    public ImageRootPath: string = path.join(path.dirname(__filename), "..", "..", "data", "Images");
    public ItemRootPath: string = path.join(path.dirname(__filename), "..", "..", "data", "Items");

    public importData(): void
    {
        const confPath = path.join(this.ConfigPath, "ServerConfig.json");
        this.ServerConfig = this.loadJsonFile<IServerConfig>(confPath);

        this.importAllLocaleData();
        this.importAllImages();

        //this.extractLocalesToSingleFile();
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

    private extractLocalesToSingleFile(): void
    {
        const enPath = path.join(this.LocaleRootPath, "fr");
        const localeFiles = fs.readdirSync(enPath);

        const locales: Record<string, string> = {};

        for (const file of localeFiles)
        {
            const localeFile = path.join(enPath, file);

            const tmpLocales = this.loadJsonFile<Record<string, string>>(localeFile);

            for (const locale in tmpLocales)
            {
                locales[locale] = tmpLocales[locale];
            }
        }

        const outPath = path.join(this.RootPath, "fr.json");
        const data = JSON.stringify(locales);

        this.InstanceManager.logger.logWithColor(outPath, LogTextColor.BLUE);

        fs.writeFileSync(outPath, data, "utf8");

        
        this.InstanceManager.logger.logWithColor(`Skills Debug: Exported ${locales.length} entries for translation.`, LogTextColor.BLUE);
    }

    private cleanUpLocaleDirectory(): void
    {
        
    }
}