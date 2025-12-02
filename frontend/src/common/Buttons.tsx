import { Edit, Plus } from "lucide-react";

export const AddButton = ({ onClick, label }: { onClick: () => void | Promise<void>; label: string }) => {
  return (
    <button onClick={onClick} className="px-4 py-2 bg-black text-white rounded hover:bg-gray-800 flex items-center gap-2">
      <Plus className="w-4 h-4" />
      {label}
    </button>
  );
};

export const EditButton = ({ onClick, label }: { onClick: () => void | Promise<void>; label: string }) => {
  return (
    <button onClick={onClick} className="px-4 py-2 bg-gray-100 text-gray-700 rounded hover:bg-gray-200 transition-colors flex items-center gap-2">
      <Edit className="w-4 h-4" />
      {label}
    </button>
  );
};