/* eslint-disable @typescript-eslint/naming-convention */
export interface ISkillData
{
    MedicalSkills: MedicalSkillData;
    USECRifleSkill: WeaponSkillData;
    BEARRifleskill: WeaponSkillData;
    LockPicking: LockPickingData
}

export interface MedicalSkillData
{
    MEDKIT_HP_BONUS: number;
    MEDKIT_HP_BONUS_ELITE: number;
    MEDICAL_SPEED_BONUS: number;
    MEDICAL_SPEED_BONUS_ELITE: number;
}

export interface WeaponSkillData
{
    WEAPON_PROF_XP: number;
    ERGO_MOD: number;
    ERGO_MOD_ELITE: number;
    RECOIL_REDUCTION: number;
    RECOIL_REDUCTION_ELITE: number;
}

export interface LockPickingData
{
    BASE_PICK_TIME: number;
    PICK_TIME_REDUCTION: number;
    PICK_TIME_REDUCTION_ELITE: number;
    BASE_PICK_CHANCE: number;
    BONUS_CHANCE_PER_LEVEL: number;
    BONUS_CHANCE_PER_LEVEL_ELITE: number;
}