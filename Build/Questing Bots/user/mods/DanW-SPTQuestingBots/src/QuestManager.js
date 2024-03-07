"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.QuestManager = void 0;
class QuestManager {
    commonUtils;
    vfs;
    constructor(commonUtils, vfs) {
        this.commonUtils = commonUtils;
        this.vfs = vfs;
    }
    validateCustomQuests() {
        const path = __dirname + "/../quests";
        let totalStandardQuestsAdded = 0;
        let totalCustomQuestsAdded = 0;
        // Ensure the directory for standard quests exists
        if (this.vfs.exists(path + "/standard/")) {
            const standardQuests = this.vfs.getFiles(path + "/standard/");
            for (const i in standardQuests) {
                const questFileText = this.vfs.readFile(path + "/standard/" + standardQuests[i]);
                // If the JSON file can be parsed into a Quest array, assume it's fine
                const quests = JSON.parse(questFileText);
                totalStandardQuestsAdded += quests.length;
                this.commonUtils.logInfo(`Found ${quests.length} standard quest(s) in "/standard/${standardQuests[i]}"`);
            }
            if (totalStandardQuestsAdded == 0) {
                this.commonUtils.logError("No standard quests have been added. Mod files may have been corrupted. Please try reinstalling.");
            }
        }
        else {
            this.commonUtils.logError("The \"user\\mods\\DanW-SPTQuestingBots\\quests\\standard\" directory is missing. Mod files may have been corrupted. Please try reinstalling.");
        }
        // Check if the directory for custom quests exists
        if (this.vfs.exists(path + "/custom/")) {
            const customQuests = this.vfs.getFiles(path + "/custom/");
            for (const i in customQuests) {
                const questFileText = this.vfs.readFile(path + "/custom/" + customQuests[i]);
                // If the JSON file can be parsed into a Quest array, assume it's fine
                const quests = JSON.parse(questFileText);
                totalCustomQuestsAdded += quests.length;
                this.commonUtils.logInfo(`Found ${quests.length} custom quest(s) in "/custom/${customQuests[i]}"`);
            }
            if (totalCustomQuestsAdded == 0) {
                this.commonUtils.logWarning("No custom quests found.");
            }
        }
    }
}
exports.QuestManager = QuestManager;
//# sourceMappingURL=QuestManager.js.map