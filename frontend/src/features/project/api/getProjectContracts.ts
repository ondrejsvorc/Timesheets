import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { ProjectContractItem } from "./shared/projectContractItem";

export interface GetProjectContractsResponse {
  projectContracts: ProjectContractItem[];
}

export const getProjectContracts = (id: string) => {
  return {
    promise: withOptionalDelay("slow", () => customFetch<GetProjectContractsResponse>(`${ApiUrl}/projects/${id}/contracts`)),
  };
};
