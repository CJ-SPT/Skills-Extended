export interface ISkillRewards
{
    ProgressionEnabled: boolean;
    RewardCategories: string[];
    DisallowMultipleSameRoll: string[];
    Tiers: IRewardTier[];
    Debug: ProgressionDebug;
}

export interface IRewardTier
{
    Tier: number;
    RewardValue: number;
    LegaMedals: number;
    MaximumNumberOfMultiples: number;
}

export interface ProgressionDebug
{
    Enabled: boolean;
    TestGeneration: boolean;
    GenerationLevel: number;
    NumberOfRuns: number;
}