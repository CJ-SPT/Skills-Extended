export interface ISkillRewards
{
    ProgressionEnabled: boolean;
    BaseRoubleReward: number;
    BaseAmmoAmount: number;
    AmmoRewardMultPerTier: number;
    BaseClassesThatCanRewardMultiple: string[];
    MaximumNumberOfMultiples: number[];
    RewardPool: IRewardTier[];
}

export interface IRewardTier
{
    Tier: number;
    Rolls: number;
    PityBonus: number;
    // Item Tpl, and the chance to get it from this tier
    Rewards: Record<string, number>;
}