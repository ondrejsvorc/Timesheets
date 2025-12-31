import { type Dispatch, useContext } from "react";
import { ProjectsContext } from "../utils/projectsContext";
import type { ProjectsAction } from "../utils/projectsReducer";

export const useProjectsDispatch = (): Dispatch<ProjectsAction> => {
  const dispatch = useContext(ProjectsContext);
  if (!dispatch) {
    throw new Error("useProjectsDispatch must be used within ProjectsContext.Provider");
  }
  return dispatch;
};
