/* eslint-disable @typescript-eslint/naming-convention */

import type { IHideoutProduction } from "@spt/models/eft/hideout/IHideoutProduction";

export interface ISkillsConfig
{
    FirstAid: IFirstAidConfig;
    FieldMedicine: IFieldMedicineConfig;
    NatoRifle: IWeaponConfig;
    EasternRifle: IWeaponConfig;
    LockPicking: ILockpickingConfig;
}

export interface IFirstAidConfig
{
    ENABLED: boolean;
    XP_PER_ACTION: number;
    MEDKIT_USAGE_REDUCTION: number;
    MEDKIT_USAGE_REDUCTION_ELITE: number;
    MEDKIT_SPEED_BONUS: number;
    MEDKIT_SPEED_BONUS_ELITE: number;
}

export interface IFieldMedicineConfig
{
    ENABLED: boolean;
    XP_PER_ACTION: number;
    SKILL_BONUS: number;
    SKILL_BONUS_ELITE: number;
    DURATION_BONUS: number;
    DURATION_BONUS_ELITE: number;
    POSITIVE_EFFECT_BONUS: number;
    POSITIVE_EFFECT_BONUS_ELITE: number;
}

export interface IWeaponConfig
{
    ENABLED: boolean;
    SKILL_SHARE_ENABLED: boolean;
    SKILL_SHARE_XP_RATIO: number;
    WEAPON_PROF_XP: number;
    ERGO_MOD: number;
    ERGO_MOD_ELITE: number;
    RECOIL_REDUCTION: number;
    RECOIL_REDUCTION_ELITE: number;
    WEAPONS: string[];
}

export interface ILockpickingConfig
{
    ENABLED: boolean;
    INSPECT_BASE_TIME: number;
    INSPECT_CHANCE_BONUS: number;
    PICK_BASE_TIME: number;
    ATTEMPTS_BEFORE_BREAK: number;
    TIME_REDUCTION_BONUS: number;
    TIME_REDUCTION_BONUS_ELITE: number;
    PICK_BASE_SUCCESS_CHANCE: number;
    PICK_BASE_DIFFICULTY_MOD: number;
    INSPECT_LOCK_XP_RATIO: number;
    XP_TABLE: Record<string, number>;
    DOOR_PICK_LEVELS: Record<string, Record<string, number>>;
    CRAFTING_RECIPES: IHideoutProduction[];
}