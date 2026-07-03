import { listCrudAdd, listCrudDelete, listCrudUpdate } from "@/utils/common";
import type { ProjectItem } from "../api";

export type ProjectsAction = { type: "add"; project: ProjectItem } | { type: "update"; project: ProjectItem } | { type: "delete"; projectId: string };

export const projectsReducer = (draft: ProjectItem[], action: ProjectsAction) => {
  switch (action.type) {
    case "add":
      listCrudAdd(draft, action.project);
      break;
    case "update":
      listCrudUpdate(draft, action.project);
      break;
    case "delete":
      listCrudDelete(draft, action.projectId);
      break;
  }
};
