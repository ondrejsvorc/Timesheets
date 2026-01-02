import { AddButton } from "@/components/shared/buttons/ActionButtons";

interface AddManagerButtonProps {
  onClick: () => void;
}

export const AddContractManagerButton = ({ onClick }: AddManagerButtonProps) => {
  return <AddButton onClick={onClick}>Přidat manažera</AddButton>;
};
