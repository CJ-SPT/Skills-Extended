export interface ISkillRewards
{
    ProgressionEnabled: boolean;
    DisallowMultipleSameRoll: string[];
    RewardPool: IRewardTier[];
    Debug: ProgressionDebug;
}

export interface IRewardTier
{
    Tier: number;
    MinRewards: number;
    MaxRewards: number;
    RewardValue: number;
    MaximumNumberOfMultiples: number;
    // Item Tpl, and the chance to get it from this tier
    RewardCategories: string[];
}

export interface ProgressionDebug
{
    Enabled: boolean;
    TestGeneration: boolean;
    GenerationLevel: number;
    NumberOfRuns: number;
}