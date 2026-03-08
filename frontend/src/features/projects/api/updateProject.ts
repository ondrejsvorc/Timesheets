import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export type UpdateProjectRequest = {
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
};

export const updateProject = async (id: string, request: UpdateProjectRequest, signal: AbortSignal): Promise<void> => {
  return withOptionalDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${id}`, {
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
