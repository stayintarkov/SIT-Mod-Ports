import { DependencyContainer } from "tsyringe";
// SPT types
import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { Item } from "@spt-aki/models/eft/common/tables/IItem";
import { SecuredContainers } from "@spt-aki/models/enums/ContainerTypes"
import { LockpickAmmoModConfig, LockpickAmmoTemplate } from "./zod_types";
import ModConfig = require("../config/config.json");
import { ITemplateItem, Slot } from "@spt-aki/models/eft/common/tables/ITemplateItem";


class LockpickAmmoMod implements IPostDBLoadMod {
    private mod: string = "LockpickAmmo"
    private logger: null | ILogger = null
    private jsonUtil: null | JsonUtil = null
    private tables: null | IDatabaseTables = null

    private getTables(): IDatabaseTables {
        if (!this.tables) {
            throw Error(`[${this.mod}]: tables is missing`);
        }
        return this.tables;
    }

    private getItemTamplates() {
        const templates = this.getTables().templates;
        if (!templates) {
            throw Error(`[${this.mod}]: templates is missing`);
        }
        return templates;
    }

    private getItems(): Record<string, ITemplateItem> {
        const items = this.getItemTamplates().items;
        if (!items) {
            throw Error(`[${this.mod}]: items is missing`);
        }
        return items;
    }

    private getLogger(): ILogger {
        if (!this.logger) {
            throw Error(`[${this.mod}]: logger is missing`);
        }
        return this.logger;
    }

    private getLocales() {
        const locales = this.getTables().locales;
        if (!locales) {
            throw Error(`[${this.mod}]: locales is missing`);
        }
        return locales;
    }

    private getHandbook() {
        const handbook = this.getItemTamplates().handbook
        if (!handbook) {
            throw Error(`[${this.mod}]: handbook is missing`);
        }
        return handbook;
    }

    private getTraders() {
        const traders = this.getTables().traders
        if (!traders) {
            throw Error(`[${this.mod}]: traders is missing`);
        }
        return traders;
    }

    /**
     * Majority of trader-related work occurs after the aki database has been loaded but prior to SPT code being run
     * @param container Dependency container
     */
    public postDBLoad(container: DependencyContainer): void {
        this.jsonUtil = container.resolve<JsonUtil>("JsonUtil");
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.tables = databaseServer.getTables();
        this.logger = container.resolve<ILogger>("WinstonLogger");

        this.setupLockpickAmmo()
    }

    private setupLockpickAmmo(): void {
        const parsed_cfg = this.readConfig()
        for (const ammo_template of parsed_cfg.lockpick_ammo_mod.lockpick_ammo) {
            this.createNewAmmo(ammo_template);
        }
    }

    private readConfig(): LockpickAmmoModConfig {
        const parsed_cfg = LockpickAmmoModConfig.parse(ModConfig)
        return parsed_cfg
    }

    private createNewAmmo(new_ammo_template: LockpickAmmoTemplate): void {
        if (this.isItemAlreadyExists(new_ammo_template.id)) {
            this.getLogger().warning(`[${this.mod}] id ${new_ammo_template.id} already exists`);
            return;
        }

        this.createItem(new_ammo_template)
        this.createItemLocale(new_ammo_template)
        this.addItemToHandbook(new_ammo_template)
        this.addItemToTraders(new_ammo_template)
        this.allowIntoSecureContainers(new_ammo_template.id)
        this.makeAmmoUsableWithWeapon(new_ammo_template);
    }

    private isItemAlreadyExists(id: string): boolean {
        const items = this.getItems();
        return id in items;
    }

    private createItem(new_ammo_template: LockpickAmmoTemplate) {
        const items = this.getItems();
        if (!(new_ammo_template.original_ammo_id in items)) {
            this.getLogger().warning(`[${this.mod}] can't find base item ${new_ammo_template.original_ammo_id} in templates`);
            return;
        }
        const base_item = items[new_ammo_template.original_ammo_id]
        const new_item = this.createItemFrom(base_item, new_ammo_template);
        items[new_ammo_template.id] = new_item;
    }

    private createItemFrom(base_item: ITemplateItem, new_ammo_template: LockpickAmmoTemplate): ITemplateItem {
        const new_item = this.jsonUtil!.clone(base_item);
        new_item._id = new_ammo_template.id;
        const new_item_props = new_item._props;
        const ammo_options = new_ammo_template.ammo_options;
        new_item_props.Damage = ammo_options.damage;
        new_item_props.ArmorDamage = ammo_options.armor_damage;
        new_item_props.PenetrationPower = ammo_options.penetration_power;
        new_item_props.PenetrationChance = ammo_options.penetration_chance;
        new_item_props.PenetrationPowerDiviation = ammo_options.penetration_power_diviation;
        new_item_props.MisfireChance = ammo_options.missfire_chance;
        new_item_props.MalfMisfireChance = ammo_options.malfunctioning_misfire_chance;
        new_item_props.MalfFeedChance = ammo_options.malfunctioning_feed_chance;
        new_item_props.DurabilityBurnModificator = ammo_options.durability_burn_modificator;
        new_item_props.BackgroundColor = ammo_options.backgound_color;
        return new_item;
    }

    private createItemLocale(new_ammo_template: LockpickAmmoTemplate) {
        const locales = Object.values(this.getLocales().global);
        for (const locale of locales) {
            locale[`${new_ammo_template.id} Name`] = new_ammo_template.name;
            locale[`${new_ammo_template.id} ShortName`] = new_ammo_template.short_name;
            locale[`${new_ammo_template.id} Description`] = new_ammo_template.description;
        }
    }

    private addItemToHandbook(new_ammo_template: LockpickAmmoTemplate) {
        const handbook = this.getHandbook();
        const base_handbook_item = handbook.Items.find((item) => item.Id == new_ammo_template.original_ammo_id)
        if (!base_handbook_item) {
            this.getLogger().warning(`[${this.mod}] can't find base item ${new_ammo_template.original_ammo_id} in handbook`);
            return;
        }

        handbook.Items.push({
            Id: new_ammo_template.id,
            ParentId: base_handbook_item.ParentId,
            Price: new_ammo_template.price_in_handbook
        });
    }

    private addItemToTraders(new_ammo_template: LockpickAmmoTemplate) {
        const traders = this.getTraders();
        for (const trader_options of new_ammo_template.traders_options) {
            if (!(trader_options.trader in traders)) {
                this.getLogger().warning(`[${this.mod}] can't find trader ${trader_options.trader}`);
                continue;
            }
            const trader = traders[trader_options.trader];
            const trader_assort = trader.assort;

            const PARENT_ID = "hideout" // tutorial says: Should always be "hideout"
            const SLOT_ID = "hideout"
            const newItem: Item = {
                "_id": new_ammo_template.id,
                "_tpl": new_ammo_template.id,
                "parentId": PARENT_ID,
                "slotId": SLOT_ID,
                "upd":
                {
                    "UnlimitedCount": false,
                    "StackObjectsCount": trader_options.count
                }
            }
            trader_assort.items.push(newItem);

            trader_assort.barter_scheme[new_ammo_template.id] = [
                [
                    {
                        "count": trader_options.price.value,
                        "_tpl": trader_options.price.currencie
                    }
                ]
            ];
            trader_assort.loyal_level_items[new_ammo_template.id] = trader_options.loyalty_level;
        }
    }

    private allowIntoSecureContainers(item_id: string): void {
        const items = this.getItems();
        for (const secure_container of [SecuredContainers.ALPHA, SecuredContainers.BETA, SecuredContainers.EPSILON, SecuredContainers.GAMMA, SecuredContainers.KAPPA]) {
            if (!(secure_container in items)) {
                this.getLogger().warning(`[${this.mod}] can't find container ${secure_container}`);
                continue;
            }

            const grids = items[secure_container]._props.Grids
            if (!grids) {
                continue;
            }

            for (const grid of grids) {
                const filters = grid._props.filters
                for (const grid_filter of filters) {
                    const grid_allowed_items = grid_filter.Filter;
                    grid_allowed_items.push(item_id)
                }
            }
        }
    }

    private makeAmmoUsableWithWeapon(new_ammo_template: LockpickAmmoTemplate) {
        const items = this.getItems();
        for (const item_id in items) {
            this.processSingleItem(items[item_id], new_ammo_template)
        }
    }

    private processSingleItem(item_template: ITemplateItem, new_ammo_template: LockpickAmmoTemplate) {
        const slots = item_template._props.Slots;
        if (slots) {
            this.processItemSlots(slots, new_ammo_template);
        }

        const chambers = item_template._props.Chambers;
        if (chambers) {
            this.processItemChambers(chambers, new_ammo_template);
        }

        const cartridges = item_template._props.Cartridges;
        if (cartridges) {
            this.processItemCartridges(cartridges, new_ammo_template);
        }
    }

    private processItemSlots(slots: Slot[], new_ammo_template: LockpickAmmoTemplate) {
        for (const slot of slots) {
            const filters = slot._props.filters;
            for (const slot_filter of filters) {
                const slot_allowed_items = slot_filter.Filter
                if (slot_allowed_items.includes(new_ammo_template.original_ammo_id)) {
                    // Case 1: allowed item is original_ammo_id
                    slot_allowed_items.push(new_ammo_template.id)
                } else {
                    // Case 2: allowed item is kind of ammo magazine
                    this.processSubSlots(slot_allowed_items, new_ammo_template)
                }    
            }   
        }
    }

    private processSubSlots(slot_allowed_items_ids: string[], new_ammo_template: LockpickAmmoTemplate) {
        const items = this.getItems();
        for (const allowed_item_id of slot_allowed_items_ids) {
            if (!(allowed_item_id in items)) {
                continue;
            }
            const allowed_item = items[allowed_item_id];
            this.processSingleItem(allowed_item, new_ammo_template)
        }
    }

    private processItemChambers(chambers: Slot[], new_ammo_template: LockpickAmmoTemplate) {
        for (const chamber of chambers) {
            for (const filter of chamber._props.filters) {
                const chamber_allowed_items = filter.Filter;
                if (chamber_allowed_items.includes(new_ammo_template.original_ammo_id)) {
                    chamber_allowed_items.push(new_ammo_template.id);
                }
            }
        }
    }

    private processItemCartridges(cartridges: Slot[], new_ammo_template: LockpickAmmoTemplate) {
        for (const cartridge of cartridges) {
            for (const filter of cartridge._props.filters) {
                const cartridge_allowed_items = filter.Filter;
                if (cartridge_allowed_items.includes(new_ammo_template.original_ammo_id)) {
                    cartridge_allowed_items.push(new_ammo_template.id);
                }
            }
        }
    }
}

module.exports = { mod: new LockpickAmmoMod() }