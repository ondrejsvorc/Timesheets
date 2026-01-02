import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { AddContractManagerButton } from "./AddContractManagerButton";
import type { GetProjectContractsManagersResponse, ProjectContractManagerItem } from "./api/getProjectContractsManagers";
import type { ContractsFilterCriteria } from "./hooks/useContractsFilter";
import { useContractsManagersFilter } from "./hooks/useContractsManagersFilter";

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
  const { filter, setFilter, filtered } = useContractsManagersFilter(response.managers);

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>Manažeři zakázek</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddContractManagerButton onClick={() => {}} />}>
        <FilterSearchInput placeholder={Texts.search} />
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
