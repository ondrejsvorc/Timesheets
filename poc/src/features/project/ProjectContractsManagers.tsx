import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubpageHeader";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { AddContractManagerButton } from "./AddContractManagerButton";
import type { GetProjectContractsManagersResponse, ProjectContractManagerItem } from "./api/getProjectContractsManagers";
import { type ContractsManagersFilterState, useContractsManagersFilters } from "./hooks/useContractsManagersFilters";

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

const ProjectContractsManagersContent = () => {
  const response = useAsyncValue() as GetProjectContractsManagersResponse;
  const { filters, setFilters, filtered } = useContractsManagersFilters(response.managers);

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>Manažeři zakázek</SubPageTitle>
      </SubPageHeader>

      <FilterBar>
        <ContractsManagersFilter value={filters} onChange={setFilters} />
        <AddContractManagerButton onClick={() => {}} />
      </FilterBar>

      <ContractsManagersTable managers={filtered} />
    </>
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
            <TableRow key={`${manager.contractId}-${manager.employeeId}`} className="cursor-pointer">
              <TableCell>{manager.contractName}</TableCell>
              <TableCell>{manager.employeePersonalNumber}</TableCell>
              <TableCell>{manager.employeeFullName}</TableCell>
              <TableCell>{manager.employeeEmail}</TableCell>
              <TableCell>TODO</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface FilterProps {
  value: ContractsManagersFilterState;
  onChange: (value: ContractsManagersFilterState) => void;
}

const ContractsManagersFilter = ({ value, onChange }: FilterProps) => {
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
