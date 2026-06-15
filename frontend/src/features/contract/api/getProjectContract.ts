import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface GetProjectContractResponse {
  id: string;
  name: string;
  registrationNumber: string;
  projectStartDate: string;
  projectEndDate: string | null;
}

export const getProjectContract = (projectId: string, contractId: string) =>
  withDelay("fast", async () => {
    try {
      return await customFetch<GetProjectContractResponse>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`);
    } catch (error) {
      if (error instanceof Error && error.message.startsWith("404")) {
        return null;
      }
      throw error;
    }
  });
