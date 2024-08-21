/* eslint-disable @typescript-eslint/naming-convention */

export interface ICustomQuestConditions
{
    QuestId: string; 
    Conditions: ICustomCondition[];
}

export interface ICustomCondition
{
    // Must match the kill condition we are overriding
    ConditionId: string;
    // Type of the condition 
    ConditionType: EQuestCondition;
    Locations?: string[];
}

export enum EQuestCondition
{
    InspectLock = "InspectLock",
    PickLock = "PickLock",
    PickLockFailed = "PickLockFailed",
    BreakLock = "BreakLock",
    HackDoor = "HackDoor",
    HackDoorFailed = "HackDoorFailed"
}