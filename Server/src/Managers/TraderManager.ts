/* eslint-disable @typescript-eslint/naming-convention */

import type { IOManager } from "./IOManager";
import { InstanceManager } from "./InstanceManager";

export class TraderManager
{
    private InstanceManager: InstanceManager = new InstanceManager(); 
    private IOManager: IOManager;
}