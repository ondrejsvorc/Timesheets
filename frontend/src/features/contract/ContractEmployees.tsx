import { isBefore, parseISO, startOfDay } from "date-fns";
import { Fragment, Suspense, useState } from "react";
import { Await, useAsyncValue, useFetcher, useLoaderData, useParams, useRevalidator } from "react-router";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { AddButton, DeleteButton, EditButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import type { GetEmployeesResponse } from "@/features/employees/api/getEmployees";
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
  const employeesFetcher = useFetcher<GetEmployeesResponse>();
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
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={
          canAddEmployee ? (
            <AddButton
              onClick={() => {
                employeesFetcher.load(Routes.resourceEmployees());
                setIsAddOpen(true);
              }}
            >
              {Texts.addEmployeePositionToEmployeeTitle}
            </AddButton>
          ) : undefined
        }
      >
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractEmployeesList
        contractId={contractId}
        employees={filtered}
        onDeleteRequested={(payload) => setPositionToDelete(payload)}
        onEditRequested={(payload) => setPositionToEdit(payload)}
      />
      {contractId && (
        <AddEmployeeDialog
          open={isAddOpen}
          employeesFetcher={employeesFetcher}
          contractId={contractId}
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

const contractEmployeesTableHeadClassName = "w-24";
const contractEmployeesTableActionsClassName = "w-28 text-right";

const ContractEmployeesList = ({ contractId, employees, onDeleteRequested, onEditRequested }: ContractEmployeesListProps) => {
  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table className="table-fixed">
        <TableHeader>
          <TableRow>
            <TableHead>{Texts.position}</TableHead>
            <TableHead className={contractEmployeesTableHeadClassName}>{Texts.workload}</TableHead>
            <TableHead className={contractEmployeesTableHeadClassName}>{Texts.from}</TableHead>
            <TableHead className={contractEmployeesTableHeadClassName}>{Texts.to}</TableHead>
            <TableHead className={contractEmployeesTableHeadClassName}>{Texts.status}</TableHead>
            <TableHead className={contractEmployeesTableActionsClassName}>{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {employees.map((employee, employeeIndex) => (
            <Fragment key={employee.id}>
              <TableRow className="hover:bg-transparent">
                <TableCell
                  colSpan={6}
                  className={employeeIndex === 0 ? "pt-0 font-medium text-foreground" : "border-t pt-4 font-medium text-foreground"}
                >
                  {employee.fullName}
                </TableCell>
              </TableRow>
              {employee.positions.length === 0 ? (
                <TableRow className="hover:bg-transparent">
                  <TableCell colSpan={6} className="text-sm text-muted-foreground">
                    {Texts.noItems}
                  </TableCell>
                </TableRow>
              ) : (
                employee.positions.map((position) => (
                  <PositionRow
                    key={position.id}
                    contractId={contractId}
                    position={position}
                    onDeleteRequested={onDeleteRequested}
                    onEditRequested={onEditRequested}
                  />
                ))
              )}
            </Fragment>
          ))}
        </TableBody>
      </Table>
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
  const active = isPositionActive(position);
  const permissionContext = { contractId: contractId ?? undefined, projectId: projectId ?? undefined };
  const canRemove = useCan(UiAction.contractEmployees.remove, permissionContext);
  const canUpdate = useCan(UiAction.contractEmployees.update, permissionContext);

  return (
    <TableRow className="cursor-pointer">
      <TableCell className="whitespace-normal">{position.position ?? Texts.dash}</TableCell>
      <TableCell className={contractEmployeesTableHeadClassName}>{formatWorkloadPercent(position.workload)}</TableCell>
      <TableCell className={contractEmployeesTableHeadClassName}>{formatDate(position.startDate)}</TableCell>
      <TableCell className={contractEmployeesTableHeadClassName}>{formatDate(position.endDate) ?? Texts.dash}</TableCell>
      <TableCell className={contractEmployeesTableHeadClassName}>{active ? Texts.active : Texts.inactive}</TableCell>
      <TableCell className={`${contractEmployeesTableActionsClassName} space-x-1`}>
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

/** Aktivní, pokud není endDate nebo endDate je dnes či v budoucnu. */
/** TODO: Posílat příznak z backendu, protože zde může být chyba kvůli časové zóně klienta. */
function isPositionActive(position: PositionItem): boolean {
  if (!position.endDate) return true;
  const today = startOfDay(new Date());
  const end = parseISO(position.endDate);
  return !isBefore(end, today);
}
