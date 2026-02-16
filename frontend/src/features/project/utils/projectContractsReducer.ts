import { compareIds } from "@/utils/compareIds";
import type { ProjectContractItem } from "../api/shared/projectContractItem";

export type ProjectContractsAction =
  | { type: "add"; contract: ProjectContractItem }
  | { type: "edit"; contract: ProjectContractItem }
  | { type: "delete"; contractId: string };

export const projectContractsReducer = (draft: ProjectContractItem[], action: ProjectContractsAction) => {
  switch (action.type) {
    case "add": {
      draft.push(action.contract);
      break;
    }
    case "edit": {
      const index = draft.findIndex((c) => compareIds(c.id, action.contract.id));
      if (index !== -1) {
        draft[index] = action.contract;
      }
      break;
    }
    case "delete": {
      const index = draft.findIndex((c) => compareIds(c.id, action.contractId));
      if (index !== -1) {
        draft.splice(index, 1);
      }
      break;
    }
  }
};
