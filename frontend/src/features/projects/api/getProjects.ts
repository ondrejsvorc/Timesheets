import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { ProjectItem } from "./shared/projectItem";

export interface GetProjectsResponse {
  projects: ProjectItem[];
}

export const getProjects = () => {
  return {
    promise: withOptionalDelay("slow", () => customFetch<GetProjectsResponse>(`${ApiUrl}/projects`)),
  };
};
