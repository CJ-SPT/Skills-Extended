export interface IServerConfig
{
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