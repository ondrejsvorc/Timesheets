export interface GetProjectsResponse {
  projects: ProjectItem[];
}

export interface ProjectItem {
  id: string;
  name: string;
  registrationNumber: string | null;
  startDate: string;
  endDate: string | null;
  contractCount: number;
}

const mockData: GetProjectsResponse = {
  projects: [
    {
      id: "4efc77cd-0479-4b57-b2c2-11783faf4063",
      name: "Digitalizace vzdělávacích procesů",
      registrationNumber: "CZ.02.3.68/0.0/0.0/19_076/001",
      startDate: "2023-01-01",
      endDate: "2025-12-31",
      contractCount: 5,
    },
    {
      id: "a8b3c5d2-1e4f-4a6b-9c8d-2f5e7a9b3c1d",
      name: "Výzkum fotovoltaických materiálů",
      registrationNumber: "FV-24-145",
      startDate: "2024-02-01",
      endDate: null,
      contractCount: 3,
    },
    {
      id: "f7e9d1c3-5a8b-4c2d-9e1f-3a7b5c9d1e3f",
      name: "Modernizace ICT infrastruktury",
      registrationNumber: null,
      startDate: "2022-05-15",
      endDate: "2024-08-30",
      contractCount: 12,
    },
    {
      id: "b2d4f6a8-3c5e-4a7b-9d1f-2e4a6c8b0d2f",
      name: "Inovace výuky technických oborů",
      registrationNumber: "OPVVV-21-002",
      startDate: "2021-09-01",
      endDate: "2023-06-30",
      contractCount: 7,
    },
    {
      id: "c9e1f3a5-7b9d-4e1f-3a5c-7b9d1e3f5a7c",
      name: "Rozvoj knihovnických služeb UJEP",
      registrationNumber: null,
      startDate: "2024-01-10",
      endDate: null,
      contractCount: 2,
    },
    {
      id: "d6f8a2b4-9e1c-4f3a-7b5d-9e1f3a5c7b9d",
      name: "Analýza regionální mobility",
      registrationNumber: "ARM-20-118",
      startDate: "2020-04-01",
      endDate: "2021-12-31",
      contractCount: 4,
    },
  ],
};

export const getProjects = async (): Promise<GetProjectsResponse> => {
  return mockData;
};

