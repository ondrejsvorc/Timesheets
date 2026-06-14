import { ApiUrl, redirectIfUnauthorized, withOptionalDelay } from "@/constants/api";

export interface GetProjectContractResponse {
  id: string;
  name: string;
  registrationNumber: string;
}

export const getProjectContract = (projectId: string, contractId: string): { promise: Promise<GetProjectContractResponse | null> } => {
  return {
    promise: withOptionalDelay("fast", async () => {
      const response = await fetch(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`, { credentials: "include" });
      redirectIfUnauthorized(response);
      if (response.status === 404) return null;
      if (!response.ok) throw new Error(`${response.status} ${response.statusText}`);
      return response.json() as Promise<GetProjectContractResponse>;
    }),
  };
};
