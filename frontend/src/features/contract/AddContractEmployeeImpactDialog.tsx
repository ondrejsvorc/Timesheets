import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import { type AddContractEmployeeRequest, addContractEmployee } from "./api/addContractEmployee";
import { type AddContractEmployeeImpactRequest, type AddContractEmployeeImpactResponse, formatAddImpactConsequences, getAddContractEmployeeImpact } from "./api/addContractEmployeeImpact";

interface AddContractEmployeeImpactDialogProps {
  contractId: string;
  request: AddContractEmployeeRequest;
  impactRequest: AddContractEmployeeImpactRequest;
  onClose: () => void;
  onSaved: () => void;
}

export const AddContractEmployeeImpactDialog = ({ contractId, request, impactRequest, onClose, onSaved }: AddContractEmployeeImpactDialogProps) => {
  const [impact, setImpact] = useState<AddContractEmployeeImpactResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    getAddContractEmployeeImpact(contractId, impactRequest, controller.signal)
      .then(setImpact)
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [contractId, impactRequest]);

  return (
    <ConsequenceDialog
      open
      title={Texts.addImpactTitle}
      description={Texts.addImpactDescription}
      consequences={impact ? formatAddImpactConsequences(impact) : []}
      confirmLabel={Texts.confirm}
      confirmDisabled={!impact || !impact.canAdd}
      loading={loading}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact?.canAdd) {
          return;
        }
        await addContractEmployee(contractId, request, signal);
        if (!signal.aborted) {
          onSaved();
        }
      }}
    />
  );
};
