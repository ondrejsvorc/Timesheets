import { UserPlus } from "lucide-react";
import { Button } from "@/components/ui/button";

interface AddEmployeePositionButtonProps {
  onClick: () => void;
}

export const AddEmployeePositionButton = ({ onClick }: AddEmployeePositionButtonProps) => {
  return (
    <Button type="button" variant="secondary" onClick={onClick}>
      <span className="inline-flex items-center gap-2">
        <UserPlus className="size-4" />
        Přidat pozici
      </span>
    </Button>
  );
};
