import { useState } from "react";
import { useAsyncValue, useLoaderData } from "react-router";
import { useImmerReducer } from "use-immer";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { AddProjectDialog } from "./AddProjectDialog";
import type { GetProjectsResponse } from "./api/getProjects";
import type { ProjectItem } from "./api/shared/projectItem";
import { type ProjectsFilterCriteria, useProjectsFilter } from "./hooks/useProjectsFilter";
import { ProjectCard } from "./ProjectCard";
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

  const handleUpdate = (project: ProjectItem) => dispatch({ type: "update", project });
  const handleDelete = (projectId: string) => dispatch({ type: "delete", projectId });

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
      {filtered.length === 0 ? (
        <EmptyState />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filtered.map((project) => (
            <ProjectCard key={project.id} project={project} onUpdate={handleUpdate} onDelete={handleDelete} />
          ))}
        </div>
      )}
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
