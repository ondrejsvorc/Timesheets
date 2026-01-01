import { useImmer } from "use-immer";
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
            <TableHead>Osobní číslo</TableHead>
            <TableHead>Celé jméno</TableHead>
            <TableHead>E-mail</TableHead>
            <TableHead>Typ zaměstnance</TableHead>
            <TableHead>Akce</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {employees.map((employee) => (
            <TableRow key={employee.id} className="cursor-pointer">
              <TableCell>{employee.personalNumber ?? "—"}</TableCell>
              <TableCell>{employee.fullName}</TableCell>
              <TableCell>{employee.email ?? "—"}</TableCell>
              <TableCell>{employee.employeeTypeId ? "Akademik" : "Neakademik"}</TableCell>
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
