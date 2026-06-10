import { Suspense, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useRevalidator } from "react-router";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { formatDate } from "@/utils/formatDate";
import { formatWorkloadPercent } from "@/utils/formatWorkload";
import { AddEmployeePositionDialog } from "./AddEmployeePositionDialog";
import type { EmployeePositionItem, GetEmployeePositionsResponse } from "./api/getEmployeePositions";
import { type PositionsFilterCriteria, usePositionsFilter } from "./hooks/usePositionsFilter";

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
  const revalidator = useRevalidator();
  const [isAddOpen, setIsAddOpen] = useState(false);

  return (
    <>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={
          <Can action={UiAction.employeePositions.add}>
            <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addEmployeePosition}</AddButton>
          </Can>
        }
      >
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <PositionsTable positions={filtered} />
      <AddEmployeePositionDialog
        open={isAddOpen}
        employeeId={response.employeeId}
        onClose={() => setIsAddOpen(false)}
        onSaved={() => {
          setIsAddOpen(false);
          revalidator.revalidate();
        }}
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
            <TableHead>{Texts.position}</TableHead>
            <TableHead>{Texts.workload}</TableHead>
            <TableHead>{Texts.from}</TableHead>
            <TableHead>{Texts.to}</TableHead>
            <TableHead>{Texts.project}</TableHead>
            <TableHead>{Texts.contractId}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {positions.map((position) => (
            <TableRow key={`${position.projectId}:${position.contractId}:${position.startDate}`}>
              <TableCell>
                {position.positionCode} · {position.position}
              </TableCell>
              <TableCell>{formatWorkloadPercent(position.workload)}</TableCell>
              <TableCell>{formatDate(position.startDate)}</TableCell>
              <TableCell>{formatDate(position.endDate)}</TableCell>
              <TableCell>{position.projectName}</TableCell>
              <TableCell>{position.contractRegistrationNumber || Texts.dash}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
};
