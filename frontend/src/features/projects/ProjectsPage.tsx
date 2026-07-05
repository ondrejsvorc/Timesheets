import { useState, type Dispatch } from "react";
import { useAsyncValue, useLoaderData } from "react-router";
import { useImmerReducer } from "use-immer";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { createFilterControls } from "@/components/shared/layout/createFilterControls";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import { AddProjectDialog } from "./AddProjectDialog";
import type { GetProjectsResponse, ProjectItem } from "./api";
import { type ProjectsFilterCriteria, useProjectsFilter } from "./hooks/useProjectsFilter";
import { ProjectCard } from "./ProjectCard";
import { projectsReducer, type ProjectsAction } from "./utils/projectsReducer";

export const ProjectsPage = () => {
  const { promise } = useLoaderData() as { promise: Promise<GetProjectsResponse> };

  return (
    <>
      <PageHeader>
        <PageTitle>{Texts.projects}</PageTitle>
      </PageHeader>
      <AwaitContent promise={promise}>
        <ProjectsPageContent />
      </AwaitContent>
    </>
  );
};

const { FilterSearchInput, FilterSelect } = createFilterControls<ProjectsFilterCriteria>();
const projectStatusFilterOptions = [
  { value: "active", label: Texts.activeOnly },
  { value: "archived", label: Texts.archivedOnly },
  { value: "all", label: Texts.allProjects },
] as const;

const ProjectsPageContent = () => {
  const response = useAsyncValue() as GetProjectsResponse;
  const [state, dispatch] = useImmerReducer(projectsReducer, response.projects);
  const { filter, setFilter, filtered } = useProjectsFilter(state);
  const [isAddOpen, setIsAddOpen] = useState(false);

  return (
    <>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={
          <Can action={UiAction.projects.add}>
            <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addProject}</AddButton>
          </Can>
        }
      >
        <FilterSearchInput placeholder={Texts.search} />
        <FilterSelect field="status" options={projectStatusFilterOptions} />
      </FilterBar>
      <ProjectCards projects={filtered} dispatch={dispatch} />
      <AddProjectDialog
        open={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onSaved={(project) => {
          dispatch({ type: "add", project });
          setIsAddOpen(false);
        }}
      />
    </>
  );
};

const ProjectCards = ({ projects, dispatch }: { projects: ProjectItem[]; dispatch: Dispatch<ProjectsAction> }) => {
  if (projects.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {projects.map((project) => (
        <ProjectCard key={project.id} project={project} onUpdate={(project) => dispatch({ type: "update", project })} onDelete={(projectId) => dispatch({ type: "delete", projectId })} />
      ))}
    </div>
  );
};
