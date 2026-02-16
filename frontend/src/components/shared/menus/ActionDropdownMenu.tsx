import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Texts } from "@/constants/texts";
import { MoreHorizontal } from "lucide-react";
import { AddIcon, DeleteIcon, EditIcon } from "../buttons/ActionButtons";

interface ActionDropdownMenuProps {
  children: React.ReactNode;
}

export const ActionDropdownMenu = ({ children }: ActionDropdownMenuProps) => {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <ActionDropdownMenuTrigger />
      </DropdownMenuTrigger>
      <DropdownMenuContent>{children}</DropdownMenuContent>
    </DropdownMenu>
  );
};

const ActionDropdownMenuTrigger = () => {
  return (
    <Button variant="ghost" size="icon" className="h-8 w-8">
      <MoreHorizontal className="h-4 w-4" />
    </Button>
  );
};

interface ActionDropdownMenuItemProps {
  icon: React.ReactNode;
  label: string;
  onClick: (event: React.MouseEvent<HTMLDivElement>) => void;
}

const ActionDropdownMenuItem = ({ icon, label, onClick }: ActionDropdownMenuItemProps) => {
  return (
    <DropdownMenuItem
      onSelect={(event) => {
        event.preventDefault();
      }}
      onClick={(event) => {
        onClick(event);
      }}
    >
      {icon}
      {label}
    </DropdownMenuItem>
  );
};

interface ActionProps {
  onClick: (event: React.MouseEvent<HTMLDivElement>) => void;
}

interface AddActionProps extends ActionProps {
  label: string;
}

export const AddAction = ({ onClick, label }: AddActionProps) => <ActionDropdownMenuItem icon={<AddIcon />} label={label} onClick={onClick} />;
export const EditAction = ({ onClick }: ActionProps) => <ActionDropdownMenuItem icon={<EditIcon />} label={Texts.edit} onClick={onClick} />;
export const DeleteAction = ({ onClick }: ActionProps) => <ActionDropdownMenuItem icon={<DeleteIcon />} label={Texts.delete} onClick={onClick} />;
