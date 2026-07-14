import { useState } from "react";
import { useAsyncValue, useLoaderData, useRevalidator } from "react-router";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { createFilterControls } from "@/components/shared/layout/createFilterControls";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { formatDate, formatWorkloadPercent } from "@/utils/format";
import { AddEmployeePositionDialog } from "./AddEmployeePositionDialog";
import type { EmployeePositionItem, GetEmployeePositionsResponse } from "./api";
import { type PositionsFilterCriteria, usePositionsFilter } from "./hooks/usePositionsFilter";

export const EmployeePositions = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetEmployeePositionsResponse>;
  };

  return (
    <AwaitContent promise={promise}>
      <EmployeePositionsContent />
    </AwaitContent>
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
      <Table className="table-fixed w-full">
        <TableHeader>
          <TableRow>
            <TableHead className="w-[22%]">{Texts.position}</TableHead>
            <TableHead className="w-[10%]">{Texts.workload}</TableHead>
            <TableHead className="w-[12%]">{Texts.from}</TableHead>
            <TableHead className="w-[12%]">{Texts.to}</TableHead>
            <TableHead className="w-[28%]">{Texts.project}</TableHead>
            <TableHead className="w-[16%]">{Texts.contractId}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {positions.map((position) => {
            const positionLabel = `${position.positionCode} · ${position.position}`;
            return (
              <TableRow key={`${position.projectId}:${position.contractId}:${position.startDate}`}>
                <TableCell className="truncate" title={positionLabel}>
                  {positionLabel}
                </TableCell>
                <TableCell>{formatWorkloadPercent(position.workload)}</TableCell>
                <TableCell>{formatDate(position.startDate)}</TableCell>
                <TableCell>{formatDate(position.endDate)}</TableCell>
                <TableCell className="truncate" title={position.projectName}>
                  {position.projectName}
                </TableCell>
                <TableCell className="truncate" title={position.contractRegistrationNumber || Texts.dash}>
                  {position.contractRegistrationNumber || Texts.dash}
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
};
