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
      <Table className="table-fixed w-full">
        <TableHeader>
          <TableRow>
            <TableHead className="w-[22%]">{Texts.personalNumber}</TableHead>
            <TableHead className="w-[53%]">{Texts.fullName}</TableHead>
            <TableHead className="w-[25%]">{Texts.employeeType}</TableHead>
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
      <TableCell className="truncate" title={employee.personalNumber ?? Texts.dash}>
        {employee.personalNumber ?? Texts.dash}
      </TableCell>
      <TableCell className="truncate" title={employee.fullName}>
        {employee.fullName}
      </TableCell>
      <TableCell className="truncate" title={resolveEmployeeTypeName(employee.employeeTypeId)}>
        {resolveEmployeeTypeName(employee.employeeTypeId)}
      </TableCell>
    </TableRow>
  );
};
