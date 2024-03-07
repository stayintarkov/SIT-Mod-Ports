"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
class Mod {
    itemHelper;
    offerService;
    tradeHelper;
    profileHelper;
    saveServer;
    logger;
    preAkiLoad(container) {
        const logger = container.resolve("WinstonLogger");
        this.logger = logger;
        const staticRouterModService = container.resolve("StaticRouterModService");
        //HELPERS
        this.itemHelper = container.resolve("ItemHelper");
        this.offerService = container.resolve("RagfairOfferService");
        this.tradeHelper = container.resolve("TradeHelper");
        this.profileHelper = container.resolve("ProfileHelper");
        this.saveServer = container.resolve("SaveServer");
        // Hook up a new static route
        staticRouterModService.registerStaticRouter("LootValueRoutes", [
            {
                url: "/LootValue/GetItemLowestFleaPrice",
                //info is the payload from client in json
                //output is the response back to client
                action: (url, info, sessionID, output) => {
                    return (JSON.stringify(this.getItemLowestFleaPrice(info.templateId)));
                }
            },
            {
                url: "/LootValue/SellItemToTrader",
                //info is the payload from client in json
                //output is the response back to client
                action: (url, info, sessionID, output) => {
                    let response = this.sellItemToTrader(sessionID, info.ItemId, info.TraderId, info.Price);
                    return (JSON.stringify(response));
                }
            }
        ], "custom-static-LootValueRoutes");
    }
    getItemLowestFleaPrice(templateId) {
        let offers = this.offerService.getOffersOfType(templateId);
        if (offers && offers.length > 0) {
            offers = offers.filter(a => a.user.memberType != 4 //exclude traders
                && a.requirements[0]._tpl == '5449016a4bdc2d6f028b456f' //consider only ruble trades
                && this.itemHelper.getItemQualityModifier(a.items[0]) == 1 //and items with full durability
            );
            if (offers.length > 0)
                return (offers.sort((a, b) => a.summaryCost - b.summaryCost)[0]).summaryCost;
        }
        return null;
    }
    sellItemToTrader(sessionId, itemId, traderId, price) {
        let pmcData = this.profileHelper.getPmcProfile(sessionId);
        if (!pmcData) {
            this.logger.error("pmcData was null");
            return false;
        }
        let item = pmcData.Inventory.items.find(x => x._id === itemId);
        if (!item) {
            this.logger.error("item was null");
            return false;
        }
        let sellRequest = {
            Action: "sell_to_trader",
            type: "sell_to_trader",
            tid: traderId,
            price: price,
            items: [{
                    id: itemId,
                    count: item.upd ? item.upd.StackObjectsCount ? item.upd.StackObjectsCount : 1 : 1,
                    scheme_id: 0
                }]
        };
        let response = this.tradeHelper.sellItem(pmcData, pmcData, sellRequest, sessionId);
        this.saveServer.saveProfile(sessionId);
        return true;
    }
}
module.exports = { mod: new Mod() };
//# sourceMappingURL=LootValueStaticRouter.js.map