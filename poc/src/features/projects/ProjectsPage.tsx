import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { useImmer, useImmerReducer } from "use-immer";
import { AddProjectDialog } from "./AddProjectDialog";
import type { GetProjectsResponse } from "./api/getProjects";
import { type ProjectsFilterCriteria, useProjectsFilter } from "./hooks/useProjectsFilter";
import { ProjectCards } from "./ProjectCards";
import { ProjectsContext } from "./utils/projectsContext";
import { projectsReducer } from "./utils/projectsReducer";

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
          <ProjectsPageContent />
        </Await>
      </Suspense>
    </>
  );
};

const { FilterSearchInput, FilterCheckbox } = createFilterControls<ProjectsFilterCriteria>();

const ProjectsPageContent = () => {
  const response = useAsyncValue() as GetProjectsResponse;
  const [state, dispatch] = useImmerReducer(projectsReducer, response.projects);
  const { filter, setFilter, filtered } = useProjectsFilter(state);
  const [isAddOpen, setIsAddOpen] = useImmer(false);

  return (
    <ProjectsContext.Provider value={dispatch}>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddButton onClick={() => setIsAddOpen(true)}>{Texts.addProject}</AddButton>}>
        <FilterSearchInput placeholder={Texts.search} />
        <FilterCheckbox field="onlyActive" label={Texts.activeOnly} />
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
