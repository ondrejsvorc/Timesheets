import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export type UpdateProjectContractRequest = {
  name: string;
  registrationNumber: string;
};

export const updateProjectContract = async (
  projectId: string,
  contractId: string,
  request: UpdateProjectContractRequest,
  signal: AbortSignal,
): Promise<void> => {
  return withOptionalDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`, {
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
