import { AddButton, EditButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { Suspense, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigate } from "react-router";
import { useImmerReducer } from "use-immer";
import { AddContractDialog } from "./AddContractDialog";
import type { GetProjectContractsResponse } from "./api/getProjectContracts";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
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
        <SubPageTitle>{Texts.contracts}</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddButton onClick={() => setIsAddOpen(true)}>{Texts.addContract}</AddButton>}>
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
  const [contractToEdit, setContractToEdit] = useState<ProjectContractItem | null>(null);

  const dispatch = useProjectContractsDispatch();

  if (contracts.length === 0) {
    return <EmptyState />;
  }

  return (
    <>
      <div className="rounded-md border p-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{Texts.registrationNumber}</TableHead>
              <TableHead>{Texts.contractName}</TableHead>
              <TableHead>{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {contracts.map((contract) => (
              <ContractRow key={contract.id} contract={contract} onEdit={setContractToEdit} />
            ))}
          </TableBody>
        </Table>
      </div>

      {contractToEdit && (
        <EditContractDialog
          open
          contract={contractToEdit}
          onClose={() => setContractToEdit(null)}
          onSaved={() => {
            dispatch({ type: "edit", contract: contractToEdit });
            setContractToEdit(null);
          }}
        />
      )}
    </>
  );
};

interface ContractRowProps {
  contract: ProjectContractItem;
  onEdit: (contract: ProjectContractItem) => void;
}

export const ContractRow = ({ contract, onEdit }: ContractRowProps) => {
  const navigate = useNavigate();

  return (
    <TableRow className="cursor-pointer" onClick={() => navigate(Routes.projects())}>
      <TableCell>{contract.registrationNumber}</TableCell>
      <TableCell>{contract.name}</TableCell>
      <TableCell>
        <EditButton
          onClick={(e) => {
            e.stopPropagation();
            onEdit(contract);
          }}
        >
          {Texts.editContract}
        </EditButton>
      </TableCell>
    </TableRow>
  );
};
