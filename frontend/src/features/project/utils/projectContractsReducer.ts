import { listCrudAdd, listCrudDelete, listCrudUpdate } from "@/utils/listCrudReducer";
import type { ProjectContractItem } from "../api/shared/projectContractItem";

export type ProjectContractsAction =
  | { type: "add"; contract: ProjectContractItem }
  | { type: "edit"; contract: ProjectContractItem }
  | { type: "delete"; contractId: string };

export const projectContractsReducer = (draft: ProjectContractItem[], action: ProjectContractsAction) => {
  switch (action.type) {
    case "add":
      listCrudAdd(draft, action.contract);
      break;
    case "edit":
      listCrudUpdate(draft, action.contract);
      break;
    case "delete":
      listCrudDelete(draft, action.contractId);
      break;
  }
};
