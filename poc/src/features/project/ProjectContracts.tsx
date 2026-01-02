import { Suspense, useState } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { useImmerReducer } from "use-immer";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { AddContractButton } from "./AddContractButton";
import { AddContractDialog } from "./AddContractDialog";
import type { GetProjectContractsResponse } from "./api/getProjectContracts";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
import { EditContractButton } from "./EditContractButton";
import { EditContractDialog } from "./EditContractDialog";
import { type ContractsFilterCriteria, useContractsFilter } from "./hooks/useContractsFilter";
import { useProjectContractsDispatch } from "./hooks/useProjectContractsDispatch";
import { ProjectContractsContext } from "./utils/projectContractsContext";
import { projectContractsReducer } from "./utils/projectContractsReducer";

export const ProjectContracts = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectContractsResponse>;
  };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={promise}>
        <ProjectContractsContent />
      </Await>
    </Suspense>
  );
};

const { FilterSearchInput } = createFilterControls<ContractsFilterCriteria>();

const ProjectContractsContent = () => {
  const response = useAsyncValue() as GetProjectContractsResponse;
  const [state, dispatch] = useImmerReducer(projectContractsReducer, response.contracts);
  const { filter, setFilter, filtered } = useContractsFilter(state);
  const [isAddOpen, setIsAddOpen] = useState(false);

  return (
    <ProjectContractsContext.Provider value={dispatch}>
      <SubPageHeader>
        <SubPageTitle>Zakázky</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddContractButton onClick={() => setIsAddOpen(true)} />}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsTable contracts={filtered} />
      <AddContractDialog
        open={isAddOpen}
        projectId=""
        onClose={() => setIsAddOpen(false)}
        onSaved={(contract) => {
          dispatch({ type: "add", contract });
          setIsAddOpen(false);
        }}
      />
    </ProjectContractsContext.Provider>
  );
};

interface ContractsTableProps {
  contracts: ProjectContractItem[];
}

export const ContractsTable = ({ contracts }: ContractsTableProps) => {
  if (contracts.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{Texts.registrationNumber}</TableHead>
            <TableHead>{Texts.contractName}</TableHead>
            <TableHead>{Texts.startDate}</TableHead>
            <TableHead>{Texts.endDate}</TableHead>
            <TableHead>{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {contracts.map((contract) => (
            <ContractRow key={contract.id} contract={contract} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

export const ContractRow = ({ contract }: { contract: ProjectContractItem }) => {
  const [editOpen, setEditOpen] = useState(false);
  const dispatch = useProjectContractsDispatch();

  return (
    <>
      <TableRow className="cursor-pointer">
        <TableCell>{contract.name}</TableCell>
        <TableCell>{contract.registrationNumber ?? Texts.dash}</TableCell>
        <TableCell>{contract.startDate ?? Texts.dash}</TableCell>
        <TableCell>{contract.endDate ?? Texts.dash}</TableCell>
        <TableCell>
          <EditContractButton onClick={() => setEditOpen(true)} />
        </TableCell>
      </TableRow>
      <EditContractDialog
        open={editOpen}
        contract={contract}
        onClose={() => setEditOpen(false)}
        onSaved={() => {
          dispatch({ type: "edit", contract });
          setEditOpen(false);
        }}
      />
    </>
  );
};
