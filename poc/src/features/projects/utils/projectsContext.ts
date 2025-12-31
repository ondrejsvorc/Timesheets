import { createContext, type Dispatch } from "react";
import type { ProjectsAction } from "./projectsReducer";

export const ProjectsContext = createContext<Dispatch<ProjectsAction> | null>(null);
