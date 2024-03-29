import { inject, injectable } from "tsyringe";

import { QuestController } from "@spt-aki/controllers/QuestController";
import { RepeatableQuestController } from "@spt-aki/controllers/RepeatableQuestController";
import { IEmptyRequestData } from "@spt-aki/models/eft/common/IEmptyRequestData";
import { IPmcData } from "@spt-aki/models/eft/common/IPmcData";
import { IQuest } from "@spt-aki/models/eft/common/tables/IQuest";
import { IPmcDataRepeatableQuest } from "@spt-aki/models/eft/common/tables/IRepeatableQuests";
import { IGetBodyResponseData } from "@spt-aki/models/eft/httpResponse/IGetBodyResponseData";
import { IItemEventRouterResponse } from "@spt-aki/models/eft/itemEvent/IItemEventRouterResponse";
import { IAcceptQuestRequestData } from "@spt-aki/models/eft/quests/IAcceptQuestRequestData";
import { ICompleteQuestRequestData } from "@spt-aki/models/eft/quests/ICompleteQuestRequestData";
import { IHandoverQuestRequestData } from "@spt-aki/models/eft/quests/IHandoverQuestRequestData";
import { IListQuestsRequestData } from "@spt-aki/models/eft/quests/IListQuestsRequestData";
import { IRepeatableQuestChangeRequest } from "@spt-aki/models/eft/quests/IRepeatableQuestChangeRequest";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";

@injectable()
export class QuestCallbacks
{
    constructor(
        @inject("HttpResponseUtil") protected httpResponse: HttpResponseUtil,
        @inject("QuestController") protected questController: QuestController,
        @inject("RepeatableQuestController") protected repeatableQuestController: RepeatableQuestController,
    )
    {}

    /**
     * Handle RepeatableQuestChange event
     */
    public changeRepeatableQuest(
        pmcData: IPmcData,
        body: IRepeatableQuestChangeRequest,
        sessionID: string,
    ): IItemEventRouterResponse
    {
        return this.repeatableQuestController.changeRepeatableQuest(pmcData, body, sessionID);
    }

    /**
     * Handle QuestAccept event
     */
    public acceptQuest(pmcData: IPmcData, body: IAcceptQuestRequestData, sessionID: string): IItemEventRouterResponse
    {
        if (body.type === "repeatable")
        {
            return this.questController.acceptRepeatableQuest(pmcData, body, sessionID);
        }
        return this.questController.acceptQuest(pmcData, body, sessionID);
    }

    /**
     * Handle QuestComplete event
     */
    public completeQuest(
        pmcData: IPmcData,
        body: ICompleteQuestRequestData,
        sessionID: string,
    ): IItemEventRouterResponse
    {
        return this.questController.completeQuest(pmcData, body, sessionID);
    }

    /**
     * Handle QuestHandover event
     */
    public handoverQuest(
        pmcData: IPmcData,
        body: IHandoverQuestRequestData,
        sessionID: string,
    ): IItemEventRouterResponse
    {
        return this.questController.handoverQuest(pmcData, body, sessionID);
    }

    /**
     * Handle client/quest/list
     */
    public listQuests(url: string, info: IListQuestsRequestData, sessionID: string): IGetBodyResponseData<IQuest[]>
    {
        return this.httpResponse.getBody(this.questController.getClientQuests(sessionID));
    }

    /**
     * Handle client/repeatalbeQuests/activityPeriods
     */
    public activityPeriods(
        url: string,
        info: IEmptyRequestData,
        sessionID: string,
    ): IGetBodyResponseData<IPmcDataRepeatableQuest[]>
    {
        return this.httpResponse.getBody(this.repeatableQuestController.getClientRepeatableQuests(info, sessionID));
    }
}
