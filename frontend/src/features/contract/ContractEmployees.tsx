import { useState } from "react";
import { useAsyncValue, useLoaderData, useParams, useRevalidator } from "react-router";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { AddButton, DeleteButton, EditButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { createFilterControls } from "@/components/shared/layout/createFilterControls";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { formatDate, formatWorkloadPercent } from "@/utils/format";
import { AddEmployeeDialog } from "./AddEmployeeDialog";
import { deleteContractEmployee, type EmployeeItem, type GetContractEmployeesResponse, type PositionItem, type UpdateContractEmployeeRequest } from "./api";
import { type ContractEmployeesFilterCriteria, useContractEmployeesFilter } from "./hooks/useContractEmployeesFilter";
import { UpdateContractEmployeeDialog } from "./UpdateContractEmployeeDialog";
import { UpdateEmployeePositionDialog } from "./UpdateEmployeePositionDialog";

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
  const canAddEmployee =
    useCan(UiAction.contractEmployees.add, {
      contractId: contractId ?? undefined,
      projectId: projectId ?? undefined,
    }) && !response.isProjectArchived;

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter} actions={canAddEmployee ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addEmployeePositionToEmployeeTitle}</AddButton> : undefined}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractEmployeesList
        contractId={contractId}
        employees={filtered}
        isReadonly={response.isProjectArchived}
        onDeleteRequested={(payload) => setPositionToDelete(payload)}
        onEditRequested={(payload) => setPositionToEdit(payload)}
      />
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
        <UpdateEmployeePositionDialog
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
        <UpdateContractEmployeeDialog
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
  isReadonly: boolean;
  onDeleteRequested: (payload: { contractId: string; contractEmployeeId: string }) => void;
  onEditRequested: (payload: { contractId: string; position: PositionItem }) => void;
}

const ContractEmployeesList = ({ contractId, employees, isReadonly, onDeleteRequested, onEditRequested }: ContractEmployeesListProps) => {
  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="space-y-6">
      {employees.map((employee) => (
        <EmployeeSection key={employee.id} contractId={contractId} employee={employee} isReadonly={isReadonly} onDeleteRequested={onDeleteRequested} onEditRequested={onEditRequested} />
      ))}
    </div>
  );
};

interface EmployeeSectionProps {
  contractId?: string;
  employee: EmployeeItem;
  isReadonly: boolean;
  onDeleteRequested: (payload: { contractId: string; contractEmployeeId: string }) => void;
  onEditRequested: (payload: { contractId: string; position: PositionItem }) => void;
}

const EmployeeSection = ({ contractId, employee, isReadonly, onDeleteRequested, onEditRequested }: EmployeeSectionProps) => {
  return (
    <div className="rounded-md border p-4">
      <div className="mb-3 font-medium text-foreground">{employee.fullName}</div>
      {employee.positions.length === 0 ? (
        <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
      ) : (
        <Table className="table-fixed w-full">
          <TableHeader>
            <TableRow>
              <TableHead className="w-[13%]">{Texts.positionCode}</TableHead>
              <TableHead className="w-[19%]">{Texts.position}</TableHead>
              <TableHead className="w-[11%]">{Texts.workload}</TableHead>
              <TableHead className="w-[13%]">{Texts.from}</TableHead>
              <TableHead className="w-[13%]">{Texts.to}</TableHead>
              <TableHead className="w-[13%]">{Texts.status}</TableHead>
              <TableHead className="w-[18%] text-right">{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {employee.positions.map((position) => (
              <PositionRow key={position.id} contractId={contractId} position={position} isReadonly={isReadonly} onDeleteRequested={onDeleteRequested} onEditRequested={onEditRequested} />
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
  isReadonly: boolean;
  onDeleteRequested: (payload: { contractId: string; contractEmployeeId: string }) => void;
  onEditRequested: (payload: { contractId: string; position: PositionItem }) => void;
}

const PositionRow = ({ contractId, position, isReadonly, onDeleteRequested, onEditRequested }: PositionRowProps) => {
  const { id: projectId } = useParams();
  const permissionContext = { contractId: contractId ?? undefined, projectId: projectId ?? undefined };
  const canRemove = useCan(UiAction.contractEmployees.remove, permissionContext) && !isReadonly;
  const canUpdate = useCan(UiAction.contractEmployees.update, permissionContext) && !isReadonly;

  return (
    <TableRow className="cursor-pointer">
      <TableCell className="truncate" title={position.positionCode ?? ""}>
        {position.positionCode ?? Texts.dash}
      </TableCell>
      <TableCell className="truncate" title={position.position ?? ""}>
        {position.position ?? Texts.dash}
      </TableCell>
      <TableCell>{formatWorkloadPercent(position.workload)}</TableCell>
      <TableCell>{formatDate(position.startDate)}</TableCell>
      <TableCell>{formatDate(position.endDate) ?? Texts.dash}</TableCell>
      <TableCell>{position.isActive ? Texts.active : Texts.inactive}</TableCell>
      <TableCell className="space-x-1 text-right">
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
