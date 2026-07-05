import { type Dispatch, useState } from "react";
import { useAsyncValue, useLoaderData } from "react-router";
import { useImmerReducer } from "use-immer";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { createFilterControls } from "@/components/shared/layout/createFilterControls";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import { type ListCrudAction, listCrudReducer, listCrudState } from "@/utils/listCrudReducer";
import { AddProjectDialog } from "./AddProjectDialog";
import { deleteProject, type GetProjectsResponse, type ProjectItem } from "./api";
import { type ProjectsFilterCriteria, useProjectsFilter } from "./hooks/useProjectsFilter";
import { ProjectCard } from "./ProjectCard";

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
  const [state, dispatch] = useImmerReducer(listCrudReducer, listCrudState(response.projects));
  const { filter, setFilter, filtered } = useProjectsFilter(state.items);
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
          dispatch({ type: "add", item: project });
          setIsAddOpen(false);
        }}
      />
      <ConfirmationDialog
        open={state.pendingDelete !== null}
        onCancel={() => dispatch({ type: "cancelDelete" })}
        onConfirm={async (_event, signal) => {
          if (!state.pendingDelete) return;
          await deleteProject(state.pendingDelete, signal);
          if (!signal.aborted) {
            dispatch({ type: "confirmDelete" });
          }
        }}
      />
    </>
  );
};

const ProjectCards = ({ projects, dispatch }: { projects: ProjectItem[]; dispatch: Dispatch<ListCrudAction<ProjectItem>> }) => {
  if (projects.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {projects.map((project) => (
        <ProjectCard
          key={project.id}
          project={project}
          onUpdate={(project) => dispatch({ type: "update", item: project })}
          onRequestDelete={(projectId) => dispatch({ type: "requestDelete", key: projectId })}
        />
      ))}
    </div>
  );
};
