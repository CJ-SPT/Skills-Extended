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
            .logWithColor(`Skills Extended: Loaded ${loadedAchievements} achievements`, LogTextColor.GREEN);
    }
}