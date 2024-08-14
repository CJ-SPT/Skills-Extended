export interface ISkillRewards
{
    ProgressionEnabled: boolean;
    BaseRoubleReward: number;
    RewardPool: IRewardTier[];
    BaseClassesThatCanRewardMultiple: string[];
    MaximumNumberOfMultiples: number;
}

export interface IRewardTier
{
    Tier: number;
    Rolls: number;
    PityBonus: number;
    // Item Tpl, and the chance to get it from this tier
    Rewards: Record<string, number>;
}