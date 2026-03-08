import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Texts } from "@/constants/texts";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
import { updateProjectContract } from "./api/updateProjectContract";
import { ContractForm, type ContractFormValues } from "./ContractForm";
import { useParams } from "react-router";

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
    await updateProjectContract(
      projectId,
      contract.id,
      {
        name: values.name,
        registrationNumber: values.contractId,
      },
      signal,
    );
    onSaved({
      ...contract,
      name: values.name,
      registrationNumber: values.contractId,
    });
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.editContract}</DialogTitle>
        </DialogHeader>
        <ContractForm
          initialValues={{
            name: contract.name,
            contractId: contract.registrationNumber,
          }}
          onSubmit={handleSubmit}
          onCancel={onClose}
        />
      </DialogContent>
    </Dialog>
  );
};
