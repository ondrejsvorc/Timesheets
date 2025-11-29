import { Plus } from "lucide-react";
import { Texts } from "../common/Texts";

export const AddProjectButton = () => {
  const handleClick = () => { };

  return (
    <button onClick={handleClick} className="px-4 py-2 bg-black text-white rounded hover:bg-gray-800 flex items-center gap-2">
      <Plus className="w-4 h-4" />
      {Texts.addProject}
    </button>
  );
};
