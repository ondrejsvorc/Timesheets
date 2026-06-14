import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import type { ProjectContractItem } from "./shared/projectContractItem";

export interface GetProjectContractsResponse {
  projectContracts: ProjectContractItem[];
}

export const getProjectContracts = (id: string) =>
  withDelay("slow", () => customFetch<GetProjectContractsResponse>(`${ApiUrl}/projects/${id}/contracts`));
