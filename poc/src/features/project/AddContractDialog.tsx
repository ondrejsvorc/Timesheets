import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Texts } from "@/constants/texts";
import { createProjectContract } from "./api/createProjectContract";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
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
        startDate: values.startDate,
        endDate: values.endDate,
        description: values.description,
      },
      signal,
    );
    onSaved(response.projectContract);
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{Texts.newContract}</DialogTitle>
        </DialogHeader>
        <ContractForm onSubmit={handleSubmit} onCancel={onClose} />
      </DialogContent>
    </Dialog>
  );
};
