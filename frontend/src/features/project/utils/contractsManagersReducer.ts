import { compareIds } from "@/utils/common";
import type { ProjectContractManagerItem } from "../api";

export interface PendingDelete {
  contractId: string;
  employeeId: string;
}

export interface ContractsManagersState {
  managers: ProjectContractManagerItem[];
  pendingDelete: PendingDelete | null;
}

export type ContractsManagersAction =
  | { type: "add"; contractManager: ProjectContractManagerItem }
  | { type: "requestDelete"; contractId: string; employeeId: string }
  | { type: "confirmDelete" }
  | { type: "cancelDelete" };

export const contractsManagersReducer = (draft: ContractsManagersState, action: ContractsManagersAction) => {
  switch (action.type) {
    case "add": {
      draft.managers.push(action.contractManager);
      return;
    }
    case "requestDelete": {
      draft.pendingDelete = { contractId: action.contractId, employeeId: action.employeeId };
      return;
    }
    case "cancelDelete": {
      draft.pendingDelete = null;
      return;
    }
    case "confirmDelete": {
      if (draft.pendingDelete === null) {
        return;
      }
      const { contractId, employeeId } = draft.pendingDelete;
      const index = draft.managers.findIndex((m) => compareIds(m.contractId, contractId) && compareIds(m.employeeId, employeeId));
      if (index !== -1) {
        draft.managers.splice(index, 1);
      }
      draft.pendingDelete = null;
      return;
    }
  }
};
