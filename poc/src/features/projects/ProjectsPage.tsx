import { useLoaderData } from "react-router";
import { useImmer, useImmerReducer } from "use-immer";
import { Texts } from "@/common/Texts";
import { AddProjectButton } from "./AddProjectButton";
import { AddProjectDialog } from "./AddProjectDialog";
import type { GetProjectsResponse } from "./api/getProjects";
import type { ProjectItem } from "./api/shared/projectItem";
import { useProjectFilters } from "./hooks/useProjectFilters";
import { ProjectCards } from "./ProjectCards";
import { ProjectsFilter } from "./ProjectsFilter";
import { ProjectsContext } from "./utils/projectsContext";
import { projectsReducer } from "./utils/projectsReducer";

export const ProjectsPage = () => {
  const response = useLoaderData() as GetProjectsResponse;
  const [state, dispatch] = useImmerReducer(projectsReducer, response.projects);
  const { filters, setFilters, filteredProjects } = useProjectFilters(state);
  const [isAddOpen, setIsAddOpen] = useImmer(false);

  return (
    <ProjectsContext.Provider value={dispatch}>
      <h1 className="text-2xl font-semibold mb-6 select-none">{Texts.projects}</h1>
      <div className="flex items-center justify-between mb-6">
        <ProjectsFilter value={filters} onChange={setFilters} />
        <AddProjectButton onClick={() => setIsAddOpen(true)} />
      </div>
      <ProjectCards projects={filteredProjects} />
      <AddProjectDialog
        open={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onSaved={(project) => {
          dispatch({ type: "add", project });
          setIsAddOpen(false);
        }}
      />
    </ProjectsContext.Provider>
  );
};
