import { IPmcData } from "@spt/models/eft/common/IPmcData";
import { IBodyPartsHealth, IHealth } from "@spt/models/eft/common/tables/IBotBase";
import { ISyncHealthRequestData } from "@spt/models/eft/health/ISyncHealthRequestData";
import { IEffects, ISptProfile } from "@spt/models/eft/profile/ISptProfile";
import { IHealthConfig } from "@spt/models/spt/config/IHealthConfig";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { SaveServer } from "@spt/servers/SaveServer";
import { DatabaseService } from "@spt/services/DatabaseService";
import { TimeUtil } from "@spt/utils/TimeUtil";
import type { ICloner } from "@spt/utils/cloners/ICloner";
export declare class HealthHelper {
    protected logger: ILogger;
    protected timeUtil: TimeUtil;
    protected saveServer: SaveServer;
    protected databaseService: DatabaseService;
    protected configServer: ConfigServer;
    protected cloner: ICloner;
    protected healthConfig: IHealthConfig;
    constructor(logger: ILogger, timeUtil: TimeUtil, saveServer: SaveServer, databaseService: DatabaseService, configServer: ConfigServer, cloner: ICloner);
    /**
     * Resets the profiles vitality/health and vitality/effects properties to their defaults
     * @param sessionID Session Id
     * @returns updated profile
     */
    resetVitality(sessionID: string): ISptProfile;
    /**
     * Update player profile vitality values with changes from client request object
     * @param pmcData Player profile
     * @param postRaidHealth Post raid data
     * @param sessionID Session id
     * @param isDead Is player dead
     * @param addEffects Should effects be added to profile (default - true)
     * @param deleteExistingEffects Should all prior effects be removed before apply new ones  (default - true)
     */
    updateProfileHealthPostRaid(pmcData: IPmcData, postRaidHealth: IHealth, sessionID: string, isDead: boolean): void;
    protected storeHydrationEnergyTempInProfile(fullProfile: ISptProfile, hydration: number, energy: number, temprature: number): void;
    /**
     * Take body part effects from client profile and apply to server profile
     * @param postRaidBodyParts Post-raid body part data
     * @param profileData Player profile on server
     */
    protected transferPostRaidLimbEffectsToProfile(postRaidBodyParts: IBodyPartsHealth, profileData: IPmcData): void;
    /**
     * Update player profile vitality values with changes from client request object
     * @param pmcData Player profile
     * @param request Heal request
     * @param sessionID Session id
     * @param addEffects Should effects be added to profile (default - true)
     * @param deleteExistingEffects Should all prior effects be removed before apply new ones  (default - true)
     */
    saveVitality(pmcData: IPmcData, request: ISyncHealthRequestData, sessionID: string, addEffects?: boolean, deleteExistingEffects?: boolean): void;
    /**
     * Adjust hydration/energy/temperate and body part hp values in player profile to values in profile.vitality
     * @param pmcData Profile to update
     * @param sessionId Session id
     */
    protected saveHealth(pmcData: IPmcData, sessionID: string): void;
    /**
     * Save effects to profile
     * Works by removing all effects and adding them back from profile
     * Removes empty 'Effects' objects if found
     * @param pmcData Player profile
     * @param sessionId Session id
     * @param bodyPartsWithEffects dict of body parts with effects that should be added to profile
     * @param addEffects Should effects be added back to profile
     */
    protected saveEffects(pmcData: IPmcData, sessionId: string, bodyPartsWithEffects: IEffects, deleteExistingEffects?: boolean): void;
    /**
     * Add effect to body part in profile
     * @param pmcData Player profile
     * @param effectBodyPart body part to edit
     * @param effectType Effect to add to body part
     * @param duration How long the effect has left in seconds (-1 by default, no duration).
     */
    protected addEffect(pmcData: IPmcData, effectBodyPart: string, effectType: string, duration?: number): void;
    protected isEmpty(map: Record<string, {
        Time: number;
    }>): boolean;
}
