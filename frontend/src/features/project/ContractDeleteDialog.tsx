import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { deleteProjectContract } from "./api";

interface ContractDeleteDialogProps {
  projectId: string;
  contractId: string;
  onClose: () => void;
  onDeleted: () => void;
}

export const ContractDeleteDialog = ({ projectId, contractId, onClose, onDeleted }: ContractDeleteDialogProps) => {
  return (
    <ConfirmationDialog
      open
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        await deleteProjectContract(projectId, contractId, signal);
        if (!signal.aborted) {
          onDeleted();
        }
      }}
    />
  );
};
