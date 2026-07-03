import { EmptyState } from "@/components/shared/data/EmptyState";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { resolveEmployeeTypeName } from "@/features/employee/employeeType";
import { useGo } from "@/hooks/useGo";
import type { EmployeeItem } from "./api";

interface EmployeesTableProps {
  employees: EmployeeItem[];
}

export const EmployeesTable = ({ employees }: EmployeesTableProps) => {
  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{Texts.personalNumber}</TableHead>
            <TableHead>{Texts.fullName}</TableHead>
            <TableHead>{Texts.employeeType}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {employees.map((employee) => (
            <EmployeeRow key={employee.id} employee={employee} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface EmployeeRowProps {
  employee: EmployeeItem;
}

export const EmployeeRow = ({ employee }: EmployeeRowProps) => {
  const go = useGo();

  return (
    <TableRow className="cursor-pointer" onClick={() => go.forward(Routes.employee(employee.id))}>
      <TableCell>{employee.personalNumber ?? Texts.dash}</TableCell>
      <TableCell>{employee.fullName}</TableCell>
      <TableCell>{resolveEmployeeTypeName(employee.employeeTypeId)}</TableCell>
    </TableRow>
  );
};
