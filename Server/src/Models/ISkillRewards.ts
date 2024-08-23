export interface ISkillRewards
{
    RewardCategories: string[];
    DisallowMultipleSameRoll: string[];
    BlackListedItems: string[];
    Tiers: IRewardTier[];
}

export interface IRewardTier
{
    Tier: number;
    RewardValue: number;
    ItemAmountRange: number[];
    LegaMedals: number;
    MaximumNumberOfMultiples: number;
}