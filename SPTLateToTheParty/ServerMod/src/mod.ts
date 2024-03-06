/* eslint-disable @typescript-eslint/naming-convention */
import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";
import { BotConversionHelper } from "./BotConversionHelper";
import { LootRankingGenerator } from "./LootRankingGenerator";
import { TraderAssortGenerator } from "./TraderAssortGenerator";

import { DependencyContainer } from "tsyringe";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import type { IPostAkiLoadMod } from "@spt-aki/models/external/IPostAkiLoadMod";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import type { DynamicRouterModService } from "@spt-aki/services/mod/dynamicRouter/DynamicRouterModService";

import type { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { ConfigServer } from "@spt-aki/servers/ConfigServer";
import { ILocationConfig, LootMultiplier } from "@spt-aki/models/spt/config/ILocationConfig";
import { IInRaidConfig } from "@spt-aki/models/spt/config/IInRaidConfig";
import { IBotConfig } from "@spt-aki/models/spt/config/IBotConfig";
import { IPmcConfig } from "@spt-aki/models/spt/config/IPmcConfig";
import { IAirdropConfig } from "@spt-aki/models/spt/config/IAirdropConfig";
import { ITraderConfig } from "@spt-aki/models/spt/config/ITraderConfig";
import { ConfigTypes } from "@spt-aki/models/enums/ConfigTypes";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";
import { LocaleService } from "@spt-aki/services/LocaleService";
import { BotWeaponGenerator } from "@spt-aki/generators/BotWeaponGenerator";
import { HashUtil } from "@spt-aki/utils/HashUtil";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
import { TimeUtil } from "@spt-aki/utils/TimeUtil";
import { RandomUtil } from "@spt-aki/utils/RandomUtil";
import { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";
import { TraderController } from "@spt-aki/controllers/TraderController";
import { FenceService } from "@spt-aki/services/FenceService";
import { FenceBaseAssortGenerator } from "@spt-aki/generators/FenceBaseAssortGenerator";
import { RagfairOfferGenerator } from "@spt-aki/generators/RagfairOfferGenerator";
import { RagfairOfferService } from "@spt-aki/services/RagfairOfferService";
import { RagfairController } from "@spt-aki/controllers/RagfairController";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { Traders } from "@spt-aki/models/enums/Traders";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { IGetOffersResult } from "@spt-aki/models/eft/ragfair/IGetOffersResult";
import { ISearchRequestData } from "@spt-aki/models/eft/ragfair/ISearchRequestData";

const modName = "LateToTheParty";

class LateToTheParty implements IPreAkiLoadMod, IPostDBLoadMod, IPostAkiLoadMod
{
    private commonUtils: CommonUtils
    private botConversionHelper: BotConversionHelper
    private lootRankingGenerator: LootRankingGenerator
    private traderAssortGenerator: TraderAssortGenerator
    
    private logger: ILogger;
    private locationConfig: ILocationConfig;
    private inRaidConfig: IInRaidConfig;
    private iBotConfig: IBotConfig;
    private iPmcConfig: IPmcConfig;
    private iAirdropConfig: IAirdropConfig;
    private iTraderConfig: ITraderConfig;
    private configServer: ConfigServer;
    private databaseServer: DatabaseServer;
    private databaseTables: IDatabaseTables;
    private vfs: VFS;
    private localeService: LocaleService;
    private botWeaponGenerator: BotWeaponGenerator;
    private hashUtil: HashUtil;
    private jsonUtil: JsonUtil;
    private timeutil: TimeUtil;
    private randomUtil: RandomUtil;
    private profileHelper: ProfileHelper;
    private httpResponseUtil: HttpResponseUtil;
    private fenceService: FenceService;
    private traderController: TraderController;
    private fenceBaseAssortGenerator: FenceBaseAssortGenerator;
    private ragfairOfferGenerator: RagfairOfferGenerator;
    private ragfairOfferService: RagfairOfferService;
    private ragfairController: RagfairController;

    private originalLooseLootMultipliers : LootMultiplier
    private originalStaticLootMultipliers : LootMultiplier
	
    public preAkiLoad(container: DependencyContainer): void
    {
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const dynamicRouterModService = container.resolve<DynamicRouterModService>("DynamicRouterModService");
        this.logger = container.resolve<ILogger>("WinstonLogger");

        // Get config.json settings for the bepinex plugin
        staticRouterModService.registerStaticRouter(`StaticGetConfig${modName}`,
            [{
                url: "/LateToTheParty/GetConfig",
                action: () => 
                {
                    return JSON.stringify(modConfig);
                }
            }], "GetConfig"
        );

        // Get the logging directory for bepinex crash reports
        staticRouterModService.registerStaticRouter(`StaticGetLoggingPath${modName}`,
            [{
                url: "/LateToTheParty/GetLoggingPath",
                action: () => 
                {
                    return JSON.stringify({ path: `${__dirname}/../log/` });
                }
            }], "GetLoggingPath"
        );

        // Report error messages to the SPT-AKI server console in case the user hasn't enabled the bepinex console
        dynamicRouterModService.registerDynamicRouter(`DynamicReportError${modName}`,
            [{
                url: "/LateToTheParty/ReportError/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const errorMessage = urlParts[urlParts.length - 1];

                    const regex = /%20/g;
                    this.commonUtils.logError(errorMessage.replace(regex, " "));

                    return JSON.stringify({ resp: "OK" });
                }
            }], "ReportError"
        );

        if (!modConfig.enabled)
        {
            return;
        }

        // Game start
        // Needed to initialize bot conversion helper instance and loot ranking generator after any other mods have potentially changed config settings
        staticRouterModService.registerStaticRouter(`StaticAkiGameStart${modName}`,
            [{
                url: "/client/game/start",
                // biome-ignore lint/suspicious/noExplicitAny: <explanation>
                action: (url: string, info: any, sessionId: string, output: string) => 
                {
                    // Clear any cached trader inventory data if the game is restarted
                    if (modConfig.trader_stock_changes.enabled)
                    {
                        this.traderAssortGenerator.clearLastAssortData();
                    }

                    this.botConversionHelper = new BotConversionHelper(this.commonUtils, this.iPmcConfig);

                    this.generateLootRankingData(sessionId);

                    if (modConfig.debug.enabled)
                    {
                        this.updateScavTimer(sessionId);
                    }

                    return output;
                }
            }], "aki"
        );

        // Update trader inventory
        dynamicRouterModService.registerDynamicRouter(`DynamicGetTraderAssort${modName}`,
            [{
                url: "/client/trading/api/getTraderAssort/",
                // biome-ignore lint/suspicious/noExplicitAny: <explanation>
                action: (url: string, info: any, sessionId: string, output: string) => 
                {
                    if (!modConfig.trader_stock_changes.enabled)
                    {
                        return output;
                    }

                    const traderID = url.replace("/client/trading/api/getTraderAssort/", "");
                    const assort = this.getUpdatedTraderAssort(traderID, sessionId);
                    return this.httpResponseUtil.getBody(assort);
                }
            }], "aki"
        );

        // Search flea offers
        // Needed to adjust flea offers to match trader stock
        staticRouterModService.registerStaticRouter(`StaticAkiSearchRagfair${modName}`,
            [{
                url: "/client/ragfair/find",
                // biome-ignore lint/suspicious/noExplicitAny: <explanation>
                action: (url: string, info: any, sessionId: string, output: string) => 
                {
                    if (!modConfig.trader_stock_changes.enabled)
                    {
                        return output;
                    }

                    const offers = this.getRagfairOffersForTraders(info, sessionId);
                    return this.httpResponseUtil.getBody(offers);
                }
            }], "aki"
        );

        // Game end
        // Needed for disabling the recurring task that modifies PMC-conversion chances
        staticRouterModService.registerStaticRouter(`StaticAkiRaidEnd${modName}`,
            [{
                url: "/client/match/offline/end",
                action: (output: string) => 
                {
                    BotConversionHelper.stopRaidTimer();                    
                    return output;
                }
            }], "aki"
        );
        
        // Get lootRanking.json for loot ranking
        staticRouterModService.registerStaticRouter(`StaticGetLootRankingData${modName}`,
            [{
                url: "/LateToTheParty/GetLootRankingData",
                action: () => 
                {
                    return JSON.stringify(this.lootRankingGenerator.getLootRankingDataFromFile());
                }
            }], "GetLootRankingData"
        );

        // Get an array of all car extract names
        staticRouterModService.registerStaticRouter(`StaticGetCarExtractNames${modName}`,
            [{
                url: "/LateToTheParty/GetCarExtractNames",
                action: () => 
                {
                    return JSON.stringify(this.inRaidConfig.carExtracts);
                }
            }], "GetCarExtractNames"
        );

        // Adjust the static and loose loot multipliers
        dynamicRouterModService.registerDynamicRouter(`DynamicSetLootMultipliers${modName}`,
            [{
                url: "/LateToTheParty/SetLootMultiplier/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const factor = Number(urlParts[urlParts.length - 1]);

                    this.setLootMultipliers(factor);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "SetLootMultiplier"
        );

        // Sets the escape time for the map and the current time remaining
        dynamicRouterModService.registerDynamicRouter(`DynamicSetEscapeTime${modName}`,
            [{
                url: "/LateToTheParty/EscapeTime/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const escapeTime = Number(urlParts[urlParts.length - 2]);
                    const timeRemaining = Number(urlParts[urlParts.length - 1]);
                    
                    this.botConversionHelper.setEscapeTime(escapeTime, timeRemaining);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "SetEscapeTime"
        );

        // Needed so trader assorts just unlocked by a quest aren't sold out immediately
        dynamicRouterModService.registerDynamicRouter(`DynamicAddRecentlyChangedQuest${modName}`,
            [{
                url: "/LateToTheParty/QuestStatusChange/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const questID = urlParts[urlParts.length - 2];
                    //const newStatus = urlParts[urlParts.length - 1];

                    if (modConfig.trader_stock_changes.enabled)
                    {
                        this.traderAssortGenerator.addRecentlyChangedQuest(questID);
                    }
                    
                    return JSON.stringify({ resp: "OK" });
                }
            }], "AddRecentlyChangedQuest"
        );
    }

    public postDBLoad(container: DependencyContainer): void
    {
        this.configServer = container.resolve<ConfigServer>("ConfigServer");
        this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.vfs = container.resolve<VFS>("VFS");
        this.localeService = container.resolve<LocaleService>("LocaleService");
        this.botWeaponGenerator = container.resolve<BotWeaponGenerator>("BotWeaponGenerator");
        this.hashUtil = container.resolve<HashUtil>("HashUtil");
        this.jsonUtil = container.resolve<JsonUtil>("JsonUtil");
        this.timeutil = container.resolve<TimeUtil>("TimeUtil");
        this.randomUtil = container.resolve<RandomUtil>("RandomUtil");
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
        this.traderController = container.resolve<TraderController>("TraderController");
        this.fenceService = container.resolve<FenceService>("FenceService");
        this.fenceBaseAssortGenerator = container.resolve<FenceBaseAssortGenerator>("FenceBaseAssortGenerator");
        this.ragfairOfferGenerator = container.resolve<RagfairOfferGenerator>("RagfairOfferGenerator");
        this.ragfairOfferService = container.resolve<RagfairOfferService>("RagfairOfferService");
        this.ragfairController = container.resolve<RagfairController>("RagfairController");

        this.locationConfig = this.configServer.getConfig(ConfigTypes.LOCATION);
        this.inRaidConfig = this.configServer.getConfig(ConfigTypes.IN_RAID);
        this.iBotConfig = this.configServer.getConfig(ConfigTypes.BOT);
        this.iPmcConfig = this.configServer.getConfig(ConfigTypes.PMC);
        this.iAirdropConfig = this.configServer.getConfig(ConfigTypes.AIRDROP);
        this.iTraderConfig = this.configServer.getConfig(ConfigTypes.TRADER);

        this.databaseTables = this.databaseServer.getTables();
        this.commonUtils = new CommonUtils(this.logger, this.databaseTables, this.localeService);
        
        if (!modConfig.enabled)
        {
            this.commonUtils.logInfo("Mod disabled in config.json.");
            return;
        }

        this.adjustSPTScavRaidChanges();
        
        // Adjust parameters to make debugging easier
        if (modConfig.debug.enabled)
        {
            this.commonUtils.logInfo("Applying debug options...");

            if (modConfig.debug.scav_cooldown_time < this.databaseTables.globals.config.SavagePlayCooldown)
            {
                this.databaseTables.globals.config.SavagePlayCooldown = modConfig.debug.scav_cooldown_time;
            }

            if (modConfig.debug.free_labs_access)
            {
                this.databaseTables.locations.laboratory.base.AccessKeys = [];
                this.databaseTables.locations.laboratory.base.DisabledForScav = false;
            }

            this.databaseTables.globals.config.RagFair.minUserLevel = modConfig.debug.min_level_for_flea;

            //this.iAirdropConfig.airdropChancePercent.bigmap = 100;
            //this.iAirdropConfig.airdropChancePercent.woods = 100;
            //this.iAirdropConfig.airdropChancePercent.lighthouse = 100;
            //this.iAirdropConfig.airdropChancePercent.shoreline = 100;
            //this.iAirdropConfig.airdropChancePercent.interchange = 100;
            //this.iAirdropConfig.airdropChancePercent.reserve = 100;
            //this.iAirdropConfig.airdropChancePercent.tarkovStreets = 100;

            // Modify trader restock times
            for (const t in this.iTraderConfig.updateTime)
            {
                this.iTraderConfig.updateTime[t].seconds *= modConfig.debug.trader_resupply_time_factor;
                const maxResupplyTime = this.timeutil.getTimestamp() + this.iTraderConfig.updateTime[t].seconds;

                // This is undefined for some trader mods
                if (!(this.iTraderConfig.updateTime[t].traderId in this.databaseTables.traders))
                {
                    continue;
                }

                if (this.databaseTables.traders[this.iTraderConfig.updateTime[t].traderId].base.nextResupply > maxResupplyTime)
                {
                    this.databaseTables.traders[this.iTraderConfig.updateTime[t].traderId].base.nextResupply = maxResupplyTime;
                }
            }
        }
    }

    public postAkiLoad(): void
    {
        if (!modConfig.enabled)
        {
            return;
        }

        // Store the original static and loose loot multipliers
        this.getLootMultipliers();

        // Initialize trader assort data
        if (modConfig.trader_stock_changes.enabled)
        {
            this.traderAssortGenerator = new TraderAssortGenerator(
                this.commonUtils,
                this.databaseTables,
                this.jsonUtil,
                this.fenceService,
                this.fenceBaseAssortGenerator,
                this.ragfairOfferGenerator,
                this.ragfairOfferService,
                this.iTraderConfig,
                this.randomUtil,
                this.timeutil
            );
        }

        
    }

    private adjustSPTScavRaidChanges(): void
    {
        this.commonUtils.logInfo("Adjusting SPT Scav-raid changes...");

        for (const map in this.locationConfig.scavRaidTimeSettings.maps)
        {
            if (modConfig.scav_raid_adjustments.always_spawn_late)
            {
                this.locationConfig.scavRaidTimeSettings.maps[map].reducedChancePercent = 100;
            }

            if (modConfig.destroy_loot_during_raid.enabled)
            {
                this.locationConfig.scavRaidTimeSettings.maps[map].reduceLootByPercent = false;
            }
        }
    }

    private updateScavTimer(sessionId: string): void
    {
        const pmcData = this.profileHelper.getPmcProfile(sessionId);
        const scavData = this.profileHelper.getScavProfile(sessionId);
		
        if ((scavData.Info === null) || (scavData.Info === undefined))
        {
            this.commonUtils.logInfo("Scav profile hasn't been created yet.");
            return;
        }
		
        // In case somebody disables scav runs and later wants to enable them, we need to reset their Scav timer unless it's plausible
        const worstCooldownFactor = this.getWorstSavageCooldownModifier();
        if (scavData.Info.SavageLockTime - pmcData.Info.LastTimePlayedAsSavage > this.databaseTables.globals.config.SavagePlayCooldown * worstCooldownFactor * 1.1)
        {
            this.commonUtils.logInfo(`Resetting scav timer for sessionId=${sessionId}...`);
            scavData.Info.SavageLockTime = 0;
        }
    }
	
    // Return the highest Scav cooldown factor from Fence's rep levels
    private getWorstSavageCooldownModifier(): number
    {
        // Initialize the return value at something very low
        let worstCooldownFactor = 0.01;

        for (const level in this.databaseTables.globals.config.FenceSettings.Levels)
        {
            if (this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier > worstCooldownFactor)
                worstCooldownFactor = this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier;
        }
        return worstCooldownFactor;
    }

    private getLootMultipliers(): void
    {
        this.originalLooseLootMultipliers = 
        {
            bigmap: this.locationConfig.looseLootMultiplier.bigmap,
            develop: this.locationConfig.looseLootMultiplier.develop,
            factory4_day: this.locationConfig.looseLootMultiplier.factory4_day,
            factory4_night: this.locationConfig.looseLootMultiplier.factory4_night,
            hideout: this.locationConfig.looseLootMultiplier.hideout,
            interchange: this.locationConfig.looseLootMultiplier.interchange,
            laboratory: this.locationConfig.looseLootMultiplier.laboratory,
            lighthouse: this.locationConfig.looseLootMultiplier.lighthouse,
            privatearea: this.locationConfig.looseLootMultiplier.privatearea,
            rezervbase: this.locationConfig.looseLootMultiplier.rezervbase,
            shoreline: this.locationConfig.looseLootMultiplier.shoreline,
            suburbs: this.locationConfig.looseLootMultiplier.suburbs,
            tarkovstreets: this.locationConfig.looseLootMultiplier.tarkovstreets,
            terminal: this.locationConfig.looseLootMultiplier.terminal,
            town: this.locationConfig.looseLootMultiplier.town,
            woods: this.locationConfig.looseLootMultiplier.woods
        }

        this.originalStaticLootMultipliers = 
        {
            bigmap: this.locationConfig.staticLootMultiplier.bigmap,
            develop: this.locationConfig.staticLootMultiplier.develop,
            factory4_day: this.locationConfig.staticLootMultiplier.factory4_day,
            factory4_night: this.locationConfig.staticLootMultiplier.factory4_night,
            hideout: this.locationConfig.staticLootMultiplier.hideout,
            interchange: this.locationConfig.staticLootMultiplier.interchange,
            laboratory: this.locationConfig.staticLootMultiplier.laboratory,
            lighthouse: this.locationConfig.staticLootMultiplier.lighthouse,
            privatearea: this.locationConfig.staticLootMultiplier.privatearea,
            rezervbase: this.locationConfig.staticLootMultiplier.rezervbase,
            shoreline: this.locationConfig.staticLootMultiplier.shoreline,
            suburbs: this.locationConfig.staticLootMultiplier.suburbs,
            tarkovstreets: this.locationConfig.staticLootMultiplier.tarkovstreets,
            terminal: this.locationConfig.staticLootMultiplier.terminal,
            town: this.locationConfig.staticLootMultiplier.town,
            woods: this.locationConfig.staticLootMultiplier.woods
        }
    }

    private setLootMultipliers(factor: number): void
    {
        this.commonUtils.logInfo(`Adjusting loot multipliers by a factor of ${factor}...`);

        this.locationConfig.looseLootMultiplier.bigmap = this.originalLooseLootMultipliers.bigmap * factor;
        this.locationConfig.looseLootMultiplier.develop = this.originalLooseLootMultipliers.develop * factor;
        this.locationConfig.looseLootMultiplier.factory4_day = this.originalLooseLootMultipliers.factory4_day * factor;
        this.locationConfig.looseLootMultiplier.factory4_night = this.originalLooseLootMultipliers.factory4_night * factor;
        this.locationConfig.looseLootMultiplier.hideout = this.originalLooseLootMultipliers.hideout * factor;
        this.locationConfig.looseLootMultiplier.interchange = this.originalLooseLootMultipliers.interchange * factor;
        this.locationConfig.looseLootMultiplier.laboratory = this.originalLooseLootMultipliers.laboratory * factor;
        this.locationConfig.looseLootMultiplier.lighthouse = this.originalLooseLootMultipliers.lighthouse * factor;
        this.locationConfig.looseLootMultiplier.privatearea = this.originalLooseLootMultipliers.privatearea * factor;
        this.locationConfig.looseLootMultiplier.rezervbase = this.originalLooseLootMultipliers.rezervbase * factor;
        this.locationConfig.looseLootMultiplier.shoreline = this.originalLooseLootMultipliers.shoreline * factor;
        this.locationConfig.looseLootMultiplier.suburbs = this.originalLooseLootMultipliers.suburbs * factor;
        this.locationConfig.looseLootMultiplier.tarkovstreets = this.originalLooseLootMultipliers.tarkovstreets * factor;
        this.locationConfig.looseLootMultiplier.terminal = this.originalLooseLootMultipliers.terminal * factor;
        this.locationConfig.looseLootMultiplier.town = this.originalLooseLootMultipliers.town * factor;
        this.locationConfig.looseLootMultiplier.woods = this.originalLooseLootMultipliers.woods * factor;

        this.locationConfig.staticLootMultiplier.bigmap = this.originalStaticLootMultipliers.bigmap * factor;
        this.locationConfig.staticLootMultiplier.develop = this.originalStaticLootMultipliers.develop * factor;
        this.locationConfig.staticLootMultiplier.factory4_day = this.originalStaticLootMultipliers.factory4_day * factor;
        this.locationConfig.staticLootMultiplier.factory4_night = this.originalStaticLootMultipliers.factory4_night * factor;
        this.locationConfig.staticLootMultiplier.hideout = this.originalStaticLootMultipliers.hideout * factor;
        this.locationConfig.staticLootMultiplier.interchange = this.originalStaticLootMultipliers.interchange * factor;
        this.locationConfig.staticLootMultiplier.laboratory = this.originalStaticLootMultipliers.laboratory * factor;
        this.locationConfig.staticLootMultiplier.lighthouse = this.originalStaticLootMultipliers.lighthouse * factor;
        this.locationConfig.staticLootMultiplier.privatearea = this.originalStaticLootMultipliers.privatearea * factor;
        this.locationConfig.staticLootMultiplier.rezervbase = this.originalStaticLootMultipliers.rezervbase * factor;
        this.locationConfig.staticLootMultiplier.shoreline = this.originalStaticLootMultipliers.shoreline * factor;
        this.locationConfig.staticLootMultiplier.suburbs = this.originalStaticLootMultipliers.suburbs * factor;
        this.locationConfig.staticLootMultiplier.tarkovstreets = this.originalStaticLootMultipliers.tarkovstreets * factor;
        this.locationConfig.staticLootMultiplier.terminal = this.originalStaticLootMultipliers.terminal * factor;
        this.locationConfig.staticLootMultiplier.town = this.originalStaticLootMultipliers.town * factor;
        this.locationConfig.staticLootMultiplier.woods = this.originalStaticLootMultipliers.woods * factor;
    }

    private generateLootRankingData(sessionId: string): void
    {
        this.lootRankingGenerator = new LootRankingGenerator(this.commonUtils, this.databaseTables, this.vfs, this.botWeaponGenerator, this.hashUtil);
        this.lootRankingGenerator.generateLootRankingData(sessionId);
    }

    private getUpdatedTraderAssort(traderID: string, sessionId: string, canRegenerate = true): ITraderAssort
    {
        // Refresh Fence's assorts
        if (traderID === Traders.FENCE)
        {
            if (!modConfig.trader_stock_changes.fence_stock_changes.enabled)
            {
                return this.traderController.getAssort(sessionId, traderID);
            }

            this.traderAssortGenerator.updateFenceAssort();
        }
        
        const pmcProfile = this.profileHelper.getPmcProfile(sessionId);
        const maxLL = pmcProfile.TradersInfo[traderID].loyaltyLevel;

        // Update stock for trader
        const assort = this.traderController.getAssort(sessionId, traderID);
        this.traderAssortGenerator.updateTraderStock(traderID, assort, maxLL, traderID === Traders.FENCE);

        // Remove fancy weapons and then check if Fence's assorts need to be regenerated
        if (traderID === Traders.FENCE)
        {
            this.traderAssortGenerator.adjustFenceAssortItemPrices(assort);
            this.traderAssortGenerator.removeExpensivePresets(assort, modConfig.trader_stock_changes.fence_stock_changes.max_preset_cost);

            if (this.traderAssortGenerator.replenishFenceStockIfNeeded(assort, maxLL) && canRegenerate)
            {
                return this.getUpdatedTraderAssort(traderID, sessionId, false);
            }
        }

        return assort;
    }

    private getRagfairOffersForTraders(info: ISearchRequestData, sessionId: string): IGetOffersResult
    {
        const pmcProfile = this.profileHelper.getPmcProfile(sessionId);

        // Update each trader's inventory if too much time has elapsed since it was last refreshed
        for (const t in this.iTraderConfig.updateTime)
        {
            const traderID = this.iTraderConfig.updateTime[t].traderId;

            // Ignore Fence because his items aren't available on the flea market
            if (traderID === Traders.FENCE)
            {
                continue;
            }

            // This is undefined for some trader mods
            if (!(this.iTraderConfig.updateTime[t].traderId in this.databaseTables.traders))
            {
                continue;
            }

            // Determine how much time has passed since the trader's inventory was last updated, and compare that to the max time allowed
            const resupplyAge = this.timeutil.getTimestamp() - this.traderAssortGenerator.getLastTraderRefreshTimestamp(traderID);
            const resupplyAgeMax = this.iTraderConfig.updateTime[t].seconds * modConfig.trader_stock_changes.ragfair_refresh_time_fraction;

            if (resupplyAge < resupplyAgeMax)
            {
                continue;
            }

            const assort = this.traderController.getAssort(sessionId, traderID);
            this.traderAssortGenerator.updateTraderStock(traderID, assort, pmcProfile.TradersInfo[traderID].loyaltyLevel, traderID === Traders.FENCE);
        }

        // Update all offers to reflect the latest trader inventory
        let offers = this.ragfairController.getOffers(sessionId, info);
        offers = this.traderAssortGenerator.updateFleaOffers(offers);

        return offers;
    }
}
module.exports = {mod: new LateToTheParty()}