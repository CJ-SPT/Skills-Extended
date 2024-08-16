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
    

    public ConfigPath: string = path.join(path.dirname(__filename), "..", "..", "config");
    public ProgressPath: string = path.join(path.dirname(__filename), "..", "..", "progression");
    public LocaleRootPath: string = path.join(path.dirname(__filename), "..", "..", "locale");

    
    /**
     * Loads and parses a config file from disk
     * @param fileName File name inside of config folder to load
     */
    public LoadConfigFile<T>(fileName: string): T
    {
        const file = path.join(this.ConfigPath, fileName);
        const rewardsRaw = this.InstanceManager.vfs.readFile(file);

        return JSON5.parse(rewardsRaw) as T;
    }

    /**
     * Loads and parses a config file from disk
     * @param fileName File name inside of config folder to load
     */
    public LoadProgressionFile(fileName: string): any
    {
        const file = path.join(this.ProgressPath, fileName);
        const progress = this.InstanceManager.vfs.readFile(file);

        return JSON5.parse(progress);
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