import { useState } from "react";
import { useAsyncValue, useLoaderData, useParams, useRevalidator } from "react-router";
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
import { AddEmployeeDialog } from "./AddEmployeeDialog";
import { deleteContractEmployee } from "./api/deleteContractEmployee";
import type { EmployeeItem, GetContractEmployeesResponse, PositionItem } from "./api/getContractEmployees";
import type { UpdateContractEmployeeRequest } from "./api/updateContractEmployee";
import { ContractEmployeeUpdateDialog } from "./ContractEmployeeUpdateDialog";
import { EditContractEmployeePositionDialog } from "./EditContractEmployeePositionDialog";
import { type ContractEmployeesFilterCriteria, useContractEmployeesFilter } from "./hooks/useContractEmployeesFilter";

export const ContractEmployees = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetContractEmployeesResponse>;
  };

  return (
    <AwaitContent promise={promise}>
      <ContractEmployeesContent />
    </AwaitContent>
  );
};

const { FilterSearchInput } = createFilterControls<ContractEmployeesFilterCriteria>();

const ContractEmployeesContent = () => {
  const response = useAsyncValue() as GetContractEmployeesResponse;
  const { filter, setFilter, filtered } = useContractEmployeesFilter(response.employees);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [positionToDelete, setPositionToDelete] = useState<{ contractId: string; contractEmployeeId: string } | null>(null);
  const [positionToEdit, setPositionToEdit] = useState<{
    contractId: string;
    position: PositionItem;
  } | null>(null);
  const [pendingUpdate, setPendingUpdate] = useState<{
    contractId: string;
    contractEmployeeId: string;
    request: UpdateContractEmployeeRequest;
  } | null>(null);
  const { id: projectId, contractId } = useParams();
  const revalidator = useRevalidator();
  const canAddEmployee = useCan(UiAction.contractEmployees.add, {
    contractId: contractId ?? undefined,
    projectId: projectId ?? undefined,
  });

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter} actions={canAddEmployee ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addEmployeePositionToEmployeeTitle}</AddButton> : undefined}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractEmployeesList contractId={contractId} employees={filtered} onDeleteRequested={(payload) => setPositionToDelete(payload)} onEditRequested={(payload) => setPositionToEdit(payload)} />
      {contractId && (
        <AddEmployeeDialog
          open={isAddOpen}
          contractId={contractId}
          projectStartDate={response.projectStartDate}
          projectEndDate={response.projectEndDate}
          existingContractEmployees={response.employees}
          onClose={() => setIsAddOpen(false)}
          onSaved={() => {
            setIsAddOpen(false);
            revalidator.revalidate();
          }}
        />
      )}

      {positionToEdit && (
        <EditContractEmployeePositionDialog
          open
          position={positionToEdit.position}
          projectStartDate={response.projectStartDate}
          projectEndDate={response.projectEndDate}
          onClose={() => setPositionToEdit(null)}
          onContinue={(request) => {
            setPendingUpdate({
              contractId: positionToEdit.contractId,
              contractEmployeeId: positionToEdit.position.id,
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
          await deleteContractEmployee(positionToDelete.contractId, positionToDelete.contractEmployeeId, signal);
          if (!signal.aborted) {
            setPositionToDelete(null);
            revalidator.revalidate();
          }
        }}
      />
    </>
  );
};

interface ContractEmployeesListProps {
  contractId?: string;
  employees: EmployeeItem[];
  onDeleteRequested: (payload: { contractId: string; contractEmployeeId: string }) => void;
  onEditRequested: (payload: { contractId: string; position: PositionItem }) => void;
}

const ContractEmployeesList = ({ contractId, employees, onDeleteRequested, onEditRequested }: ContractEmployeesListProps) => {
  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="space-y-6">
      {employees.map((employee) => (
        <EmployeeSection key={employee.id} contractId={contractId} employee={employee} onDeleteRequested={onDeleteRequested} onEditRequested={onEditRequested} />
      ))}
    </div>
  );
};

interface EmployeeSectionProps {
  contractId?: string;
  employee: EmployeeItem;
  onDeleteRequested: (payload: { contractId: string; contractEmployeeId: string }) => void;
  onEditRequested: (payload: { contractId: string; position: PositionItem }) => void;
}

const EmployeeSection = ({ contractId, employee, onDeleteRequested, onEditRequested }: EmployeeSectionProps) => {
  return (
    <div className="rounded-md border p-4">
      <div className="mb-3 font-medium text-foreground">{employee.fullName}</div>
      {employee.positions.length === 0 ? (
        <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
      ) : (
        <Table className="table-fixed w-full">
          <TableHeader>
            <TableRow>
              <TableHead className="w-32">{Texts.positionCode}</TableHead>
              <TableHead>{Texts.position}</TableHead>
              <TableHead className="w-24">{Texts.workload}</TableHead>
              <TableHead className="w-28">{Texts.from}</TableHead>
              <TableHead className="w-28">{Texts.to}</TableHead>
              <TableHead className="w-24">{Texts.status}</TableHead>
              <TableHead className="w-28">{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {employee.positions.map((position) => (
              <PositionRow key={position.id} contractId={contractId} position={position} onDeleteRequested={onDeleteRequested} onEditRequested={onEditRequested} />
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
};

interface PositionRowProps {
  contractId?: string;
  position: PositionItem;
  onDeleteRequested: (payload: { contractId: string; contractEmployeeId: string }) => void;
  onEditRequested: (payload: { contractId: string; position: PositionItem }) => void;
}

const PositionRow = ({ contractId, position, onDeleteRequested, onEditRequested }: PositionRowProps) => {
  const { id: projectId } = useParams();
  const permissionContext = { contractId: contractId ?? undefined, projectId: projectId ?? undefined };
  const canRemove = useCan(UiAction.contractEmployees.remove, permissionContext);
  const canUpdate = useCan(UiAction.contractEmployees.update, permissionContext);

  return (
    <TableRow className="cursor-pointer">
      <TableCell className="w-32 max-w-32 truncate whitespace-nowrap" title={position.positionCode ?? ""}>
        {position.positionCode ?? Texts.dash}
      </TableCell>
      <TableCell>{position.position ?? Texts.dash}</TableCell>
      <TableCell>{formatWorkloadPercent(position.workload)}</TableCell>
      <TableCell>{formatDate(position.startDate)}</TableCell>
      <TableCell>{formatDate(position.endDate) ?? Texts.dash}</TableCell>
      <TableCell>{position.isActive ? Texts.active : Texts.inactive}</TableCell>
      <TableCell className="space-x-1">
        {canUpdate && (
          <EditButton
            onClick={() => {
              if (!contractId || !position.id) return;
              onEditRequested({ contractId, position });
            }}
          />
        )}
        {canRemove && (
          <DeleteButton
            onClick={async () => {
              if (!contractId) return;
              if (!position.id) return;
              onDeleteRequested({ contractId, contractEmployeeId: position.id });
            }}
          />
        )}
      </TableCell>
    </TableRow>
  );
};
