import type { Dispatch } from "react";
import { useState } from "react";
import { useAsyncValue, useLoaderData, useParams } from "react-router";
import { useImmerReducer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { AddButton, DeleteButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { createFilterControls } from "@/components/shared/layout/createFilterControls";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { useGo } from "@/hooks/useGo";
import { compareIds } from "@/utils/common";
import { createListCrudReducer, type ListCrudAction, listCrudState } from "@/utils/listCrudReducer";
import { AddProjectManagerDialog } from "./AddProjectManagerDialog";
import { type GetProjectManagersResponse, type ProjectManagerItem, removeProjectManager } from "./api";
import { type ProjectManagersFilterCriteria, useProjectManagersFilter } from "./hooks/useProjectManagersFilter";

const projectManagersReducer = createListCrudReducer<ProjectManagerItem, { employeeId: string }>((m, key) => compareIds(m.employeeId, key.employeeId));

export const ProjectManagers = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectManagersResponse>;
  };

  return (
    <AwaitContent promise={promise}>
      <ProjectManagersContent />
    </AwaitContent>
  );
};

const { FilterSearchInput } = createFilterControls<ProjectManagersFilterCriteria>();

const ProjectManagersContent = () => {
  const { id: projectId } = useParams<{ id: string }>();
  const response = useAsyncValue() as GetProjectManagersResponse;
  const [state, dispatch] = useImmerReducer(projectManagersReducer, listCrudState<ProjectManagerItem, { employeeId: string }>(response.managers));
  const [isAddOpen, setIsAddOpen] = useState(false);
  const { filter, setFilter, filtered } = useProjectManagersFilter(state.items);
  const canAddManager = useCan(UiAction.projectManagers.add, { projectId: projectId ?? undefined });

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter} actions={canAddManager ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addManager}</AddButton> : undefined}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ProjectManagersTable managers={filtered} dispatch={dispatch} />
      <AddProjectManagerDialog
        projectId={projectId ?? ""}
        existingManagers={state.items}
        open={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onSaved={(manager) => {
          dispatch({ type: "add", item: manager });
          setIsAddOpen(false);
        }}
      />
      <ConfirmationDialog
        open={state.pendingDelete !== null}
        onCancel={() => dispatch({ type: "cancelDelete" })}
        onConfirm={async (_event, signal) => {
          if (!state.pendingDelete || !projectId) return;
          await removeProjectManager(projectId, state.pendingDelete.employeeId, signal);
          if (!signal.aborted) {
            dispatch({ type: "confirmDelete" });
          }
        }}
      />
    </>
  );
};

interface ProjectManagersTableProps {
  managers: ProjectManagerItem[];
  dispatch: Dispatch<ListCrudAction<ProjectManagerItem, { employeeId: string }>>;
}

export const ProjectManagersTable = ({ managers, dispatch }: ProjectManagersTableProps) => {
  if (managers.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{Texts.personalNumber}</TableHead>
            <TableHead>{Texts.fullName}</TableHead>
            <TableHead>{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {managers.map((manager) => (
            <ProjectManagerRow key={`${manager.projectId}-${manager.employeeId}`} manager={manager} dispatch={dispatch} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface ProjectManagerRowProps {
  manager: ProjectManagerItem;
  dispatch: Dispatch<ListCrudAction<ProjectManagerItem, { employeeId: string }>>;
}

export const ProjectManagerRow = ({ manager, dispatch }: ProjectManagerRowProps) => {
  const go = useGo();
  const { id: projectId } = useParams<{ id: string }>();
  const canRemove = useCan(UiAction.projectManagers.remove, { projectId: projectId ?? undefined });

  return (
    <TableRow className="cursor-pointer" onClick={() => go.forward(Routes.employee(manager.employeeId))}>
      <TableCell>{manager.employeePersonalNumber}</TableCell>
      <TableCell>{manager.employeeFullName}</TableCell>
      <TableCell>
        {canRemove && (
          <DeleteButton
            onClick={(e) => {
              e.stopPropagation();
              dispatch({ type: "requestDelete", key: { employeeId: manager.employeeId } });
            }}
          />
        )}
      </TableCell>
    </TableRow>
  );
};
