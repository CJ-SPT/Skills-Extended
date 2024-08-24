/* eslint-disable @typescript-eslint/naming-convention */

import type { IHideoutProduction } from "@spt/models/eft/hideout/IHideoutProduction";

export interface ISkillsConfig
{
    FirstAid: IFirstAidConfig;
    FieldMedicine: IFieldMedicineConfig;
    NatoRifle: IWeaponConfig;
    EasternRifle: IWeaponConfig;
    LockPicking: ILockpickingConfig;
    ProneMovement: IProneMovementConfig;
    BearAuthority: IBearAuthorityConfig;
    UsecNegotiations: IUsecNegotiationsConfig;
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
    PICK_STRENGTH: number;
    PICK_STRENGTH_PER_LEVEL: number;
    SWEET_SPOT_ANGLE: number;
    SWEET_SPOT_ANGLE_PER_LEVEL: number;
    ATTEMPTS_BEFORE_BREAK: number;
    INSPECT_LOCK_XP_RATIO: number;
    FAILURE_LOCK_XP_RATIO: number;
    XP_TABLE: Record<string, number>;
    DOOR_PICK_LEVELS: Record<string, Record<string, number>>;
    CRAFTING_RECIPES: IHideoutProduction[];
}

export interface IProneMovementConfig
{
    ENABLED: boolean;
    XP_PER_ACTION: number;
    MOVEMENT_SPEED_INCREASE_MAX: number;
    MOVEMENT_SPEED_INCREASE_MAX_ELITE: number;
    MOVEMENT_VOLUME_DECREASE_MAX: number;
    MOVEMENT_VOLUME_DECREASE_MAX_ELITE: number;
}

export interface IBearAuthorityConfig
{
    ENABLED: boolean;
    XP_PER_ACTION: number;
    TRADER_PRICE_DECREASE: number;
    TRADER_PRICE_DECREASE_ELITE: number;
    REPAIR_PRICE_DECREASE: number;
    REPAIR_PRICE_DECREASE_ELITE: number;
}

export interface IUsecNegotiationsConfig
{
    ENABLED: boolean;
    XP_PER_ACTION: number;
    TRADER_PRICE_DECREASE: number;
    TRADER_PRICE_DECREASE_ELITE: number;
    HEAL_PRICE_DECREASE: number;
    HEAL_PRICE_DECREASE_ELITE: number;
}

export interface IAdditionalWeapons
{
    AdditionalNatoWeapons: string[];
    AdditionalEasternWeapons: string[];
}