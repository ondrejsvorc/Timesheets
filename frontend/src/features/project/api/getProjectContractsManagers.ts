import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface ProjectContractManagerItem {
  contractId: string;
  employeeId: string;
  contractRegistrationNumber: string;
  employeePersonalNumber: number;
  employeeFullName: string;
  employeeEmail: string;
}

export interface GetProjectContractsManagersResponse {
  managers: ProjectContractManagerItem[];
}

export const getProjectContractsManagers = (id: string) => {
  return {
    promise: withOptionalDelay("slow", () =>
      customFetch<GetProjectContractsManagersResponse>(`${ApiUrl}/projects/${id}/contracts/managers`),
    ),
  };
};
