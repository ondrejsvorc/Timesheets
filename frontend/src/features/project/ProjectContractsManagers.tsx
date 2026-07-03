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
import { useNavigateFrom } from "@/hooks/useNavigateFrom";
import { AddContractManagerDialog } from "./AddContractManagerDialog";
import { type GetProjectContractsManagersResponse, type ProjectContractManagerItem, removeContractManager } from "./api";
import type { ContractsFilterCriteria } from "./hooks/useContractsFilter";
import { useContractsManagersFilter } from "./hooks/useContractsManagersFilter";
import { type ContractsManagersAction, contractsManagersReducer } from "./utils/contractsManagersReducer";

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
  const [state, dispatch] = useImmerReducer(contractsManagersReducer, {
    managers: response.managers,
    pendingDelete: null,
  });
  const [isAddOpen, setIsAddOpen] = useState(false);
  const { filter, setFilter, filtered } = useContractsManagersFilter(state.managers);
  const canAddManager = useCan(UiAction.contractManagers.add, { projectId: projectId ?? undefined });

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter} actions={canAddManager ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addManager}</AddButton> : undefined}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsManagersTable managers={filtered} dispatch={dispatch} />
      <AddContractManagerDialog
        projectId={projectId ?? ""}
        existingManagers={state.managers}
        open={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onSaved={(manager) => {
          dispatch({ type: "add", contractManager: manager });
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
  dispatch: Dispatch<ContractsManagersAction>;
}

export const ContractsManagersTable = ({ managers, dispatch }: ContractsManagersTableProps) => {
  if (managers.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{Texts.contractId}</TableHead>
            <TableHead>{Texts.personalNumber}</TableHead>
            <TableHead>{Texts.fullName}</TableHead>
            <TableHead>{Texts.actions}</TableHead>
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
  dispatch: Dispatch<ContractsManagersAction>;
}

export const ContractManagerRow = ({ manager, dispatch }: ContractManagerRowProps) => {
  const navigate = useNavigateFrom();
  const { id: projectId } = useParams<{ id: string }>();
  const canRemove = useCan(UiAction.contractManagers.remove, { projectId: projectId ?? undefined });

  return (
    <TableRow className="cursor-pointer" onClick={() => navigate(Routes.employee(manager.employeeId))}>
      <TableCell>{manager.contractRegistrationNumber || Texts.dash}</TableCell>
      <TableCell>{manager.employeePersonalNumber}</TableCell>
      <TableCell>{manager.employeeFullName}</TableCell>
      <TableCell>
        {canRemove && (
          <DeleteButton
            onClick={(e) => {
              e.stopPropagation();
              dispatch({
                type: "requestDelete",
                contractId: manager.contractId,
                employeeId: manager.employeeId,
              });
            }}
          />
        )}
      </TableCell>
    </TableRow>
  );
};
