import { AddButton } from "@/common/Buttons";

interface AddProjectButtonProps {
  onClick: () => void;
}

export const AddProjectButton = ({ onClick }: AddProjectButtonProps) => {
  return <AddButton onClick={onClick}>Přidat projekt</AddButton>;
};
