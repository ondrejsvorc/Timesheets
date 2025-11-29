import { Texts } from "../common/Texts";
import { AddProjectButton } from "./AddProjectButton";
import { ProjectCards } from "./ProjectCards";
import { ProjectCardsFilter } from "./ProjectCardsFilter";

export const ProjectsPage = () => {
  const { projects } = localMock;

  return (
    <div className="w-full">
      <h1 className="text-2xl font-semibold mb-6 select-none">
        {Texts.projects}
      </h1>
      <div className="flex items-center justify-between mb-6">
        <ProjectCardsFilter />
        <AddProjectButton />
      </div>
      <ProjectCards projects={projects} />
    </div>
  );
};

export interface GetProjectsResponse { projects: ProjectItem[]; }
export interface ProjectItem {
  id: string;
  name: string;
  registrationNumber: string | null;
  startDate: string;
  endDate: string | null;
  contractCount: number;
}

const localMock: GetProjectsResponse = {
  projects: [
    {
      id: "P-2023-001",
      name: "Digitalizace vzdělávacích procesů",
      registrationNumber: "CZ.02.3.68/0.0/0.0/19_076/001",
      startDate: "2023-01-01",
      endDate: "2025-12-31",
      contractCount: 5
    },
    {
      id: "P-2024-014",
      name: "Výzkum fotovoltaických materiálů",
      registrationNumber: "FV-24-145",
      startDate: "2024-02-01",
      endDate: null,
      contractCount: 3
    },
    {
      id: "P-2022-040",
      name: "Modernizace ICT infrastruktury",
      registrationNumber: null,
      startDate: "2022-05-15",
      endDate: "2024-08-30",
      contractCount: 12
    },
    {
      id: "P-2021-012",
      name: "Inovace výuky technických oborů",
      registrationNumber: "OPVVV-21-002",
      startDate: "2021-09-01",
      endDate: "2023-06-30",
      contractCount: 7
    },
    {
      id: "P-2024-002",
      name: "Rozvoj knihovnických služeb UJEP",
      registrationNumber: null,
      startDate: "2024-01-10",
      endDate: null,
      contractCount: 2
    },
    {
      id: "P-2020-118",
      name: "Analýza regionální mobility",
      registrationNumber: "ARM-20-118",
      startDate: "2020-04-01",
      endDate: "2021-12-31",
      contractCount: 4
    }
  ]
};
