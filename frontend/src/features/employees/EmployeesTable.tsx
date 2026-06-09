import { useState } from "react";
import { ActionButtons, EditButton } from "@/components/shared/buttons/ActionButtons";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { useNavigateFrom } from "@/hooks/useNavigateFrom";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import type { EmployeeItem } from "./api/getEmployees";
import { EditEmployeeTypeDialog } from "./EditEmployeeTypeDialog";

interface EmployeesTableProps {
  employees: EmployeeItem[];
  onEmployeeTypeSaved: (employeeId: string, employeeTypeId: string | null) => void;
}

export const EmployeesTable = ({ employees, onEmployeeTypeSaved }: EmployeesTableProps) => {
  const [employeeToEdit, setEmployeeToEdit] = useState<EmployeeItem | null>(null);

  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <>
      <div className="rounded-md border p-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{Texts.personalNumber}</TableHead>
              <TableHead>{Texts.fullName}</TableHead>
              <TableHead>{Texts.email}</TableHead>
              <TableHead>{Texts.employeeType}</TableHead>
              <TableHead>{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {employees.map((employee) => (
              <EmployeeRow key={employee.id} employee={employee} onEdit={setEmployeeToEdit} />
            ))}
          </TableBody>
        </Table>
      </div>

      {employeeToEdit && (
        <EditEmployeeTypeDialog
          open
          employee={employeeToEdit}
          onClose={() => setEmployeeToEdit(null)}
          onSaved={(employeeTypeId) => {
            onEmployeeTypeSaved(employeeToEdit.id, employeeTypeId);
            setEmployeeToEdit(null);
          }}
        />
      )}
    </>
  );
};

interface EmployeeRowProps {
  employee: EmployeeItem;
  onEdit: (employee: EmployeeItem) => void;
}

export const EmployeeRow = ({ employee, onEdit }: EmployeeRowProps) => {
  const navigate = useNavigateFrom();
  const canEditType = useCan(UiAction.employees.editType);

  return (
    <TableRow className="cursor-pointer" onClick={() => navigate(Routes.employee(employee.id))}>
      <TableCell>{employee.personalNumber ?? Texts.dash}</TableCell>
      <TableCell>{employee.fullName}</TableCell>
      <TableCell>{employee.email ?? Texts.dash}</TableCell>
      <TableCell>{resolveEmployeeTypeName(employee.employeeTypeId)}</TableCell>
      <TableCell>
        {canEditType && (
          <ActionButtons>
            <EditButton
              onClick={(e) => {
                e.stopPropagation();
                onEdit(employee);
              }}
            />
          </ActionButtons>
        )}
      </TableCell>
    </TableRow>
  );
};
