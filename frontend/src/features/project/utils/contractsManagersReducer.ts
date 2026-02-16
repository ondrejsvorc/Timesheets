import { compareIds } from "@/utils/compareIds";
import type { ProjectContractManagerItem } from "../api/getProjectContractsManagers";

export interface ContractsManagersState {
  managers: ProjectContractManagerItem[];
  pendingDeleteId: string | null;
}

export type ContractsManagersAction =
  | { type: "add"; contractManager: ProjectContractManagerItem }
  | { type: "requestDelete"; contractManagerId: string }
  | { type: "confirmDelete" }
  | { type: "cancelDelete" };

export const contractsManagersReducer = (draft: ContractsManagersState, action: ContractsManagersAction) => {
  switch (action.type) {
    case "add": {
      draft.managers.push(action.contractManager);
      return;
    }
    case "requestDelete": {
      draft.pendingDeleteId = action.contractManagerId;
      return;
    }
    case "cancelDelete": {
      draft.pendingDeleteId = null;
      return;
    }
    case "confirmDelete": {
      if (draft.pendingDeleteId === null) {
        return;
      }
      const index = draft.managers.findIndex((manager) => compareIds(manager.employeeId, draft.pendingDeleteId));
      if (index !== -1) {
        draft.managers.splice(index, 1);
      }
      draft.pendingDeleteId = null;
      return;
    }
  }
};
