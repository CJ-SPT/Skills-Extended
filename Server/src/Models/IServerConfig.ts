export interface IServerConfig
{
    EnableTrader: boolean;
    EnableProgression: boolean;
    ProgressionDebug: ProgressionDebug;
}

export interface ProgressionDebug
{
    Enabled: boolean;
    TestGeneration: boolean;
    GenerationLevel: number;
    NumberOfRuns: number;
}