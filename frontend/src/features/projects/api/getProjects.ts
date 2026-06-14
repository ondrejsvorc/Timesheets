import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import type { ProjectItem } from "./shared/projectItem";

export interface GetProjectsResponse {
  projects: ProjectItem[];
}

export const getProjects = () => withDelay("slow", () => customFetch<GetProjectsResponse>(`${ApiUrl}/projects`));
