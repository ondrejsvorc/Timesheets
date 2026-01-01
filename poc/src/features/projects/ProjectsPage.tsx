import { useLoaderData } from "react-router";
import { useImmer, useImmerReducer } from "use-immer";
import { FilterBar } from "@/common/FilterBar";
import { PageHeader, PageTitle } from "@/common/PageHeader";
import { Texts } from "@/common/Texts";
import { AddProjectButton } from "./AddProjectButton";
import { AddProjectDialog } from "./AddProjectDialog";
import type { GetProjectsResponse } from "./api/getProjects";
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
      <PageHeader>
        <PageTitle>{Texts.projects}</PageTitle>
      </PageHeader>
      <FilterBar>
        <ProjectsFilter value={filters} onChange={setFilters} />
        <AddProjectButton onClick={() => setIsAddOpen(true)} />
      </FilterBar>
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
