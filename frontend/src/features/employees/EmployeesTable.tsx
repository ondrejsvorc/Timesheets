import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { useState } from "react";
import { useNavigate } from "react-router";
import { AddEmployeePositionDialog } from "./AddEmployeePositionDialog";
import type { EmployeeItem } from "./api/getEmployees";

interface EmployeesTableProps {
  employees: EmployeeItem[];
}

export const EmployeesTable = ({ employees }: EmployeesTableProps) => {
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeItem | null>(null);

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
              <EmployeeRow key={employee.id} employee={employee} onAddPosition={setSelectedEmployee} />
            ))}
          </TableBody>
        </Table>
      </div>

      {selectedEmployee && (
        <AddEmployeePositionDialog
          employee={selectedEmployee}
          open
          onClose={() => setSelectedEmployee(null)}
          onSaved={() => setSelectedEmployee(null)}
        />
      )}
    </>
  );
};

interface EmployeeRowProps {
  employee: EmployeeItem;
  onAddPosition: (employee: EmployeeItem) => void;
}

export const EmployeeRow = ({ employee, onAddPosition }: EmployeeRowProps) => {
  const navigate = useNavigate();

  return (
    <TableRow className="cursor-pointer" onClick={() => navigate(Routes.employee(employee.id))}>
      <TableCell>{employee.personalNumber ?? Texts.dash}</TableCell>
      <TableCell>{employee.fullName}</TableCell>
      <TableCell>{employee.email ?? Texts.dash}</TableCell>
      <TableCell>{employee.employeeTypeId ? Texts.academic : Texts.nonAcademic}</TableCell>
      <TableCell>
        <AddButton
          onClick={(e) => {
            e.stopPropagation();
            onAddPosition(employee);
          }}
        >
          {Texts.addEmployeePosition}
        </AddButton>
      </TableCell>
    </TableRow>
  );
};
