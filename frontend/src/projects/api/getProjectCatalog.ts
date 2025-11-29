import { Constants } from "../../common/Constants";

export type ProjectCatalogItem = {
  id: string;
  name: string;
};

export type GetProjectCatalogResponse = {
  projects: ProjectCatalogItem[];
};

export const getProjectCatalog = async (): Promise<GetProjectCatalogResponse> => {
  const response = await fetch(`${Constants.apiUrl}/projects/catalog`);
  if (!response.ok) throw new Error("Failed to fetch project catalog.");
  return await response.json();
};

