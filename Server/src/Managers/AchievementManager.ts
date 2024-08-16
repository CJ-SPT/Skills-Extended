/* eslint-disable @typescript-eslint/naming-convention */
import type { IOManager } from "./IOManager";
import { InstanceManager } from "./InstanceManager";

export class AchievementManager
{
    private InstanceManager: InstanceManager = new InstanceManager(); 
    private IOManager: IOManager;

    private importAchievementData(): void
    {
        const dataPath = path.join(path.dirname(__filename), "..", "db", "data");
        const files = fs.readdirSync(dataPath);
        
        const jsonFiles = files
            .filter(file => path.extname(file) === ".json")
            .map(file => path.basename(file, ".json"));

        const achievmentTable = this.Database.templates.achievements;

        for (const file of jsonFiles)
        {   
            const filePath = path.join(path.dirname(__filename), "..", "db", "data", `${file}.json`);
            const data = this.loadAchievementFile(filePath);

            for (const achievement of data)
            {
                achievmentTable.push(achievement);
            }

            this.InstanceManager.logger.logWithColor(`CJCAL: Loaded: ${data.length} achievements from ${file}.json`, LogTextColor.GREEN);
        }    
    }
}