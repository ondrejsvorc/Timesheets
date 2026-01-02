import { EditButton } from "@/components/shared/buttons/ActionButtons";

interface EditContractButtonProps {
  onClick: () => void;
}

export const EditContractButton = ({ onClick }: EditContractButtonProps) => {
  return <EditButton onClick={onClick}>Upravit zakázku</EditButton>;
};
