import { type Dispatch, useContext } from "react";
import { ContractsManagersContext } from "../utils/contractsManagersContext";
import type { ContractsManagersAction } from "../utils/contractsManagersReducer";

export const useContractsManagersDispatch = (): Dispatch<ContractsManagersAction> => {
  const dispatch = useContext(ContractsManagersContext);
  if (!dispatch) {
    throw new Error("useContractsManagersDispatch must be used within ContractsManagersContext.Provider");
  }
  return dispatch;
};
