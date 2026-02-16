import { createContext, type Dispatch } from "react";
import type { ContractsManagersAction } from "./contractsManagersReducer";

export const ContractsManagersContext = createContext<Dispatch<ContractsManagersAction> | null>(null);
