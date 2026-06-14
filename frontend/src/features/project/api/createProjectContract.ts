import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import type { ProjectContractItem } from "./shared/projectContractItem";

export type CreateProjectContractRequest = {
  name: string;
  registrationNumber: string;
};

export type CreateProjectContractResponse = {
  projectContract: ProjectContractItem;
};

export const createProjectContract = async (
  projectId: string,
  request: CreateProjectContractRequest,
  signal: AbortSignal,
): Promise<CreateProjectContractResponse> => {
  return withDelay("fast", () =>
    customFetch<CreateProjectContractResponse>(`${ApiUrl}/projects/${projectId}/contracts`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: request.name,
        registrationNumber: request.registrationNumber,
      }),
      signal,
    }),
  );
};
