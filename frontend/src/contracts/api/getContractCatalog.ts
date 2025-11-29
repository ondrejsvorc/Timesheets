import { Constants } from "../../common/Constants";

export type ContractCatalogItem = {
  id: string;
  name: string;
};

export type GetContractCatalogResponse = {
  projects: ContractCatalogItem[];
};

export const getContractCatalog = async (): Promise<GetContractCatalogResponse> => {
  const response = await fetch(`${Constants.apiUrl}/contracts/catalog`);
  if (!response.ok) throw new Error("Failed to fetch contract catalog.");
  return await response.json();
};

