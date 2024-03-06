import modConfig from "../config/config.json";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
import { LocaleService } from "@spt-aki/services/LocaleService";
import { Item } from "@spt-aki/models/eft/common/tables/IItem";

export class CommonUtils
{
    private debugMessagePrefix = "[Late to the Party] ";
    private translations: Record<string, string>;
	
    constructor (private logger: ILogger, private databaseTables: IDatabaseTables, private localeService: LocaleService)
    {
        // Get all translations for the current locale
        this.translations = this.localeService.getLocaleDb();
    }
	
    public logInfo(message: string, alwaysShow = false): void
    {
        if (modConfig.debug.enabled || alwaysShow)
            this.logger.info(this.debugMessagePrefix + message);
    }

    public logWarning(message: string): void
    {
        this.logger.warning(this.debugMessagePrefix + message);
    }

    public logError(message: string): void
    {
        this.logger.error(this.debugMessagePrefix + message);
    }

    public getItemName(itemID: string): string
    {
        const translationKey = `${itemID} Name`;
        if (translationKey in this.translations)
            return this.translations[translationKey];
		
        // If a key can't be found in the translations dictionary, fall back to the template data if possible
        if (!(itemID in this.databaseTables.templates.items))
        {
            return undefined;
        }

        const item = this.databaseTables.templates.items[itemID];
        return item._name;
    }

    public getMaxItemPrice(itemID: string): number
    {
        // Get the handbook.json price, if any exists
        const matchingHandbookItems = this.databaseTables.templates.handbook.Items.filter((i) => i.Id === itemID);
        let handbookPrice = 0;
        if (matchingHandbookItems.length === 1)
        {
            handbookPrice = matchingHandbookItems[0].Price;

            // Some mods add a record with a junk value
            if ((handbookPrice == null) || Number.isNaN(handbookPrice))
            {
                this.logWarning(`Invalid handbook price (${handbookPrice}) for ${this.getItemName(itemID)} (${itemID}). Defaulting to 0.`);
                handbookPrice = 0;
            }
        }

        // Get the prices.json price, if any exists
        let price = 0;
        if (itemID in this.databaseTables.templates.prices)
        {
            price = this.databaseTables.templates.prices[itemID];

            // Some mods add a record with a junk value
            if ((price == null) || Number.isNaN(price))
            {
                // Only show a warning if the method will return 0
                if (handbookPrice === 0)
                {
                    this.logWarning(`Invalid price (${price}) for ${this.getItemName(itemID)} (${itemID}). Defaulting to 0.`);
                }

                price = 0;
            }
        }
        
        return Math.max(handbookPrice, price);
    }

    /**
     * Check if @param item is a child of the item with ID @param parentID
     */
    public static hasParent(item: ITemplateItem, parentID: string, databaseTables: IDatabaseTables): boolean
    {
        const allParents = CommonUtils.getAllParents(item, databaseTables);
        return allParents.includes(parentID);
    }

    public static getAllParents(item: ITemplateItem, databaseTables: IDatabaseTables): string[]
    {
        if ((item._parent === null) || (item._parent === undefined) || (item._parent === ""))
            return [];
		
        const allParents = CommonUtils.getAllParents(databaseTables.templates.items[item._parent], databaseTables);
        allParents.push(item._parent);
		
        return allParents;
    }

    public static canItemDegrade(item: Item, databaseTables: IDatabaseTables): boolean
    {
        if (item.upd === undefined)
        {
            return false;
        }

        if ((item.upd.MedKit === undefined) && (item.upd.Repairable === undefined) && (item.upd.Resource === undefined))
        {
            return false;
        }
        
        const itemTpl = databaseTables.templates.items[item._tpl];

        if ((itemTpl._props.armorClass !== undefined) && (itemTpl._props.armorClass.toString() === "0"))
        {
            return false;
        }

        return true;
    }

    public static interpolateForFirstCol(array: number[][], value: number): number
    {
        if (array.length === 1)
        {
            return array[array.length - 1][1];
        }

        if (value <= array[0][0])
        {
            return array[0][1];
        }

        for (let i = 1; i < array.length; i++)
        {
            if (array[i][0] >= value)
            {
                if (array[i][0] - array[i - 1][0] === 0)
                {
                    return array[i][1];
                }

                return array[i - 1][1] + (value - array[i - 1][0]) * (array[i][1] - array[i - 1][1]) / (array[i][0] - array[i - 1][0]);
            }
        }

        return array[array.length - 1][1];
    }
}