"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const config_json_1 = __importDefault(require("../config/config.json"));
const CommonUtils_1 = require("./CommonUtils");
const QuestManager_1 = require("./QuestManager");
const ConfigTypes_1 = require("C:/snapshot/project/obj/models/enums/ConfigTypes");
const modName = "SPTQuestingBots";
class QuestingBots {
    commonUtils;
    questManager;
    logger;
    configServer;
    databaseServer;
    databaseTables;
    localeService;
    questHelper;
    profileHelper;
    vfs;
    iBotConfig;
    iPmcConfig;
    iLocationConfig;
    iAirdropConfig;
    convertIntoPmcChanceOrig = {};
    preAkiLoad(container) {
        this.logger = container.resolve("WinstonLogger");
        const staticRouterModService = container.resolve("StaticRouterModService");
        const dynamicRouterModService = container.resolve("DynamicRouterModService");
        // Get config.json settings for the bepinex plugin
        staticRouterModService.registerStaticRouter(`StaticGetConfig${modName}`, [{
                url: "/QuestingBots/GetConfig",
                action: () => {
                    return JSON.stringify(config_json_1.default);
                }
            }], "GetConfig");
        // Report error messages to the SPT-AKI server console in case the user hasn't enabled the bepinex console
        dynamicRouterModService.registerDynamicRouter(`DynamicReportError${modName}`, [{
                url: "/QuestingBots/ReportError/",
                action: (url) => {
                    const urlParts = url.split("/");
                    const errorMessage = urlParts[urlParts.length - 1];
                    const regex = /%20/g;
                    this.commonUtils.logError(errorMessage.replace(regex, " "));
                    return JSON.stringify({ resp: "OK" });
                }
            }], "ReportError");
        // Get the logging directory for saving quest information after raids
        staticRouterModService.registerStaticRouter(`StaticGetLoggingPath${modName}`, [{
                url: "/QuestingBots/GetLoggingPath",
                action: () => {
                    return JSON.stringify({ path: __dirname + "/../log/" });
                }
            }], "GetLoggingPath");
        if (!config_json_1.default.enabled) {
            return;
        }
        // Game start
        // Needed to update Scav timer
        staticRouterModService.registerStaticRouter(`StaticAkiGameStart${modName}`, [{
                url: "/client/game/start",
                action: (url, info, sessionId, output) => {
                    if (config_json_1.default.debug.enabled) {
                        this.updateScavTimer(sessionId);
                    }
                    return output;
                }
            }], "aki");
        // Apply a scalar factor to the SPT-AKI PMC conversion chances
        dynamicRouterModService.registerDynamicRouter(`DynamicAdjustPMCConversionChances${modName}`, [{
                url: "/QuestingBots/AdjustPMCConversionChances/",
                action: (url) => {
                    const urlParts = url.split("/");
                    const factor = Number(urlParts[urlParts.length - 2]);
                    const verify = JSON.parse(urlParts[urlParts.length - 1].toLowerCase());
                    this.adjustPmcConversionChance(factor, verify);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "AdjustPMCConversionChances");
        // Get all EFT quest templates
        // NOTE: This includes custom quests added by mods
        staticRouterModService.registerStaticRouter(`GetAllQuestTemplates${modName}`, [{
                url: "/QuestingBots/GetAllQuestTemplates",
                action: () => {
                    return JSON.stringify({ templates: this.questHelper.getQuestsFromDb() });
                }
            }], "GetAllQuestTemplates");
    }
    postDBLoad(container) {
        this.configServer = container.resolve("ConfigServer");
        this.databaseServer = container.resolve("DatabaseServer");
        this.localeService = container.resolve("LocaleService");
        this.questHelper = container.resolve("QuestHelper");
        this.profileHelper = container.resolve("ProfileHelper");
        this.vfs = container.resolve("VFS");
        this.iBotConfig = this.configServer.getConfig(ConfigTypes_1.ConfigTypes.BOT);
        this.iPmcConfig = this.configServer.getConfig(ConfigTypes_1.ConfigTypes.PMC);
        this.iLocationConfig = this.configServer.getConfig(ConfigTypes_1.ConfigTypes.LOCATION);
        this.iAirdropConfig = this.configServer.getConfig(ConfigTypes_1.ConfigTypes.AIRDROP);
        this.databaseTables = this.databaseServer.getTables();
        this.commonUtils = new CommonUtils_1.CommonUtils(this.logger, this.databaseTables, this.localeService);
        this.questManager = new QuestManager_1.QuestManager(this.commonUtils, this.vfs);
        if (!config_json_1.default.enabled) {
            return;
        }
        // Ensure all of the custom quests are valid JSON files
        this.questManager.validateCustomQuests();
        if (config_json_1.default.debug.always_have_airdrops) {
            this.commonUtils.logInfo("Forcing airdrops to occur at the beginning of every raid...");
            this.iAirdropConfig.airdropChancePercent.bigmap = 100;
            this.iAirdropConfig.airdropChancePercent.woods = 100;
            this.iAirdropConfig.airdropChancePercent.lighthouse = 100;
            this.iAirdropConfig.airdropChancePercent.shoreline = 100;
            this.iAirdropConfig.airdropChancePercent.interchange = 100;
            this.iAirdropConfig.airdropChancePercent.reserve = 100;
            this.iAirdropConfig.airdropChancePercent.tarkovStreets = 100;
            this.iAirdropConfig.airdropMinStartTimeSeconds = 5;
            this.iAirdropConfig.airdropMaxStartTimeSeconds = 10;
        }
        // Adjust parameters to make debugging easier
        if (config_json_1.default.debug.enabled) {
            this.commonUtils.logInfo("Applying debug options...");
            if (config_json_1.default.debug.scav_cooldown_time < this.databaseTables.globals.config.SavagePlayCooldown) {
                this.databaseTables.globals.config.SavagePlayCooldown = config_json_1.default.debug.scav_cooldown_time;
            }
            if (config_json_1.default.debug.free_labs_access) {
                this.databaseTables.locations.laboratory.base.AccessKeys = [];
                this.databaseTables.locations.laboratory.base.DisabledForScav = false;
            }
            if (config_json_1.default.debug.full_length_scav_raids) {
                this.forceFullLengthScavRaids();
            }
        }
    }
    postAkiLoad(container) {
        if (!config_json_1.default.enabled) {
            this.commonUtils.logInfo("Mod disabled in config.json", true);
            return;
        }
        this.removeBlacklistedBrainTypes();
        // If we find SWAG or MOAR, disable initial spawns
        const preAkiModLoader = container.resolve("PreAkiModLoader");
        if (config_json_1.default.initial_PMC_spawns.enabled && preAkiModLoader.getImportedModsNames().includes("SWAG")) {
            this.commonUtils.logWarning("SWAG Detected. Disabling initial PMC spawns.");
            config_json_1.default.initial_PMC_spawns.enabled = false;
        }
        if (config_json_1.default.initial_PMC_spawns.enabled && preAkiModLoader.getImportedModsNames().includes("DewardianDev-MOAR")) {
            this.commonUtils.logWarning("MOAR Detected. Disabling initial PMC spawns.");
            config_json_1.default.initial_PMC_spawns.enabled = false;
        }
        if (preAkiModLoader.getImportedModsNames().includes("Andrudis-QuestManiac")) {
            this.commonUtils.logWarning("QuestManiac Detected. This mod is known to cause performance issues when used with QuestingBots. No support will be provided.");
        }
        if (!config_json_1.default.initial_PMC_spawns.enabled) {
            return;
        }
        this.commonUtils.logInfo("Configuring game for initial PMC spawns...");
        // Store the current PMC-conversion chances in case they need to be restored later
        this.setOriginalPMCConversionChances();
        // Currently these are all PMC waves, which are unnecessary with PMC spawns in this mod
        this.disableCustomBossWaves();
        // If Rogues don't spawn immediately, PMC spawns will be significantly delayed
        this.iLocationConfig.rogueLighthouseSpawnTimeSettings.waitTimeSeconds = -1;
        this.increaseBotCaps();
        this.commonUtils.logInfo("Configuring game for initial PMC spawns...done.");
    }
    updateScavTimer(sessionId) {
        const pmcData = this.profileHelper.getPmcProfile(sessionId);
        const scavData = this.profileHelper.getScavProfile(sessionId);
        if ((scavData.Info === null) || (scavData.Info === undefined)) {
            this.commonUtils.logInfo("Scav profile hasn't been created yet.");
            return;
        }
        // In case somebody disables scav runs and later wants to enable them, we need to reset their Scav timer unless it's plausible
        const worstCooldownFactor = this.getWorstSavageCooldownModifier();
        if (scavData.Info.SavageLockTime - pmcData.Info.LastTimePlayedAsSavage > this.databaseTables.globals.config.SavagePlayCooldown * worstCooldownFactor * 1.1) {
            this.commonUtils.logInfo(`Resetting scav timer for sessionId=${sessionId}...`);
            scavData.Info.SavageLockTime = 0;
        }
    }
    // Return the highest Scav cooldown factor from Fence's rep levels
    getWorstSavageCooldownModifier() {
        // Initialize the return value at something very low
        let worstCooldownFactor = 0.01;
        for (const level in this.databaseTables.globals.config.FenceSettings.Levels) {
            if (this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier > worstCooldownFactor)
                worstCooldownFactor = this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier;
        }
        return worstCooldownFactor;
    }
    setOriginalPMCConversionChances() {
        // Store the default PMC-conversion chances for each bot type defined in SPT's configuration file
        let logMessage = "";
        for (const pmcType in this.iPmcConfig.convertIntoPmcChance) {
            if (this.convertIntoPmcChanceOrig[pmcType] !== undefined) {
                logMessage += `${pmcType}: already buffered, `;
                continue;
            }
            const chances = {
                min: this.iPmcConfig.convertIntoPmcChance[pmcType].min,
                max: this.iPmcConfig.convertIntoPmcChance[pmcType].max
            };
            this.convertIntoPmcChanceOrig[pmcType] = chances;
            logMessage += `${pmcType}: ${chances.min}-${chances.max}%, `;
        }
        this.commonUtils.logInfo(`Reading default PMC spawn chances: ${logMessage}`);
    }
    adjustPmcConversionChance(scalingFactor, verify) {
        // Adjust the chances for each applicable bot type
        let logMessage = "";
        let verified = true;
        for (const pmcType in this.iPmcConfig.convertIntoPmcChance) {
            // Do not allow the chances to exceed 100%. Who knows what might happen...
            const min = Math.round(Math.min(100, this.convertIntoPmcChanceOrig[pmcType].min * scalingFactor));
            const max = Math.round(Math.min(100, this.convertIntoPmcChanceOrig[pmcType].max * scalingFactor));
            if (verify) {
                if (this.iPmcConfig.convertIntoPmcChance[pmcType].min != min) {
                    verified = false;
                    break;
                }
                if (this.iPmcConfig.convertIntoPmcChance[pmcType].max != max) {
                    verified = false;
                    break;
                }
            }
            else {
                this.iPmcConfig.convertIntoPmcChance[pmcType].min = min;
                this.iPmcConfig.convertIntoPmcChance[pmcType].max = max;
                logMessage += `${pmcType}: ${min}-${max}%, `;
            }
        }
        if (!verify) {
            this.commonUtils.logInfo(`Adjusting PMC spawn chances (${scalingFactor}): ${logMessage}`);
        }
        if (!verified) {
            this.commonUtils.logError("Another mod has changed the PMC conversion chances. This mod may not work properly!");
        }
    }
    disableCustomBossWaves() {
        this.commonUtils.logInfo("Disabling custom boss waves...");
        this.iLocationConfig.customWaves.boss = {};
    }
    increaseBotCaps() {
        if (!config_json_1.default.initial_PMC_spawns.add_max_players_to_bot_cap) {
            return;
        }
        const maxAddtlBots = config_json_1.default.initial_PMC_spawns.max_additional_bots;
        const maxTotalBots = config_json_1.default.initial_PMC_spawns.max_total_bots;
        this.iBotConfig.maxBotCap["factory4_day"] = Math.min(this.iBotConfig.maxBotCap["factory4_day"] + Math.min(this.databaseTables.locations.factory4_day.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["factory4_night"] = Math.min(this.iBotConfig.maxBotCap["factory4_night"] + Math.min(this.databaseTables.locations.factory4_night.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["bigmap"] = Math.min(this.iBotConfig.maxBotCap["bigmap"] + Math.min(this.databaseTables.locations.bigmap.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["woods"] = Math.min(this.iBotConfig.maxBotCap["woods"] + Math.min(this.databaseTables.locations.woods.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["shoreline"] = Math.min(this.iBotConfig.maxBotCap["shoreline"] + Math.min(this.databaseTables.locations.shoreline.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["lighthouse"] = Math.min(this.iBotConfig.maxBotCap["lighthouse"] + Math.min(this.databaseTables.locations.lighthouse.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["rezervbase"] = Math.min(this.iBotConfig.maxBotCap["rezervbase"] + Math.min(this.databaseTables.locations.rezervbase.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["interchange"] = Math.min(this.iBotConfig.maxBotCap["interchange"] + Math.min(this.databaseTables.locations.interchange.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["laboratory"] = Math.min(this.iBotConfig.maxBotCap["laboratory"] + Math.min(this.databaseTables.locations.laboratory.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["tarkovstreets"] = Math.min(this.iBotConfig.maxBotCap["tarkovstreets"] + Math.min(this.databaseTables.locations.tarkovstreets.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap["default"] = Math.min(this.iBotConfig.maxBotCap["default"] + maxAddtlBots, maxTotalBots);
        for (const location in this.iBotConfig.maxBotCap) {
            this.commonUtils.logInfo(`Changed bot cap for ${location} to: ${this.iBotConfig.maxBotCap[location]}`);
        }
    }
    removeBlacklistedBrainTypes() {
        const badBrains = config_json_1.default.initial_PMC_spawns.blacklisted_pmc_bot_brains;
        this.commonUtils.logInfo("Removing blacklisted brain types from being used for PMC's...");
        let removedBrains = 0;
        for (const pmcType in this.iPmcConfig.pmcType) {
            for (const map in this.iPmcConfig.pmcType[pmcType]) {
                const mapBrains = this.iPmcConfig.pmcType[pmcType][map];
                for (const i in badBrains) {
                    if (mapBrains[badBrains[i]] === undefined) {
                        continue;
                    }
                    //this.commonUtils.logInfo(`Removing ${badBrains[i]} from ${pmcType} in ${map}...`);
                    delete mapBrains[badBrains[i]];
                    removedBrains++;
                }
            }
        }
        this.commonUtils.logInfo(`Removing blacklisted brain types from being used for PMC's...done. Removed entries: ${removedBrains}`);
    }
    forceFullLengthScavRaids() {
        this.commonUtils.logInfo("Forcing full-length Scav raids...");
        for (const map in this.iLocationConfig.scavRaidTimeSettings.maps) {
            this.iLocationConfig.scavRaidTimeSettings.maps[map].reducedChancePercent = 0;
        }
    }
}
module.exports = { mod: new QuestingBots() };
//# sourceMappingURL=mod.js.map