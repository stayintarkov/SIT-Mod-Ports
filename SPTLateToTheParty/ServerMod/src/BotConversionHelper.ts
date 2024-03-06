import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { MinMax } from "@spt-aki/models/common/MinMax";
import { IPmcConfig } from "@spt-aki/models/spt/config/IPmcConfig";

export class BotConversionHelper
{
    // All variables should be static because there should only be one instance of this object
    private static escapeTime: number
    private static simulatedTimeRemaining: number

    private static commonUtils: CommonUtils
    private static iPmcConfig: IPmcConfig

    private static timerHandle: NodeJS.Timer
    private static timerRunning: boolean
    private static convertIntoPmcChanceOrig: Record<string, MinMax> = {};

    constructor(commonUtils: CommonUtils, iPmcConfig: IPmcConfig)
    {
        BotConversionHelper.commonUtils = commonUtils;
        BotConversionHelper.iPmcConfig = iPmcConfig;

        // Store the values in iBotConfig as default settings
        this.setOriginalData();
    }

    public setEscapeTime(escapeTime: number, timeRemaining: number): void
    {
        // Ensure the instance of this object is valid
        if (!this.checkIfInitialized())
        {
            BotConversionHelper.commonUtils.logError("BotConversionHelper object not initialized!");
            return;
        }

        BotConversionHelper.escapeTime = escapeTime;
        BotConversionHelper.simulatedTimeRemaining = timeRemaining;

        // Ensure there isn't already a timer running
        if (!BotConversionHelper.timerRunning && modConfig.adjust_bot_spawn_chances.adjust_pmc_conversion_chances)
        {
            // Start a recurring task to update bot spawn settings
            BotConversionHelper.timerHandle = setInterval(BotConversionHelper.simulateRaidTime, 1000 * modConfig.adjust_bot_spawn_chances.pmc_conversion_update_rate);
        }
        
        BotConversionHelper.commonUtils.logInfo("Updated escape time");
    }

    public static stopRaidTimer(): void
    {
        if (!modConfig.adjust_bot_spawn_chances.adjust_pmc_conversion_chances)
        {
            return;
        }

        // Stop the recurring task
        clearInterval(BotConversionHelper.timerHandle);
        BotConversionHelper.timerRunning = false;

        // Reset the PMC-conversion chances to their original settings
        BotConversionHelper.adjustPmcConversionChance(1);

        BotConversionHelper.commonUtils.logInfo("Stopped task for adjusting PMC-conversion chances.");
    }

    public static adjustPmcConversionChance(timeRemainingFactor: number): void
    {
        // Determine the factor that should be applied to the PMC-conversion chances based on the config.json setting
        const adjFactor = CommonUtils.interpolateForFirstCol(modConfig.pmc_spawn_chance_multipliers, timeRemainingFactor);

        // Adjust the chances for each applicable bot type
        let logMessage = "";
        for (const pmcType in BotConversionHelper.iPmcConfig.convertIntoPmcChance)
        {
            // Do not allow the chances to exceed 100%. Who knows what might happen...
            const min = Math.round(Math.min(100, BotConversionHelper.convertIntoPmcChanceOrig[pmcType].min * adjFactor));
            const max = Math.round(Math.min(100, BotConversionHelper.convertIntoPmcChanceOrig[pmcType].max * adjFactor));

            BotConversionHelper.iPmcConfig.convertIntoPmcChance[pmcType].min = min;
            BotConversionHelper.iPmcConfig.convertIntoPmcChance[pmcType].max = max;

            logMessage += `${pmcType}: ${min}-${max}%, `;
        }

        BotConversionHelper.commonUtils.logInfo(`Adjusting PMC spawn chances (${adjFactor}): ${logMessage}`);
    }

    private checkIfInitialized(): boolean
    {
        if (BotConversionHelper.commonUtils === undefined)
        {
            return false;
        }

        if (BotConversionHelper.iPmcConfig === undefined)
        {
            return false;
        }

        return true;
    }

    private setOriginalData(): void
    {
        // Store the default PMC-conversion chances for each bot type defined in SPT's configuration file
        let logMessage = "";
        for (const pmcType in BotConversionHelper.iPmcConfig.convertIntoPmcChance)
        {
            if (BotConversionHelper.convertIntoPmcChanceOrig[pmcType] !== undefined)
            {
                logMessage += `${pmcType}: already buffered, `;
                continue;
            }

            const chances: MinMax = {
                min: BotConversionHelper.iPmcConfig.convertIntoPmcChance[pmcType].min,
                max: BotConversionHelper.iPmcConfig.convertIntoPmcChance[pmcType].max
            }
            BotConversionHelper.convertIntoPmcChanceOrig[pmcType] = chances;

            logMessage += `${pmcType}: ${chances.min}-${chances.max}%, `;
        }

        BotConversionHelper.commonUtils.logInfo(`Reading default PMC spawn chances: ${logMessage}`);
    }

    private static simulateRaidTime(): void
    {
        BotConversionHelper.timerRunning = true;

        // Adjust the PMC-conversion chances once per cycle
        const timeFactor = BotConversionHelper.simulatedTimeRemaining / BotConversionHelper.escapeTime;
        BotConversionHelper.adjustPmcConversionChance(timeFactor);

        // Decrement the simulated raid time to prepare for the next cycle
        BotConversionHelper.simulatedTimeRemaining -= modConfig.adjust_bot_spawn_chances.pmc_conversion_update_rate;
    }
}