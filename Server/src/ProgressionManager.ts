/* eslint-disable @typescript-eslint/naming-convention */
import fs from 'fs';
import path from "node:path";
import JSON5 from "json5";


import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import { IPmcData } from "@spt/models/eft/common/IPmcData";
import { InstanceManager } from "./InstanceManager";
import { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";
import { MessageType } from "@spt/models/enums/MessageType";
import { IRewardTier, ISkillRewards } from "./Models/IConfig";
import { ILogger } from '@spt/models/spt/utils/ILogger';
import { IProgression } from './Models/IProgression';
import { Item } from "@spt/models/eft/common/tables/IItem";
import { ItemAddedResult } from '@spt/models/enums/ItemAddedResult';

export class ProgressionManager
{
    private InstanceManager: InstanceManager;
    private logger: ILogger;

    private PmcProfile: IPmcData;
    private Progression: IProgression;
    private SkillRewards: ISkillRewards;

    private ProgressPath: string;

    public init(instanceManager: InstanceManager): void
    {
        this.InstanceManager = instanceManager;
        this.logger = instanceManager.logger;

        this.ProgressPath = path.join(path.dirname(__filename), "..", "progression");

        const rewardsRaw = this.InstanceManager.vfs.readFile(path.join(path.dirname(__filename), "..", "config", "SkillRewards.json5"));
        this.SkillRewards = JSON5.parse(rewardsRaw);
    }

    public getActivePmcData(sessionId: string): void
    {
        this.PmcProfile = this.InstanceManager.profileHelper.getPmcProfile(sessionId);

        this.checkForOrCreateProgressFile();
    }

    private checkForOrCreateProgressFile(): void
    {
        const progPath = path.join(this.ProgressPath, `${this.PmcProfile._id}.json`);

        if (!this.InstanceManager.vfs.exists(progPath))
        {
            this.logger.logWithColor(`Skills Extended: Progress file for ${this.PmcProfile.Info.Nickname} does not exist.`, LogTextColor.YELLOW);
            this.logger.logWithColor("Skills Extended: This is normal, creating a new one now.", LogTextColor.YELLOW);

            this.Progression = {
                Id: this.PmcProfile._id,
                PmcName: this.PmcProfile.Info.Nickname,
                Progress: {

                }
            }

            this.saveProgressionFile();

            this.checkForPendingRewards();
            return;
        }

        this.loadProgressionFile();

        this.logger.logWithColor(`Skills Extended: Progress file for ${this.Progression.PmcName} loaded.`, LogTextColor.GREEN);
        
        this.checkForPendingRewards();
    }

    public checkForPendingRewards(): void
    {
        if (this.PmcProfile.Skills.Common === undefined)
        {
            this.logger.logWithColor("Skills Extended: No skills defined on profile, this is normal on new or wiped profiles.", LogTextColor.YELLOW);
            return;
        }

        this.logger.logWithColor(`Skills Extended: Checking for pending rewards for ${this.Progression.PmcName}`, LogTextColor.GREEN);

        const skills = this.PmcProfile.Skills.Common;
        for (const skill of skills)
        {
            if (skill.Progress === 0) continue;

            const tier = Math.floor(this.convertSkillProgressToRewardTier(skill.Progress));
            const hasReward = this.hasRewardForSkillTier(skill.Id, tier);
            const rewardDiff = this.findDifferenceInRewardLevel(skill.Id, tier);

            if (rewardDiff > 0)
            {
                this.sendPendingSkillRewards(skill.Id, tier);
            }

            this.logger.logWithColor(`${skill.Id} : ${skill.Progress / 100} : ${tier} : ${hasReward} : ${rewardDiff}`, LogTextColor.GREEN);
        }

        this.saveProgressionFile();
    }

    private convertSkillProgressToRewardTier(progress: number): number
    {
        return (progress / 100) / 5;
    }

    private hasRewardForSkillTier(skillId: string, tier: number): boolean
    {
        if (this.Progression.Progress[skillId] !== undefined)
        {
            return this.Progression.Progress[skillId] >= tier;
        }

        return false;
    }

    private findDifferenceInRewardLevel(skillId: string, tier: number): number
    {
        if (this.Progression.Progress[skillId] !== undefined)
        {
            return this.Progression.Progress[skillId] - tier;
        }

        return tier;
    }

    private sendPendingSkillRewards(skillId: string, tier: number): void
    {
        // We want to start awarding the tier after the highest recorded or level 1 if undefined
        const startTier = this.Progression.Progress[skillId] !== undefined
            ? this.Progression.Progress[skillId]
            : 0;
        
        // Difference in tiers
        const tierDiff = tier - startTier;

        if (tierDiff <= 0) return;

        this.logger.logWithColor(`Skills Extended: Pending rewards for ${skillId}. ${tierDiff} tiers!`, LogTextColor.GREEN);

        for (let i = startTier; i <= tier; i++)
        {    
            this.sendMailReward(skillId, i);
        }

        this.Progression.Progress[skillId] = tier;
    }

    private selectRewardFromTier(tier: number): Item[]
    {
        const items: Item[] = [];
        const hashUtil = this.InstanceManager.hashUtil;
        let randomRoll = Math.random() * 100;

        const rewards = this.SkillRewards.RewardPool as IRewardTier[];
        const pool = rewards.find(x => x.Tier === tier);

        const alreadyReceived: string[] = [];

        for (const reward in pool.Rewards)
        {
            if (pool.Rewards[reward] < randomRoll) continue;
            if (alreadyReceived.includes(reward)) continue;

            this.logger.logWithColor(`Skills Extended: Generating reward ${reward} at tier ${tier}`, LogTextColor.GREEN);

            const newReward: Item = {
                _id: hashUtil.generate(),
                _tpl: reward
            }

            
            // Roll another number
            randomRoll = Math.random() * 100;
            items.push(newReward);
            alreadyReceived.push(reward);
        }

        return items;
    }

    private sendMailReward(skillId: string, tier: number): void
    {
        const mailService = this.InstanceManager.mailSendService;

        const items = this.selectRewardFromTier(tier);

        if (items.length <= 0) return;

        const message: ISendMessageDetails = {
            recipientId: this.PmcProfile._id,
            sender: MessageType.SYSTEM_MESSAGE,
            messageText: `Here is your reward for tier ${tier} of ${skillId}`,
            items: items,
            itemsMaxStorageLifetimeSeconds: 72000
        }

        mailService.sendMessageToPlayer(message);
    }

    private saveProgressionFile(): void
    {
        const progPath = path.join(this.ProgressPath, `${this.PmcProfile._id}.json`);

        const jsonData = JSON.stringify(this.Progression, null, 2);

        fs.writeFileSync(progPath, jsonData, 'utf8');

        this.logger.logWithColor(`Skills Extended: Progression file for ${this.PmcProfile.Info.Nickname} saved.`, LogTextColor.GREEN);
    }

    private loadProgressionFile(): void
    {
        const progPath = path.join(this.ProgressPath, `${this.PmcProfile._id}.json`);
        const progressionRaw = this.InstanceManager.vfs.readFile(progPath);
        this.Progression = JSON5.parse(progressionRaw);
    }
}