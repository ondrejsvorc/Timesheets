import { useState } from "react";
import { Texts } from "../common/Texts";
import { AddEmployeeDialog } from "./AddEmployeeDialog";
import type { EmployeeFormData } from "./AddEmployeeDialog";
import { AddButton } from "../common/Buttons";

export const AddEmployeeButton = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const handleClick = () => setIsDialogOpen(true);
  const handleClose = () => setIsDialogOpen(false);

  const handleConfirm = async (data: EmployeeFormData) => {
    console.log("Employee data:", data);
    // TODO: Implement employee creation logic
    setIsDialogOpen(false);
  };

  return (
    <>
      <AddButton onClick={handleClick} label={Texts.registerEmployee} />
      <AddEmployeeDialog isOpen={isDialogOpen} onClose={handleClose} onConfirm={handleConfirm} />
    </>
  );
};

