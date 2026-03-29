import { AddButton, DeleteButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { Suspense, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigate, useParams } from "react-router";
import { useImmerReducer } from "use-immer";
import { AddContractManagerDialog } from "./AddContractManagerDialog";
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
  const { filter, setFilter, filtered } = useContractsManagersFilter(state.managers);

  return (
    <ContractsManagersContext.Provider value={dispatch}>
      <SubPageHeader>
        <SubPageTitle>{Texts.contractsManagers}</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddButton onClick={() => setIsAddOpen(true)}>{Texts.addManager}</AddButton>}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsManagersTable managers={filtered} />
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
  const navigate = useNavigate();

  return (
    <TableRow className="cursor-pointer" onClick={() => navigate(Routes.employee(manager.employeeId))}>
      <TableCell>{manager.contractRegistrationNumber || Texts.dash}</TableCell>
      <TableCell>{manager.employeePersonalNumber}</TableCell>
      <TableCell>{manager.employeeFullName}</TableCell>
      <TableCell>{manager.employeeEmail}</TableCell>
      <TableCell>
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
      </TableCell>
    </TableRow>
  );
};
