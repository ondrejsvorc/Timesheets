import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { useImmer, useImmerReducer } from "use-immer";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { AddContractButton } from "./AddContractButton";
import { AddContractDialog } from "./AddContractDialog";
import type { GetProjectContractsResponse } from "./api/getProjectContracts";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
import { type ContractsFilterState, useContractFilters } from "./hooks/useContractFilters";
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

const ProjectContractsContent = () => {
  const response = useAsyncValue() as GetProjectContractsResponse;
  const [state, dispatch] = useImmerReducer(projectContractsReducer, response.contracts);
  const { filters, setFilters, filtered } = useContractFilters(state);
  const [isAddOpen, setIsAddOpen] = useImmer(false);

  return (
    <ProjectContractsContext.Provider value={dispatch}>
      <SubPageHeader>
        <SubPageTitle>Zakázky</SubPageTitle>
      </SubPageHeader>
      <FilterBar>
        <ContractsFilter value={filters} onChange={setFilters} />
        <AddContractButton onClick={() => setIsAddOpen(true)} />
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
            <TableRow key={contract.id} className="cursor-pointer">
              <TableCell>{contract.name}</TableCell>
              <TableCell>{contract.registrationNumber ?? Texts.dash}</TableCell>
              <TableCell>{contract.startDate ?? Texts.dash}</TableCell>
              <TableCell>{contract.endDate ?? Texts.dash}</TableCell>
              <TableCell>TODO</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface FilterProps {
  value: ContractsFilterState;
  onChange: (value: ContractsFilterState) => void;
}

const ContractsFilter = ({ value, onChange }: FilterProps) => {
  return (
    <div className="flex items-center gap-4 flex-wrap">
      <Input
        type="text"
        placeholder={Texts.search}
        value={value.query}
        onChange={(e) => onChange({ ...value, query: e.target.value })}
        className="w-64"
      />
    </div>
  );
};
