import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import { DependencyContainer } from "tsyringe";
import { CustomItemService } from "@spt/services/mod/CustomItemService";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { PreSptModLoader } from "@spt/loaders/PreSptModLoader";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { JsonUtil } from "@spt/utils/JsonUtil";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { RagfairPriceService } from "@spt/services/RagfairPriceService";
import { ImporterUtil } from "@spt/utils/ImporterUtil";
import { SaveServer } from "@spt/servers/SaveServer";
import { ItemHelper } from "@spt/helpers/ItemHelper";
import { MailSendService } from "@spt/services/MailSendService";
import { VFS } from "@spt/utils/VFS";
import { HashUtil } from "@spt/utils/HashUtil";
import { TraderHelper } from "@spt/helpers/TraderHelper";

export class InstanceManager 
{
    //#region Accessible in or after preAkiLoad
    public debug: boolean;

    // Instances
    public container: DependencyContainer;
    public preSptModLoader: PreSptModLoader;
    public configServer: ConfigServer;
    public saveServer: SaveServer;
    public itemHelper: ItemHelper;
    public logger: ILogger;
    public staticRouter: StaticRouterModService;
    public vfs: VFS;
    public hashUtil: HashUtil;
    //#endregion

    //#region Acceessible in or after postDBLoad
    public database: IDatabaseTables;
    public customItem: CustomItemService;
    public imageRouter: ImageRouter;
    public jsonUtil: JsonUtil;
    public profileHelper: ProfileHelper;
    public ragfairPriceService: RagfairPriceService;
    public importerUtil: ImporterUtil;
    public customItemService: CustomItemService;
    public mailSendService: MailSendService;
    public traderHelper: TraderHelper;
    //#endregion

    // Call at the start of the mods postDBLoad method
    public preSptLoad(container: DependencyContainer): void
    {
        this.container = container;
        this.preSptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
        this.imageRouter = container.resolve<ImageRouter>("ImageRouter");
        this.configServer = container.resolve<ConfigServer>("ConfigServer");
        this.saveServer = container.resolve<SaveServer>("SaveServer");
        this.itemHelper = container.resolve<ItemHelper>("ItemHelper");
        this.logger = container.resolve<ILogger>("WinstonLogger");
        this.staticRouter = container.resolve<StaticRouterModService>("StaticRouterModService");
        this.vfs = container.resolve<VFS>("VFS");
        this.hashUtil = container.resolve<HashUtil>("HashUtil");
    }

    public postDBLoad(container: DependencyContainer): void
    {
        this.database = container.resolve<DatabaseServer>("DatabaseServer").getTables();
        this.customItem = container.resolve<CustomItemService>("CustomItemService");
        this.jsonUtil = container.resolve<JsonUtil>("JsonUtil");
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        this.ragfairPriceService = container.resolve<RagfairPriceService>("RagfairPriceService");
        this.importerUtil = container.resolve<ImporterUtil>("ImporterUtil");
        this.customItemService = container.resolve<CustomItemService>("CustomItemService");
        this.mailSendService = container.resolve<MailSendService>("MailSendService");
        this.traderHelper = container.resolve<TraderHelper>("TraderHelper");
    }
}