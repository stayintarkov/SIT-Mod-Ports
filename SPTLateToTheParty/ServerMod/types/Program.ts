import { container } from "tsyringe";

import { ErrorHandler } from "@spt-aki/ErrorHandler";
import { Container } from "@spt-aki/di/Container";
import type { PreAkiModLoader } from "@spt-aki/loaders/PreAkiModLoader";
import { App } from "@spt-aki/utils/App";
import { Watermark } from "@spt-aki/utils/Watermark";

export class Program
{
    private errorHandler: ErrorHandler;
    constructor()
    {
        // set window properties
        process.stdout.setEncoding("utf8");
        process.title = "SPT-AKI Server";
        this.errorHandler = new ErrorHandler();
    }

    public async start(): Promise<void>
    {
        try
        {
            Container.registerTypes(container);
            const childContainer = container.createChildContainer();
            const watermark = childContainer.resolve<Watermark>("Watermark");
            watermark.initialize();

            const preAkiModLoader = childContainer.resolve<PreAkiModLoader>("PreAkiModLoader");
            Container.registerListTypes(childContainer);
            await preAkiModLoader.load(childContainer);

            Container.registerPostLoadTypes(container, childContainer);
            childContainer.resolve<App>("App").load();
        }
        catch (err: any)
        {
            this.errorHandler.handleCriticalError(err instanceof Error ? err : new Error(err));
        }
    }
}
