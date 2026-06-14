import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { ProjectItem } from "./shared/projectItem";

export type UpdateProjectRequest = {
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
};

export type UpdateProjectResponse = {
  project: ProjectItem;
};

export const updateProject = async (id: string, request: UpdateProjectRequest, signal: AbortSignal): Promise<UpdateProjectResponse> => {
  return withOptionalDelay("fast", () =>
    customFetch<UpdateProjectResponse>(`${ApiUrl}/projects/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: request.name,
        registrationNumber: request.registrationNumber,
        startDate: request.startDate,
        endDate: request.endDate ?? null,
      }),
      signal,
    }),
  );
};
