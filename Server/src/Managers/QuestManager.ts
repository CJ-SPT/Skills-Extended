/* eslint-disable @typescript-eslint/naming-convention */
import fs from "fs";
import path from "node:path";

import type { InstanceManager } from "./InstanceManager";
import type { IOManager } from "./IOManager";
import type { IQuest } from "@spt/models/eft/common/tables/IQuest";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

export class QuestManager
{
    private InstanceManager: InstanceManager;
    private IOManager: IOManager;

    public postDbLoad(instanceManager: InstanceManager, ioManager: IOManager): void
    {
        this.InstanceManager = instanceManager;
        this.IOManager = ioManager;

        this.importQuests();
    }

    private importQuests(): void
    {
        const dataPath = this.IOManager.QuestsRootPath;
        const files = fs.readdirSync(dataPath);
        
        const jsonFiles = files
            .filter(file => path.extname(file) === ".json")
            .map(file => path.basename(file, ".json"));

        const questTable = this.InstanceManager.database.templates.quests;

        let loadedQuests = 0;

        for (const file of jsonFiles)
        {   
            const filePath = path.resolve(dataPath, `${file}.json`);
            const data = this.IOManager.loadJsonFile<IQuest[]>(filePath);

            for (const quest of data)
            {
                questTable[quest._id] = quest;
            }

            loadedQuests += data.length;
        }

        this.InstanceManager.logger
            .logWithColor(`Skills Extended: Loaded ${loadedQuests} quests`, LogTextColor.GREEN);
    }
}