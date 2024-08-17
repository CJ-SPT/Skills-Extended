/* eslint-disable @typescript-eslint/naming-convention */

export interface ICustomQuestCondition
{
    QuestId: string;
    ConditionId: string;
    ConditionType: string;
    Locations?: string[];
}