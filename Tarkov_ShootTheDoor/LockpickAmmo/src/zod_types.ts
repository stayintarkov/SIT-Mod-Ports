
import { z } from "zod";

import { Money } from "@spt-aki/models/enums/Money";
import { Traders } from "@spt-aki/models/enums/Traders";

import { Ammo12Gauge } from "@spt-aki/models/enums/AmmoTypes";


export const LOCKPICK_AMMO_TAG = "#Lockpick"
export const TIER1_LOCKPICK_TAG = "#TIER1"
export const TIER2_LOCKPICK_TAG = "#TIER2"
export const TIER3_LOCKPICK_TAG = "#TIER3"

export const MoneyEnum = z.nativeEnum(Money);
export type MoneyEnum = z.infer<typeof MoneyEnum>;

export const TradersEnum = z.nativeEnum(Traders);
export type TradersEnum = z.infer<typeof TradersEnum>;

// Not sure about supported values. 
// For now use values from https://hub.sp-tarkov.com/doc/entry/7-resources-item-properties-list/
export const BackgroundColourEnum = z.enum([
    "blue",
    "yellow",
    "green",
    "red",
    "black",
    "grey",
    "violet",
    "orange",
    "tracerYellow",
    "tracerGreen",
    "tracerRed"
]);
export type BackgroundColourEnum = z.infer<typeof BackgroundColourEnum>;

export class Defaults {
    static AMMO_PRICE = 15000
    static CURRENCIE = Money.ROUBLES
    static TRADER = Traders.MECHANIC
    static LOYALITY_LEVEL = 1
    static COUNT = 5
    static ITEM_PRICE: ItemPrice = { value: Defaults.AMMO_PRICE, currencie: Defaults.CURRENCIE }
    static DAMAGE = 10
    static ARMOR_DAMAGE = 1
    static PENETRATION_POWER = 1
    static PENETRATION_CHANCE = 0.2
    static PENETRATION_POWER_DIVIATION = 1
    static MISSFIRE_CHANCE = 0.3
    static MALFUNCTIONING_MISFIRE_CHANCE = 0.3
    static MALFUNCTIONING_FEED_CHANCE = 0.3
    static DURABILITY_BURN_MODIFICATOR = 10.0
    static BACKGOUND_COLOR = BackgroundColourEnum.enum.violet
    static ORIGINAL_AMMO_ID = Ammo12Gauge.LEAD_SLUG
    static ID = `${LOCKPICK_AMMO_TAG} ${TIER1_LOCKPICK_TAG} default ammo`
    static SHORT_NAME = "Lockpick default ammo"
    static NAME = "Lockpick default ammo"
    static DESCRIPTION = "Lockpick default ammo description"
    static PRICE_IN_HANDBOOK = Defaults.AMMO_PRICE
    static AMMO_OPTIONS: AmmoOptions = {
        damage: Defaults.DAMAGE,
        armor_damage: Defaults.ARMOR_DAMAGE,
        penetration_power: Defaults.PENETRATION_POWER,
        penetration_chance: Defaults.PENETRATION_CHANCE,
        penetration_power_diviation: Defaults.PENETRATION_POWER_DIVIATION,
        missfire_chance: Defaults.MISSFIRE_CHANCE,
        malfunctioning_misfire_chance: Defaults.MALFUNCTIONING_MISFIRE_CHANCE,
        malfunctioning_feed_chance: Defaults.MALFUNCTIONING_FEED_CHANCE,
        durability_burn_modificator: Defaults.DURABILITY_BURN_MODIFICATOR,
        backgound_color: Defaults.BACKGOUND_COLOR
    }
    static TRADERS_OPTIONS: TraderOptions[] = [{
        trader: Defaults.TRADER,
        count: Defaults.COUNT,
        price: Defaults.ITEM_PRICE,
        loyalty_level: Defaults.LOYALITY_LEVEL
    }]
}

export const ItemPrice = z.object({
    value: z.number().default(Defaults.AMMO_PRICE),
    currencie: MoneyEnum.default(Defaults.CURRENCIE),
}).required();
export type ItemPrice = z.infer<typeof ItemPrice>;

export const TraderOptions = z.object({
    trader: TradersEnum.default(Defaults.TRADER),
    count: z.number().min(0).default(Defaults.COUNT),
    price: ItemPrice.default(Defaults.ITEM_PRICE),
    loyalty_level: z.number().min(1).max(4).default(Defaults.LOYALITY_LEVEL)
});
export type TraderOptions = z.infer<typeof TraderOptions>;

export const AmmoOptions = z.object({
    damage: z.number().positive().default(Defaults.DAMAGE),
    armor_damage: z.number().positive().default(Defaults.ARMOR_DAMAGE),

    // This is the actual penetration value of a round, 10 pens class 1 armor, 50 pens class 5. So on and so forth.
    penetration_power: z.number().nonnegative().int().default(Defaults.PENETRATION_POWER),
    // Not sure what this does
    penetration_chance: z.number().min(0).max(1).default(Defaults.PENETRATION_CHANCE),
    // Also not sure about this
    penetration_power_diviation: z.number().default(Defaults.PENETRATION_POWER_DIVIATION),

    // Chance of the round misfiring in the chamber, 0 is pretty much no chance where as 1 is high chance.
    missfire_chance: z.number().min(0).max(1).default(Defaults.MISSFIRE_CHANCE),
    // Chance of the round malfunctioning in the weapon and causing a jam after being fired, 0 is pretty much no chance where as 1 is high chance.
    malfunctioning_misfire_chance: z.number().min(0).max(1).default(Defaults.MALFUNCTIONING_MISFIRE_CHANCE),
    // Chance of the round malfunctioning in the weapon and failing to feed, 0 is pretty much no chance where as 1 is high chance.
    malfunctioning_feed_chance: z.number().min(0).max(1).default(Defaults.MALFUNCTIONING_FEED_CHANCE),
    //  A value of 1 means that the round doesn't have an increased or decreased burn modifier, 0.9 means that the round will have a -10% burn modifier where as 1.1 has a +10% burn modifier. So on and so forth. Lower value is better for the weapon.
    durability_burn_modificator: z.number().positive().default(Defaults.DURABILITY_BURN_MODIFICATOR),

    backgound_color: BackgroundColourEnum.default(Defaults.BACKGOUND_COLOR)
});
export type AmmoOptions = z.infer<typeof AmmoOptions>;

export const LockpickAmmoTemplate = z.object({
    original_ammo_id: z.string().default(Defaults.ORIGINAL_AMMO_ID),
    id: z.string().startsWith(LOCKPICK_AMMO_TAG).default(Defaults.ID),
    short_name: z.string().default(Defaults.SHORT_NAME),
    name: z.string().default(Defaults.NAME),
    description: z.string().default(Defaults.DESCRIPTION),
    price_in_handbook: z.number().nonnegative().default(Defaults.AMMO_PRICE),
    ammo_options: AmmoOptions.default(Defaults.AMMO_OPTIONS),
    traders_options: z.array(TraderOptions).default(Defaults.TRADERS_OPTIONS)
});
export type LockpickAmmoTemplate = z.infer<typeof LockpickAmmoTemplate>;

export const LockpickAmmoModConfig = z.object({
    lockpick_ammo_mod: z.object({
        lockpick_ammo: z.array(LockpickAmmoTemplate)
    })
})
export type LockpickAmmoModConfig = z.infer<typeof LockpickAmmoModConfig>;