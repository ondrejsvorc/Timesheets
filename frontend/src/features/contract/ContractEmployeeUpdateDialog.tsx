import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import {
  type ContractEmployeeUpdateImpactResponse,
  formatUpdateImpactConsequences,
  getContractEmployeeUpdateImpact,
} from "./api/contractEmployeeUpdateImpact";
import { type UpdateContractEmployeeRequest, updateContractEmployee } from "./api/updateContractEmployee";

interface ContractEmployeeUpdateDialogProps {
  contractId: string;
  contractEmployeeId: string;
  request: UpdateContractEmployeeRequest;
  onClose: () => void;
  onSaved: () => void;
}

export const ContractEmployeeUpdateDialog = ({ contractId, contractEmployeeId, request, onClose, onSaved }: ContractEmployeeUpdateDialogProps) => {
  const [impact, setImpact] = useState<ContractEmployeeUpdateImpactResponse | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    getContractEmployeeUpdateImpact(contractId, contractEmployeeId, request, controller.signal)
      .then(setImpact)
      .catch(() => {
        if (!controller.signal.aborted) {
          setImpact(null);
        }
      });
    return () => controller.abort();
  }, [contractId, contractEmployeeId, request]);

  const title = Texts.editPositionTitle;
  const consequences = impact ? formatUpdateImpactConsequences(impact) : [];

  return (
    <ConsequenceDialog
      open
      title={title}
      description={Texts.updatePositionDescription}
      consequences={consequences}
      confirmLabel={Texts.confirm}
      confirmDisabled={Boolean(impact && !impact.canUpdate)}
      loading={impact === null}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact?.canUpdate) return;
        await updateContractEmployee(contractId, contractEmployeeId, request, signal);
        if (!signal.aborted) {
          onSaved();
        }
      }}
    />
  );
};
