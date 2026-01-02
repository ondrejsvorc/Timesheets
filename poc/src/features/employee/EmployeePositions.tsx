import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import type { EmployeesFilterState } from "../employees/hooks/useEmployeeFilters";
import type { EmployeePositionItem, GetEmployeePositionsResponse } from "./api/getEmployeePositions";
import { usePositionFilters } from "./hooks/usePositionFilters";

export const EmployeePositions = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetEmployeePositionsResponse>;
  };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={promise}>
        <EmployeePositionsContent />
      </Await>
    </Suspense>
  );
};

const EmployeePositionsContent = () => {
  const response = useAsyncValue() as GetEmployeePositionsResponse;
  const { filters, setFilters, filtered } = usePositionFilters(response.positions);

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>Pozice</SubPageTitle>
      </SubPageHeader>
      <FilterBar>
        <PositionsFilter value={filters} onChange={setFilters} />
      </FilterBar>
      <PositionsTable positions={filtered} />
    </>
  );
};

interface PositionsTableProps {
  positions: EmployeePositionItem[];
}

export const PositionsTable = ({ positions }: PositionsTableProps) => {
  if (positions.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{Texts.project}</TableHead>
            <TableHead>{Texts.contract}</TableHead>
            <TableHead>{Texts.position}</TableHead>
            <TableHead>{Texts.startDate}</TableHead>
            <TableHead>{Texts.endDate}</TableHead>
            <TableHead>{Texts.status}</TableHead>
            <TableHead>{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {positions.map((position) => (
            <TableRow key={`${position.projectId}:${position.contractId}:${position.startDate}`} className="cursor-pointer">
              <TableCell>{position.projectName}</TableCell>
              <TableCell>{position.contractName}</TableCell>
              <TableCell>{position.position}</TableCell>
              <TableCell>{position.startDate}</TableCell>
              <TableCell>{position.endDate ?? Texts.dash}</TableCell>
              <TableCell>TODO</TableCell>
              <TableCell>TODO</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface FilterProps {
  value: EmployeesFilterState;
  onChange: (value: EmployeesFilterState) => void;
}

const PositionsFilter = ({ value, onChange }: FilterProps) => {
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
