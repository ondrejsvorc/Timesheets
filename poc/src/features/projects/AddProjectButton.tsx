import { AddButton } from "@/components/shared/buttons/ActionButtons";

interface AddProjectButtonProps {
  onClick: () => void;
}

export const AddProjectButton = ({ onClick }: AddProjectButtonProps) => {
  return <AddButton onClick={onClick}>Přidat projekt</AddButton>;
};
