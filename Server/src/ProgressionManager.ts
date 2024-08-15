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
import { Money } from '@spt/models/enums/Money';
import { BaseClasses } from '@spt/models/enums/BaseClasses';

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
        if (!this.SkillRewards.ProgressionEnabled) return;

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
            return;
        }

        this.loadProgressionFile();

        this.logger.logWithColor(`Skills Extended: Progress file for ${this.Progression.PmcName} loaded.`, LogTextColor.GREEN);
        
        this.checkForPendingRewards();
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

        this.saveProgressionFile();
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

    private selectRewardsFromTier(tier: number): Item[]
    {
        const items: Item[] = [];
        const hashUtil = this.InstanceManager.hashUtil;
        let randomRoll = Math.random() * 100;

        const locale = this.InstanceManager.database.locales.global["en"];

        const itemHelper = this.InstanceManager.itemHelper;

        const rewards = this.SkillRewards.RewardPool as IRewardTier[];
        const pool = rewards.find(x => x.Tier === tier);

        // Shuffle the keys so its entirely random each time
        const shuffledKeys = this.shuffleKeys(pool.Rewards);
        
        if (shuffledKeys.length === 0) return [];

        let rolls = 0;
        let winningRolls = 0;

        for (const reward of shuffledKeys)
        {
            if (rolls > pool.Rolls) break;

            rolls++;

            const itemName = locale[`${reward} Name`];
            const chance = pool.Rewards[reward];
            const pityBonusEnabled = rolls > winningRolls
            const pityBonus = 1 - (pool.PityBonus * (rolls - winningRolls));

            randomRoll = pityBonusEnabled 
                ? randomRoll * pityBonus 
                : randomRoll;
 
            if (chance < randomRoll) continue;

            const legendary = pool.Rewards[reward] < 10;
            const legendaryText = legendary ? " legendary" : "";
            const color = legendary ? LogTextColor.BLUE : LogTextColor.GREEN;

            let numberToAward = 1;

            // If the item is of a certain type randomize the amount to send
            if (itemHelper.isOfBaseclasses(reward, this.SkillRewards.BaseClassesThatCanRewardMultiple))
            {
                numberToAward = Math.round(Math.random() * this.SkillRewards.MaximumNumberOfMultiples[tier]);
            }

            // Handle Ammo as a special case
            if (itemHelper.isOfBaseclass(reward, BaseClasses.AMMO))
            {
                const baseReward = this.SkillRewards.BaseAmmoAmount;
                const multiplier = this.SkillRewards.AmmoRewardMultPerTier * tier;

                numberToAward = Math.round(baseReward * (1 + multiplier));

                const newReward: Item = {
                    _id: hashUtil.generate(),
                    _tpl: reward
                }

                if (itemHelper.addUpdObjectToItem(newReward))
                {       
                    newReward.upd.StackObjectsCount = numberToAward;
                    items.push(newReward);

                    this.logger.logWithColor(`Skills Extended: Generating${legendaryText} reward ${itemName} in the amount of ${numberToAward} from tier ${tier} pool`, color);

                    winningRolls++;
                    randomRoll = Math.random() * 100;
                    continue;
                }
            }

            for (let i = 0; i < numberToAward; i++)
            {
                const newReward: Item = {
                    _id: hashUtil.generate(),
                    _tpl: reward
                }

                items.push(newReward);
            }

            this.logger.logWithColor(`Skills Extended: Generating${legendaryText} reward ${itemName} in the amount of ${numberToAward} from tier ${tier} pool`, color);

            // Roll another number
            winningRolls++;
            randomRoll = Math.random() * 100;
        }

        const roubles = this.SkillRewards.BaseRoubleReward * tier;

        const newReward: Item = {
            _id: hashUtil.generate(),
            _tpl: Money.ROUBLES
        }

        if (itemHelper.addUpdObjectToItem(newReward))
        {
            newReward.upd.StackObjectsCount = roubles;
            items.push(newReward);        
        }

        return items;
    }

    private sendMailReward(skillId: string, tier: number): boolean
    {
        const mailService = this.InstanceManager.mailSendService;

        const items = this.selectRewardsFromTier(tier);

        if (items.length <= 0) return false;

        const message: ISendMessageDetails = {
            recipientId: this.PmcProfile._id,
            sender: MessageType.SYSTEM_MESSAGE,
            messageText: `Here is your reward for tier ${tier} of ${skillId}`,
            items: items,
            itemsMaxStorageLifetimeSeconds: 72000
        }

        mailService.sendMessageToPlayer(message);

        return true;
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