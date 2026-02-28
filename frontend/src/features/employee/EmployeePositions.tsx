import { Suspense, useState } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import type { EmployeePositionItem, GetEmployeePositionsResponse } from "./api/getEmployeePositions";
import { type PositionsFilterCriteria, usePositionsFilter } from "./hooks/usePositionsFilter";
import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { AddEmployeePositionDialog } from "./AddEmployeePositionDialog";

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

const { FilterSearchInput } = createFilterControls<PositionsFilterCriteria>();

const EmployeePositionsContent = () => {
  const response = useAsyncValue() as GetEmployeePositionsResponse;
  const { filter, setFilter, filtered } = usePositionsFilter(response.positions);
  const [isAddOpen, setIsAddOpen] = useState(false);

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>Pozice</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddButton onClick={() => setIsAddOpen(true)}>{Texts.addEmployeePosition}</AddButton>}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <PositionsTable positions={filtered} />
      <AddEmployeePositionDialog
        open={isAddOpen}
        employeeId={response.employeeId}
        onClose={() => setIsAddOpen(false)}
        onSaved={() => setIsAddOpen(false)}
      />
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
