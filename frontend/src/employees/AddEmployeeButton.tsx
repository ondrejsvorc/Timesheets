import { useState } from "react";
import { Plus } from "lucide-react";
import { Texts } from "../common/Texts";
import { AddEmployeeDialog } from "./AddEmployeeDialog";
import type { EmployeeFormData } from "./AddEmployeeDialog";

export const AddEmployeeButton = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const handleClick = () => setIsDialogOpen(true);
  const handleClose = () => setIsDialogOpen(false);

  const handleConfirm = (data: EmployeeFormData) => {
    console.log("Employee data:", data);
    // TODO: Implement employee creation logic
    setIsDialogOpen(false);
  };

  return (
    <>
      <button
        onClick={handleClick}
        className="px-4 py-2 bg-black text-white rounded hover:bg-gray-800 flex items-center gap-2"
      >
        <Plus className="w-4 h-4" />
        {Texts.registerEmployee}
      </button>
      <AddEmployeeDialog
        isOpen={isDialogOpen}
        onClose={handleClose}
        onConfirm={handleConfirm}
      />
    </>
  );
};

