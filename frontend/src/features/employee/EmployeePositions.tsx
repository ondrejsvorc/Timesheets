import { useState } from "react";
import { useAsyncValue, useLoaderData, useRevalidator } from "react-router";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { AddButton, DeleteButton, EditButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { formatDate } from "@/utils/formatDate";
import { formatWorkloadPercent } from "@/utils/formatWorkload";
import { deleteContractEmployee, type PositionItem, type UpdateContractEmployeeRequest } from "../contract/api";
import { ContractEmployeeUpdateDialog } from "../contract/ContractEmployeeUpdateDialog";
import { EditContractEmployeePositionDialog } from "../contract/EditContractEmployeePositionDialog";
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
  const [positionToDelete, setPositionToDelete] = useState<EmployeePositionItem | null>(null);
  const [positionToEdit, setPositionToEdit] = useState<EmployeePositionItem | null>(null);
  const [pendingUpdate, setPendingUpdate] = useState<{
    contractId: string;
    contractEmployeeId: string;
    request: UpdateContractEmployeeRequest;
  } | null>(null);

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
      <PositionsTable positions={filtered} onDeleteRequested={setPositionToDelete} onEditRequested={setPositionToEdit} />
      <AddEmployeePositionDialog
        open={isAddOpen}
        employeeId={response.employeeId}
        onClose={() => setIsAddOpen(false)}
        onSaved={() => {
          setIsAddOpen(false);
          revalidator.revalidate();
        }}
      />
      {positionToEdit && (
        <EditContractEmployeePositionDialog
          open
          position={toContractPosition(positionToEdit)}
          projectStartDate={positionToEdit.projectStartDate}
          projectEndDate={positionToEdit.projectEndDate}
          onClose={() => setPositionToEdit(null)}
          onContinue={(request) => {
            setPendingUpdate({
              contractId: positionToEdit.contractId,
              contractEmployeeId: positionToEdit.id,
              request,
            });
            setPositionToEdit(null);
          }}
        />
      )}
      {pendingUpdate && (
        <ContractEmployeeUpdateDialog
          contractId={pendingUpdate.contractId}
          contractEmployeeId={pendingUpdate.contractEmployeeId}
          request={pendingUpdate.request}
          onClose={() => setPendingUpdate(null)}
          onSaved={() => {
            setPendingUpdate(null);
            revalidator.revalidate();
          }}
        />
      )}
      <ConfirmationDialog
        open={positionToDelete !== null}
        onCancel={() => setPositionToDelete(null)}
        onConfirm={async (_event, signal) => {
          if (!positionToDelete) return;
          await deleteContractEmployee(positionToDelete.contractId, positionToDelete.id, signal);
          if (!signal.aborted) {
            setPositionToDelete(null);
            revalidator.revalidate();
          }
        }}
      />
    </>
  );
};

interface PositionsTableProps {
  positions: EmployeePositionItem[];
  onDeleteRequested: (position: EmployeePositionItem) => void;
  onEditRequested: (position: EmployeePositionItem) => void;
}

export const PositionsTable = ({ positions, onDeleteRequested, onEditRequested }: PositionsTableProps) => {
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
            <TableHead>{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {positions.map((position) => (
            <TableRow key={position.id}>
              <TableCell>
                {position.positionCode} · {position.position}
              </TableCell>
              <TableCell>{formatWorkloadPercent(position.workload)}</TableCell>
              <TableCell>{formatDate(position.startDate)}</TableCell>
              <TableCell>{formatDate(position.endDate)}</TableCell>
              <TableCell>{position.projectName}</TableCell>
              <TableCell>{position.contractRegistrationNumber || Texts.dash}</TableCell>
              <TableCell className="space-x-1">
                <PositionActions position={position} onDeleteRequested={onDeleteRequested} onEditRequested={onEditRequested} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface PositionActionsProps {
  position: EmployeePositionItem;
  onDeleteRequested: (position: EmployeePositionItem) => void;
  onEditRequested: (position: EmployeePositionItem) => void;
}

const PositionActions = ({ position, onDeleteRequested, onEditRequested }: PositionActionsProps) => {
  const permissionContext = { contractId: position.contractId, projectId: position.projectId };
  const canUpdate = useCan(UiAction.contractEmployees.update, permissionContext);
  const canRemove = useCan(UiAction.contractEmployees.remove, permissionContext);

  return (
    <>
      {canUpdate && <EditButton onClick={() => onEditRequested(position)} />}
      {canRemove && <DeleteButton onClick={() => onDeleteRequested(position)} />}
    </>
  );
};

const toContractPosition = (position: EmployeePositionItem): PositionItem => ({
  id: position.id,
  positionCode: position.positionCode,
  position: position.position,
  workload: position.workload,
  startDate: position.startDate,
  endDate: position.endDate,
  isActive: true,
});
