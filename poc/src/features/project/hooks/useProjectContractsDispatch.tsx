import { type Dispatch, useContext } from "react";
import { ProjectContractsContext } from "../utils/projectContractsContext";
import type { ProjectContractsAction } from "../utils/projectContractsReducer";

export const useProjectContractsDispatch = (): Dispatch<ProjectContractsAction> => {
  const dispatch = useContext(ProjectContractsContext);
  if (!dispatch) {
    throw new Error("useProjectContractsDispatch must be used within ProjectContractsContext.Provider");
  }
  return dispatch;
};
