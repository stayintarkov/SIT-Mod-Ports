"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.CommonUtils = void 0;
const config_json_1 = __importDefault(require("../config/config.json"));
class CommonUtils {
    logger;
    databaseTables;
    localeService;
    debugMessagePrefix = "[Questing Bots] ";
    translations;
    constructor(logger, databaseTables, localeService) {
        this.logger = logger;
        this.databaseTables = databaseTables;
        this.localeService = localeService;
        // Get all translations for the current locale
        this.translations = this.localeService.getLocaleDb();
    }
    logInfo(message, alwaysShow = false) {
        if (config_json_1.default.enabled || alwaysShow)
            this.logger.info(this.debugMessagePrefix + message);
    }
    logWarning(message) {
        this.logger.warning(this.debugMessagePrefix + message);
    }
    logError(message) {
        this.logger.error(this.debugMessagePrefix + message);
    }
    getItemName(itemID) {
        const translationKey = itemID + " Name";
        if (translationKey in this.translations)
            return this.translations[translationKey];
        // If a key can't be found in the translations dictionary, fall back to the template data if possible
        if (!(itemID in this.databaseTables.templates.items)) {
            return undefined;
        }
        const item = this.databaseTables.templates.items[itemID];
        return item._name;
    }
}
exports.CommonUtils = CommonUtils;
//# sourceMappingURL=CommonUtils.js.map