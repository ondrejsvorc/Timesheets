import { useParams } from "react-router";
import { FormDialog } from "@/components/shared/dialogs/FormDialog";
import { Texts } from "@/constants/texts";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
import { updateProjectContract } from "./api/updateProjectContract";
import { ContractForm, type ContractFormValues } from "./ContractForm";

interface EditContractDialogProps {
  open: boolean;
  contract: ProjectContractItem;
  onClose: () => void;
  onSaved: (contract: ProjectContractItem) => void;
}

export const EditContractDialog = ({ open, contract, onClose, onSaved }: EditContractDialogProps) => {
  const { id: projectId } = useParams<{ id: string }>();

  const handleSubmit = async (values: ContractFormValues, signal: AbortSignal) => {
    if (!projectId) return;
    const response = await updateProjectContract(
      projectId,
      contract.id,
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
    <FormDialog open={open} title={Texts.editContract} onClose={onClose}>
      <ContractForm
        initialValues={{
          name: contract.name,
          contractId: contract.registrationNumber,
        }}
        onSubmit={handleSubmit}
        onCancel={onClose}
      />
    </FormDialog>
  );
};
