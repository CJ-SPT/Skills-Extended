/* eslint-disable @typescript-eslint/naming-convention */
import fs from "fs";
import path from "node:path";
import JSON5 from "json5";

import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import { IPmcData } from "@spt/models/eft/common/IPmcData";
import { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";
import { MessageType } from "@spt/models/enums/MessageType";
import { IRewardTier, ISkillRewards } from "../Models/IConfig";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { Item } from "@spt/models/eft/common/tables/IItem";
import { BaseClasses } from "@spt/models/enums/BaseClasses";

import { InstanceManager } from "./InstanceManager";
import { IOManager } from "./IOManager";
import { IProgression } from "../Models/IProgression";

export class ProgressionManager
{
    private InstanceManager: InstanceManager;
    private IOManager: IOManager;
    private logger: ILogger;

    private PmcProfile: IPmcData;
    private Progression: IProgression;
    private SkillRewards: ISkillRewards;

    public init(instanceManager: InstanceManager, ioManager: IOManager): void
    {
        this.InstanceManager = instanceManager;
        this.IOManager = ioManager;
        this.logger = instanceManager.logger;

        this.SkillRewards = this.IOManager.LoadConfigFile("SkillRewards.json5");

        this.debugTestGeneration();
    }

    public getActivePmcData(sessionId: string): void
    {
        if (!this.SkillRewards.ProgressionEnabled) return;

        this.PmcProfile = this.InstanceManager.profileHelper.getPmcProfile(sessionId);

        this.checkForOrCreateProgressFile();
    }

    public wipeProgressFile(sessionId: string): void
    {
        if (!this.SkillRewards.ProgressionEnabled) return;

        this.PmcProfile = this.InstanceManager.profileHelper.getPmcProfile(sessionId);

        if (!this.checkForOrCreateProgressFile())
        {
            this.Progression.Progress = {};
            this.logger.logWithColor(`Skills Extended: Progress file for ${this.PmcProfile.Info.Nickname} wiped.`, LogTextColor.YELLOW);
            this.IOManager.saveProgressionFile(this.Progression, `${this.PmcProfile._id}.json`);
        }
    }

    private debugTestGeneration(): void
    {
        if (this.SkillRewards.Debug.Enabled && this.SkillRewards.Debug.TestGeneration)
        {
            const runs = this.SkillRewards.Debug.NumberOfRuns;
            const level = this.SkillRewards.Debug.GenerationLevel;

            for (let i = 0; i < runs; i++)
            {
                this.generateReward(level, true);
            }
        }
    }

    private checkForOrCreateProgressFile(): boolean
    {
        const progPath = path.join(this.IOManager.ProgressPath, `${this.PmcProfile._id}.json`);

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

            this.IOManager.saveProgressionFile(this.Progression, `${this.PmcProfile._id}.json`);
            return true;
        }

        this.Progression = this.IOManager.LoadProgressionFile(`${this.PmcProfile._id}.json`);

        this.logger.logWithColor(`Skills Extended: Progress file for ${this.Progression.PmcName} loaded.`, LogTextColor.GREEN);
        return false;
    }

    public checkForPendingRewards(): void
    {
        if (!this.SkillRewards.ProgressionEnabled) return;

        if (this.PmcProfile.Skills.Common === undefined)
        {
            this.logger.logWithColor("Skills Extended: No skills defined on profile, this is normal on new or wiped profiles.", LogTextColor.YELLOW);
            return;
        }

        this.logger.logWithColor(`Skills Extended: Checking for pending rewards for ${this.Progression.PmcName}`, LogTextColor.CYAN);

        const skills = this.PmcProfile.Skills.Common;
        for (const skill of skills)
        {
            if (skill.Progress === 0) continue;

            const tier = Math.floor(this.convertSkillProgressToRewardTier(skill.Progress));
            const rewardDiff = this.findDifferenceInRewardLevel(skill.Id, tier);

            if (rewardDiff > 0)
            {
                this.sendPendingSkillRewards(skill.Id, tier);
            }
        }

        this.IOManager.saveProgressionFile(this.Progression, `${this.PmcProfile._id}.json`);
    }

    private convertSkillProgressToRewardTier(progress: number): number
    {
        return (progress / 100) / 5;
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

        this.logger.logWithColor(`Skills Extended: Pending rewards for ${skillId}. ${tierDiff} tiers!`, LogTextColor.CYAN);

        for (let i = startTier; i <= tier; i++)
        {    
            if (this.sendMailReward(skillId, i))
            {
                this.Progression.Progress[skillId] = i;
            }
        }   
    }

    private generateReward(tier: number, debug: boolean = false): Item[]
    {
        const items: Item[] = [];
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;

        const locale = this.InstanceManager.database.locales.global["en"];

        const rewards = this.SkillRewards.RewardPool as IRewardTier[];
        const tierData = rewards.find(x => x.Tier === tier);

        const itemPrices: Record<string, number> = {};
       
        if (debug)
        {
            this.logger.logWithColor(`\nGenerating reward for tier ${tier}`, LogTextColor.YELLOW);
            this.logger.logWithColor("Settings:", LogTextColor.YELLOW);
            this.logger.logWithColor(`Reward value: ${tierData.RewardValue}`, LogTextColor.YELLOW);
            this.logger.logWithColor(`Max amount of same item: ${tierData.MaximumNumberOfMultiples}`, LogTextColor.YELLOW);
        }
        
        // Build a price map of all items in all categories
        for (const category of tierData.RewardCategories)
        {
            const rewards = this.getItemsAndPricesOfCategory(category);

            for (const reward in rewards)
            {
                itemPrices[reward] = rewards[reward];
            }
        }

        // Shuffle the category keys to ensure randomness
        const randomShuffle = this.shuffleKeys(itemPrices);

        let rewardValue = 0;
        let itemsReceived = 0;

        for (const key of randomShuffle)
        {
            // We have more value than allowed
            if (rewardValue > tierData.RewardValue) break;

            // Item is over this tiers price cap
            if (itemPrices[key] > tierData.RewardValue && tier !== 10) continue;
     
            // Item has no price, skip it
            if (itemPrices[key] === 0) continue;

            // Skip dog tags and quest items
            if (itemHelper.isDogtag(key) || itemHelper.isQuestItem(key)) continue;

            const noDupes = itemHelper.isOfBaseclasses(key, this.SkillRewards.DisallowMultipleSameRoll);
            
            let roundedAmount = 0;

            if (!noDupes)
            {
                roundedAmount = Math.round(Math.random() * tierData.MaximumNumberOfMultiples);
            }
            
            let amount = roundedAmount == 0 
                ? 1 
                : roundedAmount;

            if (itemHelper.isOfBaseclass(key, BaseClasses.AMMO))
            {
                amount = 20 * tier < 40 
                    ? 40 
                    : 20 * tier;
            }

            rewardValue += itemPrices[key] * amount;

            const newItem: Item = {
                _tpl: key,
                _id: hashUtil.generate()
            }

            if (itemHelper.addUpdObjectToItem(newItem))
            {
                newItem.upd.StackObjectsCount = amount;
            }

            this.logger.logWithColor(`${locale[`${key} Name`]} amt: (${amount}) val: (${itemPrices[key] * amount})`, LogTextColor.GREEN);

            itemsReceived += amount;
            items.push(newItem);
        }

        if (debug)
        {
            this.logger.logWithColor(`Total reward value: ${rewardValue} - Item count ${itemsReceived}`, LogTextColor.YELLOW);
        }
        
        // Set the items found in raid
        itemHelper.setFoundInRaid(items)

        return items;
    }

    private getItemsAndPricesOfCategory(parentId: string): Record<string, number>
    {
        const itemHelper = this.InstanceManager.itemHelper;
        const itemPriceMap: Record<string, number> = {};

        const items = itemHelper.getItemTplsOfBaseType(parentId);

        for (const item of items)
        {
            itemPriceMap[item] = itemHelper.getItemMaxPrice(item);
        }
        
        return itemPriceMap;
    }

    private sendMailReward(skillId: string, tier: number): boolean
    {
        const mailService = this.InstanceManager.mailSendService;

        const items = this.generateReward(tier);

        if (items.length <= 0) return false;

        const message: ISendMessageDetails = {
            recipientId: this.PmcProfile._id,
            sender: MessageType.SYSTEM_MESSAGE,
            messageText: `Here is your reward for tier ${tier} of ${skillId}`, // TODO: Hook this up to achievements
            items: items,
            itemsMaxStorageLifetimeSeconds: 72000
        }

        mailService.sendMessageToPlayer(message);

        return true;
    }

    private shuffleKeys<T>(record: Record<string, T>): string[] 
    {
        const keys = Object.keys(record);
        // Fisher-Yates shuffle algorithm
        for (let i = keys.length - 1; i > 0; i--) 
        {
            const j = Math.floor(Math.random() * (i + 1));
            [keys[i], keys[j]] = [keys[j], keys[i]];
        }

        return keys;
    }
}