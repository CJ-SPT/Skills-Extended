/* eslint-disable @typescript-eslint/naming-convention */

import fs from "fs";
import path from "node:path";
import JSON5 from "json5";

import type { InstanceManager } from "./InstanceManager";
import type { IProgression } from "../Models/IProgression";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

export class IOManager 
{
    constructor(instanceManager: InstanceManager)
    {
        this.InstanceManager = instanceManager;
    }

    private InstanceManager: InstanceManager;

    public DataPath: string = path.join(path.dirname(__filename), "..", "..", "Data");
    public ConfigPath: string = path.join(path.dirname(__filename), "..", "..", "config");
    public ProgressPath: string = path.join(path.dirname(__filename), "..", "..", "progression");

    public AchievementsRootPath: string = path.join(path.dirname(__filename), "..", "..", "Data", "Achievements");
    public AssortRootPath: string = path.join(path.dirname(__filename), "..", "..", "Data", "Assort");
    public LocaleRootPath: string = path.join(path.dirname(__filename), "..", "..", "Data", "Locales");
    public TraderRootPath: string = path.join(path.dirname(__filename), "..", "..", "Data", "Trader");
    public QuestsRootPath: string = path.join(path.dirname(__filename), "..", "..", "Data", "Quests");
    public ImageRootPath: string = path.join(path.dirname(__filename), "..", "..", "Data", "Images");

    /**
     * Loads and parses a config file from disk
     * @param fileName File name inside of config folder to load
     */
    public loadJsonFile<T>(filePath: string): T
    {
        const file = path.join(filePath);
        const rewardsRaw = this.InstanceManager.vfs.readFile(file);

        return JSON5.parse(rewardsRaw) as T;
    }

    public saveProgressionFile(progressFile: IProgression, fileName: string): void
    {
        const progPath = path.join(this.ProgressPath, fileName);

        const jsonData = JSON.stringify(progressFile, null, 2);

        fs.writeFileSync(progPath, jsonData, "utf8");

        this.InstanceManager.logger
            .logWithColor(
                `Skills Extended: Progression file for ${progressFile.PmcName} saved.`, 
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
}