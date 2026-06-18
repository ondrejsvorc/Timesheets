import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import { type ContractEmployeeUpdateImpactResponse, formatUpdateImpactConsequences, getContractEmployeeUpdateImpact, type UpdateContractEmployeeRequest, updateContractEmployee } from "./api";

interface ContractEmployeeUpdateDialogProps {
  contractId: string;
  contractEmployeeId: string;
  request: UpdateContractEmployeeRequest;
  onClose: () => void;
  onSaved: () => void;
}

export const ContractEmployeeUpdateDialog = ({ contractId, contractEmployeeId, request, onClose, onSaved }: ContractEmployeeUpdateDialogProps) => {
  const [impact, setImpact] = useState<ContractEmployeeUpdateImpactResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    getContractEmployeeUpdateImpact(contractId, contractEmployeeId, request, controller.signal)
      .then(setImpact)
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [contractId, contractEmployeeId, request]);

  return (
    <ConsequenceDialog
      open
      title={Texts.editPositionTitle}
      description={Texts.updatePositionDescription}
      consequences={impact ? formatUpdateImpactConsequences(impact) : []}
      confirmLabel={Texts.confirm}
      confirmDisabled={!impact || !impact.canUpdate}
      loading={loading}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact?.canUpdate) {
          return;
        }
        await updateContractEmployee(contractId, contractEmployeeId, request, signal);
        if (!signal.aborted) {
          onSaved();
        }
      }}
    />
  );
};
