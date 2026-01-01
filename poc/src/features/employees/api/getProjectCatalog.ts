export interface ProjectCatalogItem {
  id: string;
  name: string;
}

export interface GetProjectCatalogResponse {
  projects: ProjectCatalogItem[];
}

const mockResponse: GetProjectCatalogResponse = {
  projects: [
    {
      id: "4efc77cd-0479-4b57-b2c2-11783faf4063",
      name: "Digitalizace vzdělávacích procesů",
    },
    {
      id: "a8b3c5d2-1e4f-4a6b-9c8d-2f5e7a9b3c1d",
      name: "Výzkum fotovoltaických materiálů",
    },
    {
      id: "f7e9d1c3-5a8b-4c2d-9e1f-3a7b5c9d1e3f",
      name: "Modernizace ICT infrastruktury",
    },
    {
      id: "b2d4f6a8-3c5e-4a7b-9d1f-2e4a6c8b0d2f",
      name: "Inovace výuky technických oborů",
    },
    {
      id: "c9e1f3a5-7b9d-4e1f-3a5c-7b9d1e3f5a7c",
      name: "Rozvoj knihovnických služeb UJEP",
    },
  ],
};

export const getProjectCatalog = async (): Promise<GetProjectCatalogResponse> => {
  await new Promise((resolve) => setTimeout(resolve, 1200));
  return mockResponse;
};
