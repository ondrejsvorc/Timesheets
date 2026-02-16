import { AddButton, DeleteButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { useImmerReducer } from "use-immer";
import type { GetProjectContractsManagersResponse, ProjectContractManagerItem } from "./api/getProjectContractsManagers";
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
  const response = useAsyncValue() as GetProjectContractsManagersResponse;
  const [state, dispatch] = useImmerReducer(contractsManagersReducer, {
    managers: response.managers,
    pendingDeleteId: null,
  });
  const { filter, setFilter, filtered } = useContractsManagersFilter(state.managers);

  return (
    <ContractsManagersContext.Provider value={dispatch}>
      <SubPageHeader>
        <SubPageTitle>{Texts.contractsManagers}</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddButton onClick={() => {}}>{Texts.addManager}</AddButton>}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsManagersTable managers={filtered} />
      <ConfirmationDialog
        open={state.pendingDeleteId !== null}
        onCancel={() => dispatch({ type: "cancelDelete" })}
        onConfirm={async (_event, signal) => {
          dispatch({ type: "confirmDelete" });
          await Promise.resolve();
          if (signal.aborted) {
            return;
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
            <TableHead>{Texts.contractName}</TableHead>
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

  return (
    <TableRow className="cursor-pointer">
      <TableCell>{manager.contractName}</TableCell>
      <TableCell>{manager.employeePersonalNumber}</TableCell>
      <TableCell>{manager.employeeFullName}</TableCell>
      <TableCell>{manager.employeeEmail}</TableCell>
      <TableCell>
        <DeleteButton
          onClick={(e) => {
            e.stopPropagation();
            dispatch({
              type: "requestDelete",
              contractManagerId: manager.employeeId,
            });
          }}
        >
          {"Odebrat"}
        </DeleteButton>
      </TableCell>
    </TableRow>
  );
};
