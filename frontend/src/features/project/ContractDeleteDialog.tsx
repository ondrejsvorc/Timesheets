import type { FetcherWithComponents } from "react-router";
import { canConfirmProtectedDelete, FetcherConsequenceDialog, forceProtectedDelete } from "@/components/shared/dialogs/FetcherConsequenceDialog";
import { Texts } from "@/constants/texts";
import { type DeleteContractImpactResponse, formatContractDeleteImpactConsequences } from "./api/contractDeleteImpact";
import { deleteProjectContract } from "./api/deleteProjectContract";

interface ContractDeleteDialogProps {
  projectId: string;
  contractId: string;
  contractName: string;
  fetcher: FetcherWithComponents<DeleteContractImpactResponse>;
  onClose: () => void;
  onDeleted: () => void;
}

export const ContractDeleteDialog = ({ projectId, contractId, contractName, fetcher, onClose, onDeleted }: ContractDeleteDialogProps) => (
  <FetcherConsequenceDialog
    fetcher={fetcher}
    title={Texts.deleteTitle.replace("{name}", contractName)}
    description={Texts.deleteDescription}
    confirmLabel={Texts.delete}
    formatConsequences={formatContractDeleteImpactConsequences}
    canConfirm={canConfirmProtectedDelete}
    onClose={onClose}
    onConfirm={async (impact, signal) => {
      await deleteProjectContract(projectId, contractId, { force: forceProtectedDelete(impact) }, signal);
      if (!signal.aborted) {
        onDeleted();
      }
    }}
  />
);
