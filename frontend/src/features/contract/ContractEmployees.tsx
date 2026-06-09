import { isBefore, parseISO, startOfDay } from "date-fns";
import { Suspense, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useParams, useRevalidator } from "react-router";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { AddButton, DeleteButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { formatDate } from "@/utils/formatDate";
import { formatWorkloadPercent } from "@/utils/formatWorkload";
import { AddEmployeeDialog } from "./AddEmployeeDialog";
import { deleteContractEmployee } from "./api/deleteContractEmployee";
import type { EmployeeItem, GetContractEmployeesResponse, PositionItem } from "./api/getContractEmployees";
import { type ContractEmployeesFilterCriteria, useContractEmployeesFilter } from "./hooks/useContractEmployeesFilter";

export const ContractEmployees = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetContractEmployeesResponse>;
  };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={promise}>
        <ContractEmployeesContent />
      </Await>
    </Suspense>
  );
};

const { FilterSearchInput } = createFilterControls<ContractEmployeesFilterCriteria>();

const ContractEmployeesContent = () => {
  const response = useAsyncValue() as GetContractEmployeesResponse;
  const { filter, setFilter, filtered } = useContractEmployeesFilter(response.employees);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [positionToDelete, setPositionToDelete] = useState<{ contractId: string; contractEmployeeId: string } | null>(null);
  const { contractId } = useParams();
  const revalidator = useRevalidator();
  const canAddEmployee = useCan(UiAction.contractEmployees.add, { contractId: contractId ?? undefined });

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>{Texts.employees}</SubPageTitle>
      </SubPageHeader>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={canAddEmployee ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addEmployeePositionToEmployeeTitle}</AddButton> : undefined}
      >
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractEmployeesList contractId={contractId} employees={filtered} onDeleteRequested={(payload) => setPositionToDelete(payload)} />
      {contractId && (
        <AddEmployeeDialog
          open={isAddOpen}
          contractId={contractId}
          existingContractEmployees={response.employees}
          onClose={() => setIsAddOpen(false)}
          onSaved={() => {
            setIsAddOpen(false);
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
}

const ContractEmployeesList = ({ contractId, employees, onDeleteRequested }: ContractEmployeesListProps) => {
  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="space-y-6">
      {employees.map((employee) => (
        <EmployeeSection key={employee.id} contractId={contractId} employee={employee} onDeleteRequested={onDeleteRequested} />
      ))}
    </div>
  );
};

interface EmployeeSectionProps {
  contractId?: string;
  employee: EmployeeItem;
  onDeleteRequested: (payload: { contractId: string; contractEmployeeId: string }) => void;
}

const EmployeeSection = ({ contractId, employee, onDeleteRequested }: EmployeeSectionProps) => {
  return (
    <div className="rounded-md border p-4">
      <div className="mb-3 font-medium text-foreground">{employee.fullName}</div>
      {employee.positions.length === 0 ? (
        <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{Texts.position}</TableHead>
              <TableHead>{Texts.workload}</TableHead>
              <TableHead>{Texts.from}</TableHead>
              <TableHead>{Texts.to}</TableHead>
              <TableHead>{Texts.status}</TableHead>
              <TableHead>{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {employee.positions.map((position) => (
              <PositionRow key={position.id} contractId={contractId} position={position} onDeleteRequested={onDeleteRequested} />
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
}

const PositionRow = ({ contractId, position, onDeleteRequested }: PositionRowProps) => {
  const active = isPositionActive(position);
  const canRemove = useCan(UiAction.contractEmployees.remove, { contractId: contractId ?? undefined });

  return (
    <TableRow className="cursor-pointer">
      <TableCell>{position.position ?? Texts.dash}</TableCell>
      <TableCell>{formatWorkloadPercent(position.workload)}</TableCell>
      <TableCell>{formatDate(position.startDate)}</TableCell>
      <TableCell>{formatDate(position.endDate) ?? Texts.dash}</TableCell>
      <TableCell>{active ? Texts.active : Texts.inactive}</TableCell>
      <TableCell>
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

/** Aktivní, pokud není endDate nebo endDate je dnes či v budoucnu. */
/** TODO: Posílat příznak z backendu, protože zde může být chyba kvůli časové zóně klienta. */
function isPositionActive(position: PositionItem): boolean {
  if (!position.endDate) return true;
  const today = startOfDay(new Date());
  const end = parseISO(position.endDate);
  return !isBefore(end, today);
}
