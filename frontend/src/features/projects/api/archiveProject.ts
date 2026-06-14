import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import type { ProjectItem } from "./shared/projectItem";

interface ArchiveProjectResponse {
  project: ProjectItem;
}

export const archiveProject = (projectId: string, signal?: AbortSignal) =>
  withDelay("fast", () =>
    customFetch<ArchiveProjectResponse>(`${ApiUrl}/projects/${projectId}/archive`, {
      method: "POST",
      signal,
    }).then((response) => response.project),
  );

export const unarchiveProject = (projectId: string, signal?: AbortSignal) =>
  withDelay("fast", () =>
    customFetch<ArchiveProjectResponse>(`${ApiUrl}/projects/${projectId}/unarchive`, {
      method: "POST",
      signal,
    }).then((response) => response.project),
  );
