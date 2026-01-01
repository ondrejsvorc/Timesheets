import { lazy, Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { useImmer, useImmerReducer } from "use-immer";
import { FilterBar } from "@/common/FilterBar";
import { GenericSkeleton } from "@/common/GenericSkeleton";
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

const ProjectsPageContentLazy = lazy(async () => ({
  default: ProjectsPageContent,
}));

export const ProjectsPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectsResponse>;
  };

  return (
    <>
      <PageHeader>
        <PageTitle>{Texts.projects}</PageTitle>
      </PageHeader>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <ProjectsPageContentLazy />
        </Await>
      </Suspense>
    </>
  );
};

const ProjectsPageContent = () => {
  const response = useAsyncValue() as GetProjectsResponse;
  const [state, dispatch] = useImmerReducer(projectsReducer, response.projects);
  const { filters, setFilters, filtered } = useProjectFilters(state);
  const [isAddOpen, setIsAddOpen] = useImmer(false);

  return (
    <ProjectsContext.Provider value={dispatch}>
      <FilterBar>
        <ProjectsFilter value={filters} onChange={setFilters} />
        <AddProjectButton onClick={() => setIsAddOpen(true)} />
      </FilterBar>
      <ProjectCards projects={filtered} />
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
