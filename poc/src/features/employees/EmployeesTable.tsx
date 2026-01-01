import { useImmer } from "use-immer";
import { Texts } from "@/common/Texts";
import { EmptyState } from "@/common/EmptyState";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { AddEmployeePositionButton } from "./AddEmployeePositionButton";
import { AddEmployeePositionDialog } from "./AddEmployeePositionDialog";
import type { EmployeeItem } from "./api/getEmployees";

interface EmployeesTableProps {
  employees: EmployeeItem[];
}

export const EmployeesTable = ({ employees }: EmployeesTableProps) => {
  const [isAddPositionOpen, setIsAddPositionOpen] = useImmer(false);

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
            <TableHead>{Texts.email}</TableHead>
            <TableHead>{Texts.employeeType}</TableHead>
            <TableHead>{Texts.actions}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {employees.map((employee) => (
            <TableRow key={employee.id} className="cursor-pointer">
              <TableCell>{employee.personalNumber ?? Texts.dash}</TableCell>
              <TableCell>{employee.fullName}</TableCell>
              <TableCell>{employee.email ?? Texts.dash}</TableCell>
              <TableCell>{employee.employeeTypeId ? Texts.academic : Texts.nonAcademic}</TableCell>
              <TableCell>
                <AddEmployeePositionButton onClick={() => setIsAddPositionOpen(true)} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <AddEmployeePositionDialog open={isAddPositionOpen} onClose={() => setIsAddPositionOpen(false)} onSaved={() => {}} />
    </div>
  );
};
