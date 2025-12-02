import { useState } from "react";
import { MoreHorizontal, UserPlus } from "lucide-react";
import { Texts } from "../common/Texts";
import { AddEmployeePositionDialog, type EmployeePositionFormData } from "./AddEmployeePositionDialog";

interface Employee {
  id: string;
  personalNumber: string;
  fullName: string;
  email: string;
  employeeType: "Akademik" | "Neakademik";
}

const mockEmployees: Employee[] = [
  {
    id: "1",
    personalNumber: "2154",
    fullName: "Jan Novák",
    email: "email@email.cz",
    employeeType: "Neakademik",
  },
  {
    id: "2",
    personalNumber: "2721",
    fullName: "David Dvořák",
    email: "email@email.cz",
    employeeType: "Akademik",
  },
  {
    id: "3",
    personalNumber: "2987",
    fullName: "Petra Malá",
    email: "email@email.cz",
    employeeType: "Neakademik",
  },
  {
    id: "4",
    personalNumber: "2647",
    fullName: "Karel Nový",
    email: "email@email.cz",
    employeeType: "Neakademik",
  },
];

export const EmployeesTable = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null);

  const handleOpenDialog = (employeeId: string) => {
    setSelectedEmployeeId(employeeId);
    setIsDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setIsDialogOpen(false);
    setSelectedEmployeeId(null);
  };

  const handleConfirm = (data: EmployeePositionFormData) => {
    console.log("Employee position data:", data, "for employee:", selectedEmployeeId);
    // TODO: Implement employee position creation logic
    handleCloseDialog();
  };

  return (
    <>
      <div className="border border-gray-300 rounded">
        <table className="w-full table-fixed">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-sm font-medium text-gray-700 border-b border-gray-300">{Texts.personalNumber}</th>
              <th className="px-4 py-3 text-left text-sm font-medium text-gray-700 border-b border-gray-300">{Texts.fullName}</th>
              <th className="px-4 py-3 text-left text-sm font-medium text-gray-700 border-b border-gray-300">{Texts.email}</th>
              <th className="px-4 py-3 text-left text-sm font-medium text-gray-700 border-b border-gray-300">{Texts.employeeType}</th>
              <th className="px-4 py-3 text-left text-sm font-medium text-gray-700 border-b border-gray-300">{Texts.actions}</th>
            </tr>
          </thead>
          <tbody>
            {mockEmployees.map((employee) => (
              <tr key={employee.id} className="border-b border-gray-200 hover:bg-gray-50">
                <td className="px-4 py-3 text-sm text-gray-900">{employee.personalNumber}</td>
                <td className="px-4 py-3 text-sm text-gray-900">{employee.fullName}</td>
                <td className="px-4 py-3 text-sm text-gray-900">{employee.email}</td>
                <td className="px-4 py-3 text-sm text-gray-900">{employee.employeeType}</td>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <button onClick={() => handleOpenDialog(employee.id)} className="px-3 py-1.5 bg-gray-100 text-gray-700 rounded hover:bg-gray-200 transition-colors flex items-center gap-2 text-sm">
                      <UserPlus className="w-4 h-4" />
                      {Texts.addEmployeePosition}
                    </button>
                    <button className="p-1.5 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded transition-colors">
                      <MoreHorizontal className="w-4 h-4" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <AddEmployeePositionDialog isOpen={isDialogOpen} onClose={handleCloseDialog} onConfirm={handleConfirm} />
    </>
  );
};

