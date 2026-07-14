import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export type ProjectStatus = "active" | "inactive" | "archived";

export interface ProjectItem {
  id: string;
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
  archivedAt?: string | null;
  contractCount: number;
  status: ProjectStatus;
}

export interface GetProjectsResponse {
  projects: ProjectItem[];
}

export interface CreateProjectRequest {
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
}

export interface CreateProjectResponse {
  project: ProjectItem;
}

export interface UpdateProjectRequest {
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
}

export interface UpdateProjectResponse {
  project: ProjectItem;
}

interface ArchiveProjectResponse {
  project: ProjectItem;
}

interface UnarchiveProjectResponse {
  project: ProjectItem;
}

export const getProjects = (): Promise<GetProjectsResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetProjectsResponse>(`${ApiUrl}/projects`);
  });
};

export const createProject = (request: CreateProjectRequest, signal: AbortSignal): Promise<CreateProjectResponse> => {
  return withDelay("fast", () => {
    return customFetch<CreateProjectResponse>(`${ApiUrl}/projects`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    });
  });
};

export const updateProject = (projectId: string, request: UpdateProjectRequest, signal: AbortSignal): Promise<UpdateProjectResponse> => {
  return withDelay("fast", () => {
    return customFetch<UpdateProjectResponse>(`${ApiUrl}/projects/${projectId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: request.name,
        registrationNumber: request.registrationNumber,
        startDate: request.startDate,
        endDate: request.endDate ?? null,
      }),
      signal,
    });
  });
};

export const archiveProject = (projectId: string, signal?: AbortSignal): Promise<ProjectItem> => {
  return withDelay("fast", () => {
    return customFetch<ArchiveProjectResponse>(`${ApiUrl}/projects/${projectId}/archive`, {
      method: "POST",
      signal,
    }).then((response) => {
      return response.project;
    });
  });
};

export const unarchiveProject = (projectId: string, signal?: AbortSignal): Promise<ProjectItem> => {
  return withDelay("fast", () => {
    return customFetch<UnarchiveProjectResponse>(`${ApiUrl}/projects/${projectId}/unarchive`, {
      method: "POST",
      signal,
    }).then((response) => {
      return response.project;
    });
  });
};

export const deleteProject = (projectId: string, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () => {
    return customFetch<void>(`${ApiUrl}/projects/${projectId}`, {
      method: "DELETE",
      signal,
    });
  });
};
