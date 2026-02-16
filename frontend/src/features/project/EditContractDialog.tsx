import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Texts } from "@/constants/texts";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
import { ContractForm, type ContractFormValues } from "./ContractForm";

interface EditContractDialogProps {
  open: boolean;
  contract: ProjectContractItem;
  onClose: () => void;
  onSaved: (contract: ProjectContractItem) => void;
}

export const EditContractDialog = ({ open, contract, onClose, onSaved }: EditContractDialogProps) => {
  const handleSubmit = async (values: ContractFormValues, signal: AbortSignal) => {
    // const response = await updateContract(
    //   contract.id,
    //   {
    //     name: values.name,
    //     registrationNumber: values.contractId,
    //     startDate: values.startDate,
    //     endDate: values.endDate,
    //     description: values.description,
    //   },
    //   signal,
    // );
    // onSaved(response.projectContract);
    // onClose();
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
            startDate: contract.startDate,
            endDate: contract.endDate ?? undefined,
          }}
          onSubmit={handleSubmit}
          onCancel={onClose}
        />
      </DialogContent>
    </Dialog>
  );
};
