/* eslint-disable @typescript-eslint/naming-convention */
import path from "node:path";

import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import type { IPmcData } from "@spt/models/eft/common/IPmcData";
import { MessageType } from "@spt/models/enums/MessageType";
import type { IRewardTier, ISkillRewards } from "../Models/ISkillRewards";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { Item } from "@spt/models/eft/common/tables/IItem";
import { BaseClasses } from "@spt/models/enums/BaseClasses";

import type { InstanceManager } from "./InstanceManager";
import type { IOManager } from "./IOManager";
import type { IProgression } from "../Models/IProgression";
import { Traders } from "@spt/models/enums/Traders";

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

        this.SkillRewards = this.IOManager.loadJsonFile<ISkillRewards>(path.join(this.IOManager.ConfigPath, "SkillRewards.json"));

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
            this.logger.logWithColor(`Skills Extended: Progress file for ${this.PmcProfile._id} does not exist.`, LogTextColor.YELLOW);
            this.logger.logWithColor("Skills Extended: This is normal, creating a new one now.", LogTextColor.YELLOW);

            this.Progression = {
                Id: this.PmcProfile._id,
                Progress: {}
            }

            this.IOManager.saveProgressionFile(this.Progression, `${this.PmcProfile._id}.json`);
            return true;
        }

        this.Progression = this.IOManager.loadJsonFile<IProgression>(`${this.PmcProfile._id}.json`);

        this.logger.logWithColor(`Skills Extended: Progress file for ${this.Progression.Id} loaded.`, LogTextColor.GREEN);
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

        this.logger.logWithColor(`Skills Extended: Checking for pending rewards for ${this.Progression.Id}`, LogTextColor.CYAN);

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

    private generateReward(tier: number, debug = false): Item[]
    {
        const items: Item[] = [];
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;

        const locale = this.InstanceManager.database.locales.global.en;

        const rewards = this.SkillRewards.Tiers as IRewardTier[];
        const tierData = rewards.find(x => x.Tier === tier);
 
        if (this.SkillRewards.RewardCategories === undefined) return;

        if (debug)
        {
            this.logger.logWithColor(`\nGenerating reward for tier ${tier}`, LogTextColor.YELLOW);
            this.logger.logWithColor("Settings:", LogTextColor.YELLOW);
            this.logger.logWithColor(`Reward value: ${tierData.RewardValue}`, LogTextColor.YELLOW);
            this.logger.logWithColor(`Max amount of same item: ${tierData.MaximumNumberOfMultiples}`, LogTextColor.YELLOW);
        }

        // Shuffle the category keys to ensure randomness
        const itemPrices = this.generateItemPrices();
        const randomShuffle = this.shuffleKeys(itemPrices);

        let rewardValue = 0;
        let itemsReceived = 0;

        for (const item of randomShuffle)
        {
            // We have more value than allowed
            if (rewardValue > tierData.RewardValue) break;

            if (!this.checkForBaseConditions(
                itemPrices[item], 
                tierData.RewardValue,
                tier,
                item
            )) continue;

            const amount = this.calculateItemAmountForReward(tierData, item);
            rewardValue += itemPrices[item] * amount;

            const newItem: Item = {
                _tpl: item,
                _id: hashUtil.generate()
            }

            if (itemHelper.addUpdObjectToItem(newItem))
            {
                newItem.upd.StackObjectsCount = amount;
            }

            this.logger.logWithColor(`${locale[`${item} Name`]} amt: (${amount}) val: (${itemPrices[item] * amount})`, LogTextColor.GREEN);

            itemsReceived += amount;
            items.push(newItem);
        }

        if (tierData.LegaMedals > 0)
        {
            items.push(this.generateLegaMedals(tierData));
        }

        if (debug)
        {
            this.logger.logWithColor(`Total reward value: ${rewardValue} - Item count ${itemsReceived}`, LogTextColor.YELLOW);
        }
        
        // Set the items found in raid
        itemHelper.setFoundInRaid(items)

        return items;
    }

    /**
     * Builds a dictionary of itemId and price information
     * @param tierData Tier to get item price information for
     * @returns Record of items and prices
     */
    private generateItemPrices(): Record<string, number>
    {
        const itemPrices: Record<string, number> = {};

        // Build a price map of all items in all categories
        for (const category of this.SkillRewards.RewardCategories)
        {
            const rewards = this.getItemsAndPricesOfCategory(category);
    
            for (const reward in rewards)
            {
                itemPrices[reward] = rewards[reward];
            }
        }

        return itemPrices;
    }


    /**
     * Check for basic filtering conditions of items
     * @param itemPrice Item price to check
     * @param maxRewardValue Max allowed item price
     * @param tier Tier we are checking for
     * @param itemTpl Item tpl to check
     * @returns true if passes
     */
    private checkForBaseConditions(itemPrice: number, maxRewardValue: number, tier: number, itemTpl: string): boolean
    {
        const itemHelper = this.InstanceManager.itemHelper;

        // Item has no price, skip it
        if (itemPrice === 0) return false;

        // Item is over this tiers price cap
        if (itemPrice > maxRewardValue && tier < 10) return false;
     
        // Skip dog tags and quest items
        if (itemHelper.isDogtag(itemTpl) 
            || itemHelper.isQuestItem(itemTpl)
            || !itemHelper.isValidItem(itemTpl)) return false;

        if (itemHelper.armorItemCanHoldMods(itemTpl)) return false;

        return true;
    }

    private generateLegaMedals(tierData: IRewardTier): Item
    {
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;

        const newItem: Item = {
            _tpl: "6656560053eaaa7a23349c86",
            _id: hashUtil.generate()
        }

        if (itemHelper.addUpdObjectToItem(newItem))
        {
            newItem.upd.StackObjectsCount = tierData.LegaMedals;
        }

        return newItem;
    }

    private calculateItemAmountForReward(tierData: IRewardTier, itemTpl: string): number
    {
        const itemHelper = this.InstanceManager.itemHelper;
        let roundedAmount = 0;

        // Blacklisted from having multiple
        if (!itemHelper.isOfBaseclasses(itemTpl, this.SkillRewards.DisallowMultipleSameRoll))
        {
            roundedAmount = Math.round(Math.random() * tierData.MaximumNumberOfMultiples);
        }
        
        // Dont ever give a no items
        let amount = roundedAmount === 0 
            ? 1 
            : roundedAmount;

        // Handle stacks of ammo
        if (itemHelper.isOfBaseclass(itemTpl, BaseClasses.AMMO))
        {
            amount = 20 * tierData.Tier < 40 
                ? 40 
                : 20 * tierData.Tier;
        }

        return amount;
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
        const traderHelper = this.InstanceManager.traderHelper;

        const items = this.generateReward(tier);

        if (items.length <= 0) return false;

        mailService.sendDirectNpcMessageToPlayer(
            this.PmcProfile._id,
            traderHelper.getTraderById(Traders.THERAPIST),
            MessageType.MESSAGE_WITH_ITEMS,
            `Here is your reward for tier ${tier} of ${skillId}`,
            items,
            720000
        );

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