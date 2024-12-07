/* eslint-disable @typescript-eslint/naming-convention */
import path from "node:path";

import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import { IPmcData } from "@spt/models/eft/common/IPmcData";
import { MessageType } from "@spt/models/enums/MessageType";
import { IRewardTier, ISkillRewards } from "../Models/ISkillRewards";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { IItem } from "@spt/models/eft/common/tables/IItem";
import { BaseClasses } from "@spt/models/enums/BaseClasses";

import { InstanceManager } from "./InstanceManager";
import { IOManager } from "./IOManager";
import { IProgression } from "../Models/IProgression";
import { IServerConfig } from "../Models/IServerConfig";
import { Traders } from "@spt/models/enums/Traders";

export class ProgressionManager
{
    private InstanceManager: InstanceManager;
    private IOManager: IOManager;
    private logger: ILogger;

    private PmcProfile: IPmcData;
    private Progression: IProgression;
    private SkillRewards: ISkillRewards;
    private ServerConfig: IServerConfig;

    public init(instanceManager: InstanceManager, ioManager: IOManager): void
    {
        this.InstanceManager = instanceManager;
        this.IOManager = ioManager;
        this.logger = instanceManager.logger;
        this.ServerConfig = ioManager.ServerConfig;

        this.SkillRewards = this.IOManager.loadJsonFile<ISkillRewards>(path.join(this.IOManager.ConfigPath, "SkillRewards.json"));

        this.debugTestGeneration();
    }

    public getActivePmcData(sessionId: string): void
    {
        if (!this.ServerConfig.EnableProgression) return;

        this.PmcProfile = this.InstanceManager.profileHelper.getPmcProfile(sessionId);

        this.checkForOrCreateProgressFile();
    }

    public wipeProgressFile(sessionId: string): void
    {
        if (!this.ServerConfig.EnableProgression) return;

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
        if (this.ServerConfig.ProgressionDebug.Enabled && this.ServerConfig.ProgressionDebug.TestGeneration)
        {
            const runs = this.ServerConfig.ProgressionDebug.NumberOfRuns;
            const level = this.ServerConfig.ProgressionDebug.GenerationLevel;

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

        this.Progression = this.IOManager.loadJsonFile<IProgression>(progPath);

        this.logger.logWithColor(`Skills Extended: Progress file for ${this.Progression.Id} loaded.`, LogTextColor.GREEN);
        return false;
    }

    public checkForPendingRewards(): void
    {
        if (!this.ServerConfig.EnableProgression) return;

        if (this.PmcProfile?.Skills?.Common === undefined)
        {
            this.logger.logWithColor("Skills Extended: No skills defined on profile, this is normal on new or wiped profiles.", LogTextColor.YELLOW);
            return;
        }

        this.logger.logWithColor(`Skills Extended: Checking for pending rewards for ${this.Progression.Id}`, LogTextColor.CYAN);

        const skills = this.PmcProfile.Skills.Common;
        for (const skill of skills)
        {
            if (skill.Progress === 0) continue;

            const tier = this.convertSkillProgressToRewardTier(skill.Progress);
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
        return Math.floor((progress / 100) / 5);
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

    private generateReward(tier: number, debug = false): IItem[]
    {
        const items: IItem[] = [];
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;

        itemHelper.getSoftInsertSlotIds();

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
            if (itemsReceived > tierData.ItemAmountRange[1]) break;

            if (!this.checkForBaseConditions(
                itemPrices[item], 
                tierData.RewardValue,
                tier,
                item
            )) continue;

            if (itemHelper.isOfBaseclass(item, BaseClasses.AMMO))
            {
                const ammoItem = this.generateAmmoReward(item, tierData);
    
                itemsReceived += ammoItem?.upd?.StackObjectsCount;
                items.push(ammoItem);
                this.logger.logWithColor(`${locale[`${item} Name`]} amt: (${ammoItem?.upd?.StackObjectsCount}) val: (${itemPrices[item] * ammoItem?.upd?.StackObjectsCount})`, LogTextColor.GREEN);
                continue;
            }

            if (itemHelper.itemRequiresSoftInserts(item))
            {
                const armorItem = this.generateArmorReward(item);

                items.push(...armorItem[0]);
                itemsReceived += 1;
                rewardValue += armorItem[1];
                this.logger.logWithColor(`${locale[`${item} Name`]} amt: (1) val: (${armorItem[1]})`, LogTextColor.GREEN);
                continue;
            }

            const amount = this.calculateItemAmountForReward(tierData, item);
            rewardValue += itemPrices[item] * amount;

            for (let i = 0; i < amount; i++)
            {
                const newItem: IItem = {
                    _tpl: item,
                    _id: hashUtil.generate()
                }

                items.push(newItem);
            }

            this.logger.logWithColor(`${locale[`${item} Name`]} amt: (${amount}) val: (${itemPrices[item] * amount})`, LogTextColor.GREEN);

            itemsReceived += amount;
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
        const config = this.SkillRewards;
        const itemHelper = this.InstanceManager.itemHelper;

        // Item is black listed, skip it
        if (config.BlackListedItems.includes(itemTpl)) return false;

        // Item has no price, skip it
        if (itemPrice === 0) return false;

        // Item is over this tiers price cap
        if (itemPrice > maxRewardValue && tier < 10) return false;
     
        // Skip dog tags and quest items
        if (itemHelper.isDogtag(itemTpl) 
            || itemHelper.isQuestItem(itemTpl)
            || !itemHelper.isValidItem(itemTpl)) return false;

        return true;
    }

    private generateLegaMedals(tierData: IRewardTier): IItem
    {
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;

        const newItem: IItem = {
            _tpl: "6656560053eaaa7a23349c86",
            _id: hashUtil.generate()
        }

        if (itemHelper.addUpdObjectToItem(newItem))
        {
            newItem.upd.StackObjectsCount = tierData.LegaMedals;
        }

        return newItem;
    }

    private generateAmmoReward(itemTpl: string, tierData: IRewardTier): IItem
    {
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;
        const locale = this.InstanceManager.database.locales.global.en;

        const newItem: IItem = {
            _tpl: itemTpl,
            _id: hashUtil.generate()
        }

        const amount = 20 * tierData.Tier < 40 
            ? 40 
            : 20 * tierData.Tier;

        if (itemHelper.addUpdObjectToItem(newItem))
        {
            newItem.upd.StackObjectsCount = amount;
        }

        this.logger.logWithColor(`${locale[`${itemTpl} Name`]} amt: (${amount})`, LogTextColor.GREEN);

        return newItem;
    }

    private generateArmorReward(itemTpl: string): [IItem[], number]
    {
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;
        
        const armor: IItem[] = [];
        const id: string = hashUtil.generate();

        const newItem: IItem = {
            _tpl: itemTpl,
            _id: id
        }

        armor.push(newItem);
        itemHelper.addUpdObjectToItem(newItem);

        const inserts = this.generateSoftArmorInserts(itemTpl, id);

        const price = itemHelper.getItemPrice(itemTpl);

        armor.push(...inserts[0]);

        return [armor, price + inserts[1]];
    }

    private generateSoftArmorInserts(itemTpl: string, itemId: string): [IItem[], number]
    {
        const hashUtil = this.InstanceManager.hashUtil;
        const itemHelper = this.InstanceManager.itemHelper;

        const items: IItem[] = [];
        
        const db = this.InstanceManager.database.templates.items;
        
        const slots = db[itemTpl]._props?.Slots;
        const slotIds = itemHelper.getSoftInsertSlotIds();

        if (slots === undefined) return [items, 0];

        let platePrices = 0;

        for (const slot of slots)
        {
            if (!slotIds.includes(slot?._name.toLocaleLowerCase())) continue;
    
            const plate = slot._props.filters[0].Plate;

            const insert: IItem = {
                _tpl: plate,
                _id: hashUtil.generate(),
                parentId: itemId,
                slotId: slot._name
            }

            platePrices += itemHelper.getItemPrice(plate);

            items.push(insert);
        }

        return [items, platePrices];
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
        const amount = roundedAmount === 0 
            ? 1 
            : roundedAmount;

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

        const traderEnabled = this.IOManager.ServerConfig.EnableTrader;
        const traderToSend = traderEnabled ? "66bf1f65e1f3b83ea069a271" : Traders.THERAPIST;

        const items = this.generateReward(tier);

        if (items.length <= 0) return false;

        mailService.sendDirectNpcMessageToPlayer(
            this.PmcProfile._id,
            traderHelper.getTraderById(traderToSend),
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