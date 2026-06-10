import { Suspense, useState } from "react";
import { Await, useAsyncValue, useFetcher, useLoaderData, useParams } from "react-router";
import { useImmerReducer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { AddButton, DeleteButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { useNavigateFrom } from "@/hooks/useNavigateFrom";
import { createFilterControls } from "@/utils/createFilterControls";
import { AddContractManagerDialog, type ContractManagerFormData } from "./AddContractManagerDialog";
import type { GetProjectContractsManagersResponse, ProjectContractManagerItem } from "./api/getProjectContractsManagers";
import { removeContractManager } from "./api/removeContractManager";
import type { ContractsFilterCriteria } from "./hooks/useContractsFilter";
import { useContractsManagersDispatch } from "./hooks/useContractsManagersDispatch";
import { useContractsManagersFilter } from "./hooks/useContractsManagersFilter";
import { ContractsManagersContext } from "./utils/contractsManagersContext";
import { contractsManagersReducer } from "./utils/contractsManagersReducer";

export const ProjectContractsManagers = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectContractsManagersResponse>;
  };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={promise}>
        <ProjectContractsManagersContent />
      </Await>
    </Suspense>
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
  const managerFormFetcher = useFetcher<ContractManagerFormData>();
  const { filter, setFilter, filtered } = useContractsManagersFilter(state.managers);
  const canAddManager = useCan(UiAction.contractManagers.add, { projectId: projectId ?? undefined });

  return (
    <ContractsManagersContext.Provider value={dispatch}>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={
          canAddManager ? (
            <AddButton
              onClick={() => {
                if (projectId) {
                  managerFormFetcher.load(Routes.resourceProjectContracts(projectId));
                }
                setIsAddOpen(true);
              }}
            >
              {Texts.addManager}
            </AddButton>
          ) : undefined
        }
      >
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsManagersTable managers={filtered} />
      <AddContractManagerDialog
        formFetcher={managerFormFetcher}
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
    </ContractsManagersContext.Provider>
  );
};

interface ContractsManagersTableProps {
  managers: ProjectContractManagerItem[];
}

export const ContractsManagersTable = ({ managers }: ContractsManagersTableProps) => {
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
            <TableHead>{Texts.email}</TableHead>
            <TableHead>{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {managers.map((manager) => (
            <ContractManagerRow key={`${manager.contractId}-${manager.employeeId}`} manager={manager} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface ContractManagerRowProps {
  manager: ProjectContractManagerItem;
}

export const ContractManagerRow = ({ manager }: ContractManagerRowProps) => {
  const dispatch = useContractsManagersDispatch();
  const navigate = useNavigateFrom();
  const { id: projectId } = useParams<{ id: string }>();
  const canRemove = useCan(UiAction.contractManagers.remove, { projectId: projectId ?? undefined });

  return (
    <TableRow className="cursor-pointer" onClick={() => navigate(Routes.employee(manager.employeeId))}>
      <TableCell>{manager.contractRegistrationNumber || Texts.dash}</TableCell>
      <TableCell>{manager.employeePersonalNumber}</TableCell>
      <TableCell>{manager.employeeFullName}</TableCell>
      <TableCell>{manager.employeeEmail}</TableCell>
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
