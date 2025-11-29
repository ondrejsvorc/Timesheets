import { Constants } from "../../common/Constants";

export type ContractItem = {
  id: string;
  name: string;
  registrationNumber: string | null;
  startDate: string;
  endDate: string | null;
  employeeCount: number;
};

export type GetProjectContractsResponse = {
  contracts: ContractItem[];
};

export const getProjectContracts = async (id: string): Promise<GetProjectContractsResponse> => {
  const response = await fetch(`${Constants.apiUrl}/projects/${id}/contracts`);
  if (!response.ok) throw new Error("Failed to fetch project contracts.");
  return await response.json();
};

