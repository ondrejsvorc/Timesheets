import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { ProjectItem } from "./shared/projectItem";

export type CreateProjectRequest = {
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
};

export type CreateProjectResponse = {
  project: ProjectItem;
};

export const createProject = async (request: CreateProjectRequest, signal: AbortSignal): Promise<CreateProjectResponse> => {
  return withOptionalDelay("fast", () =>
    customFetch<CreateProjectResponse>(`${ApiUrl}/projects`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    }),
  );
};
