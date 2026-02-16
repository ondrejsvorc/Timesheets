import { createContext, type Dispatch } from "react";
import type { ProjectContractsAction } from "./projectContractsReducer";

export const ProjectContractsContext = createContext<Dispatch<ProjectContractsAction> | null>(null);
