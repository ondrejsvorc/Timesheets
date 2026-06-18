import { FormDialog } from "@/components/shared/dialogs/FormDialog";
import { Texts } from "@/constants/texts";
import { createProjectContract, type ProjectContractItem } from "./api";
import { ContractForm, type ContractFormValues } from "./ContractForm";

interface AddContractDialogProps {
  projectId: string;
  open: boolean;
  onClose: () => void;
  onSaved: (contract: ProjectContractItem) => void;
}

export const AddContractDialog = ({ projectId, open, onClose, onSaved }: AddContractDialogProps) => {
  const handleSubmit = async (values: ContractFormValues, signal: AbortSignal) => {
    const response = await createProjectContract(
      projectId,
      {
        name: values.name,
        registrationNumber: values.contractId,
      },
      signal,
    );
    onSaved(response.projectContract);
    onClose();
  };

  return (
    <FormDialog open={open} title={Texts.newContract} onClose={onClose}>
      <ContractForm onSubmit={handleSubmit} onCancel={onClose} />
    </FormDialog>
  );
};
