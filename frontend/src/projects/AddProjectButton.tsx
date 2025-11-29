import { useState } from "react";
import { Plus } from "lucide-react";
import { Texts } from "../common/Texts";
import { AddProjectDialog } from "./AddProjectDialog";
import type { ProjectFormData } from "./AddProjectDialog";

export const AddProjectButton = () => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const handleClick = () => setIsDialogOpen(true);
  const handleClose = () => setIsDialogOpen(false);

  const handleConfirm = (data: ProjectFormData) => {
    console.log("Project data:", data);
    // TODO: Implement project creation logic
    setIsDialogOpen(false);
  };

  return (<>
      <button onClick={handleClick} className="px-4 py-2 bg-black text-white rounded hover:bg-gray-800 flex items-center gap-2">
        <Plus className="w-4 h-4" />
        {Texts.addProject}
      </button>
      <AddProjectDialog isOpen={isDialogOpen} onClose={handleClose} onConfirm={handleConfirm} />
  </>);
};
