import { useState } from "react";
import { Texts } from "../common/Texts";
import { AddProjectDialog } from "./AddProjectDialog";
import type { ProjectFormData } from "./AddProjectDialog";
import { AddButton } from "../common/Buttons";

export const AddProjectButton = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const handleClick = () => setIsDialogOpen(true);
  const handleClose = () => setIsDialogOpen(false);

  const handleConfirm = async (data: ProjectFormData) => {
    console.log("Project data:", data);
    // TODO: Implement project creation logic
    setIsDialogOpen(false);
  };

  return (<>
      <AddButton onClick={handleClick} label={Texts.addProject} />
      <AddProjectDialog isOpen={isDialogOpen} onClose={handleClose} onConfirm={handleConfirm} />
  </>);
};
