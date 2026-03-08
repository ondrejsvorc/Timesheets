import { compareIds } from "@/utils/compareIds";
import type { ProjectItem } from "../api/shared/projectItem";

export type ProjectsAction = { type: "add"; project: ProjectItem } | { type: "update"; project: ProjectItem } | { type: "delete"; projectId: string };

export const projectsReducer = (draft: ProjectItem[], action: ProjectsAction) => {
  switch (action.type) {
    case "add": {
      draft.push(action.project);
      break;
    }
    case "update": {
      const index = draft.findIndex((p) => compareIds(p.id, action.project.id));
      if (index !== -1) {
        draft[index] = action.project;
      }
      break;
    }
    case "delete": {
      const index = draft.findIndex((p) => compareIds(p.id, action.projectId));
      if (index !== -1) {
        draft.splice(index, 1);
      }
      break;
    }
  }
};
