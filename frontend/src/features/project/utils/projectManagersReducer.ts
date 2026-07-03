import { compareIds } from "@/utils/common";
import type { ProjectManagerItem } from "../api";

export interface PendingDelete {
  employeeId: string;
}

export interface ProjectManagersState {
  managers: ProjectManagerItem[];
  pendingDelete: PendingDelete | null;
}

export type ProjectManagersAction = { type: "add"; projectManager: ProjectManagerItem } | { type: "requestDelete"; employeeId: string } | { type: "confirmDelete" } | { type: "cancelDelete" };

export const projectManagersReducer = (draft: ProjectManagersState, action: ProjectManagersAction) => {
  switch (action.type) {
    case "add": {
      draft.managers.push(action.projectManager);
      return;
    }
    case "requestDelete": {
      draft.pendingDelete = { employeeId: action.employeeId };
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
      const { employeeId } = draft.pendingDelete;
      const index = draft.managers.findIndex((m) => compareIds(m.employeeId, employeeId));
      if (index !== -1) {
        draft.managers.splice(index, 1);
      }
      draft.pendingDelete = null;
      return;
    }
  }
};
