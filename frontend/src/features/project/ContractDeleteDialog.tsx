import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import { canConfirmProtectedDelete, forceProtectedDelete } from "@/utils/deleteImpactConsequences";
import { type DeleteContractImpactResponse, formatContractDeleteImpactConsequences, getContractDeleteImpact } from "./api/contractDeleteImpact";
import { deleteProjectContract } from "./api/deleteProjectContract";

interface ContractDeleteDialogProps {
  projectId: string;
  contractId: string;
  contractName: string;
  onClose: () => void;
  onDeleted: () => void;
}

export const ContractDeleteDialog = ({ projectId, contractId, contractName, onClose, onDeleted }: ContractDeleteDialogProps) => {
  const [impact, setImpact] = useState<DeleteContractImpactResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    getContractDeleteImpact(contractId, controller.signal)
      .then(setImpact)
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [contractId]);

  return (
    <ConsequenceDialog
      open
      title={Texts.deleteTitle.replace("{name}", contractName)}
      description={Texts.deleteDescription}
      consequences={impact ? formatContractDeleteImpactConsequences(impact) : []}
      confirmLabel={Texts.delete}
      confirmDisabled={!impact || !canConfirmProtectedDelete(impact)}
      loading={loading}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact || !canConfirmProtectedDelete(impact)) {
          return;
        }
        await deleteProjectContract(projectId, contractId, { force: forceProtectedDelete(impact) }, signal);
        if (!signal.aborted) {
          onDeleted();
        }
      }}
    />
  );
};
