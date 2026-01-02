import { AddButton } from "@/components/shared/buttons/ActionButtons";

interface AddContractButtonProps {
  onClick: () => void;
}

export const AddContractButton = ({ onClick }: AddContractButtonProps) => {
  return <AddButton onClick={onClick}>Přidat zakázku</AddButton>;
};
