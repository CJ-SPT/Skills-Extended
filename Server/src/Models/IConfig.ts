export interface ISkillRewards
{
    ProgressionEnabled: boolean;
    RewardTiers: Record<string, number>;
    SkillRewards: IRewards;
}

export interface IRewards
{
    Endurance: IRewardTier[];
    Strength: IRewardTier[];
}

export interface IRewardTier
{
    Tier: number;
    Rewards: string[];
}