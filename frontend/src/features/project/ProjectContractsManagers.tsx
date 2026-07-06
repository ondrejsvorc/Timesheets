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
import { AddContractManagerDialog } from "./AddContractManagerDialog";
import { type GetProjectContractsManagersResponse, type ProjectContractManagerItem, removeContractManager } from "./api";
import type { ContractsFilterCriteria } from "./hooks/useContractsFilter";
import { useContractsManagersFilter } from "./hooks/useContractsManagersFilter";

const contractsManagersReducer = createListCrudReducer<ProjectContractManagerItem, { contractId: string; employeeId: string }>(
  (m, key) => compareIds(m.contractId, key.contractId) && compareIds(m.employeeId, key.employeeId),
);

export const ProjectContractsManagers = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectContractsManagersResponse>;
  };

  return (
    <AwaitContent promise={promise}>
      <ProjectContractsManagersContent />
    </AwaitContent>
  );
};

const { FilterSearchInput } = createFilterControls<ContractsFilterCriteria>();

const ProjectContractsManagersContent = () => {
  const { id: projectId } = useParams<{ id: string }>();
  const response = useAsyncValue() as GetProjectContractsManagersResponse;
  const [state, dispatch] = useImmerReducer(contractsManagersReducer, listCrudState<ProjectContractManagerItem, { contractId: string; employeeId: string }>(response.managers));
  const [isAddOpen, setIsAddOpen] = useState(false);
  const { filter, setFilter, filtered } = useContractsManagersFilter(state.items);
  const canAddManager = useCan(UiAction.contractManagers.add, { projectId: projectId ?? undefined });

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter} actions={canAddManager ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addManager}</AddButton> : undefined}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsManagersTable managers={filtered} dispatch={dispatch} />
      <AddContractManagerDialog
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
          const { contractId, employeeId } = state.pendingDelete;
          await removeContractManager(contractId, employeeId, signal);
          if (!signal.aborted) {
            dispatch({ type: "confirmDelete" });
          }
        }}
      />
    </>
  );
};

interface ContractsManagersTableProps {
  managers: ProjectContractManagerItem[];
  dispatch: Dispatch<ListCrudAction<ProjectContractManagerItem, { contractId: string; employeeId: string }>>;
}

export const ContractsManagersTable = ({ managers, dispatch }: ContractsManagersTableProps) => {
  if (managers.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table className="table-fixed w-full">
        <TableHeader>
          <TableRow>
            <TableHead className="w-[18%]">{Texts.contractId}</TableHead>
            <TableHead className="w-[16%]">{Texts.personalNumber}</TableHead>
            <TableHead className="w-[46%]">{Texts.fullName}</TableHead>
            <TableHead className="w-[20%] text-right">{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {managers.map((manager) => (
            <ContractManagerRow key={`${manager.contractId}-${manager.employeeId}`} manager={manager} dispatch={dispatch} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface ContractManagerRowProps {
  manager: ProjectContractManagerItem;
  dispatch: Dispatch<ListCrudAction<ProjectContractManagerItem, { contractId: string; employeeId: string }>>;
}

export const ContractManagerRow = ({ manager, dispatch }: ContractManagerRowProps) => {
  const go = useGo();
  const { id: projectId } = useParams<{ id: string }>();
  const canRemove = useCan(UiAction.contractManagers.remove, { projectId: projectId ?? undefined });

  return (
    <TableRow className="cursor-pointer" onClick={() => go.forward(Routes.employee(manager.employeeId))}>
      <TableCell className="truncate" title={manager.contractRegistrationNumber || Texts.dash}>
        {manager.contractRegistrationNumber || Texts.dash}
      </TableCell>
      <TableCell className="truncate" title={manager.employeePersonalNumber}>
        {manager.employeePersonalNumber}
      </TableCell>
      <TableCell className="truncate" title={manager.employeeFullName}>
        {manager.employeeFullName}
      </TableCell>
      <TableCell>
        <div className="flex justify-end">
          {canRemove && (
            <DeleteButton
              onClick={(e) => {
                e.stopPropagation();
                dispatch({ type: "requestDelete", key: { contractId: manager.contractId, employeeId: manager.employeeId } });
              }}
            />
          )}
        </div>
      </TableCell>
    </TableRow>
  );
};
