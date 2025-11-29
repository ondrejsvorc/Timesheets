import { useLoaderData } from "react-router";
import { Texts } from "../common/Texts";
import { AddProjectButton } from "./AddProjectButton";
import { ProjectCards } from "./ProjectCards";
import { ProjectCardsFilter } from "./ProjectCardsFilter";
import type { GetProjectsResponse } from "./api/getProjects";

export const ProjectsPage = () => {
  const { projects } = useLoaderData() as GetProjectsResponse;

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