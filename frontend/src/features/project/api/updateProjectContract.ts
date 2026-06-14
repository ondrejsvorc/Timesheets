import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { ProjectContractItem } from "./shared/projectContractItem";

export type UpdateProjectContractRequest = {
  name: string;
  registrationNumber: string;
};

export type UpdateProjectContractResponse = {
  projectContract: ProjectContractItem;
};

export const updateProjectContract = async (
  projectId: string,
  contractId: string,
  request: UpdateProjectContractRequest,
  signal: AbortSignal,
): Promise<UpdateProjectContractResponse> => {
  return withOptionalDelay("fast", () =>
    customFetch<UpdateProjectContractResponse>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: request.name,
        registrationNumber: request.registrationNumber,
      }),
      signal,
    }),
  );
};
