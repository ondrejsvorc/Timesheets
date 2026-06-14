import { useEffect } from "react";
import { useFetcher } from "react-router";
import { FetcherConsequenceDialog } from "@/components/shared/dialogs/FetcherConsequenceDialog";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { type ContractEmployeeUpdateImpactResponse, formatUpdateImpactConsequences } from "./api/contractEmployeeUpdateImpact";
import { type UpdateContractEmployeeRequest, updateContractEmployee } from "./api/updateContractEmployee";

interface ContractEmployeeUpdateDialogProps {
  contractId: string;
  contractEmployeeId: string;
  request: UpdateContractEmployeeRequest;
  onClose: () => void;
  onSaved: () => void;
}

export const ContractEmployeeUpdateDialog = ({ contractId, contractEmployeeId, request, onClose, onSaved }: ContractEmployeeUpdateDialogProps) => {
  const fetcher = useFetcher<ContractEmployeeUpdateImpactResponse>();

  useEffect(() => {
    fetcher.submit({ ...request }, { method: "POST", encType: "application/json", action: Routes.resourceContractEmployeeUpdateImpact(contractId, contractEmployeeId) });
  }, [contractId, contractEmployeeId, request, fetcher]);

  return (
    <FetcherConsequenceDialog
      fetcher={fetcher}
      title={Texts.editPositionTitle}
      description={Texts.updatePositionDescription}
      confirmLabel={Texts.confirm}
      formatConsequences={formatUpdateImpactConsequences}
      canConfirm={(impact) => impact.canUpdate}
      onClose={onClose}
      onConfirm={async (_impact, signal) => {
        await updateContractEmployee(contractId, contractEmployeeId, request, signal);
        if (!signal.aborted) {
          onSaved();
        }
      }}
    />
  );
};
