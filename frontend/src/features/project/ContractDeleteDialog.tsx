import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import {
  formatDeleteImpactConsequences,
  getProjectContractDeleteImpact,
  type ProjectDeleteImpactResponse,
} from "../projects/api/projectDeleteImpact";
import { deleteProjectContract } from "./api/deleteProjectContract";

interface ContractDeleteDialogProps {
  projectId: string;
  contractId: string;
  contractName: string;
  onClose: () => void;
  onDeleted: () => void;
}

export const ContractDeleteDialog = ({ projectId, contractId, contractName, onClose, onDeleted }: ContractDeleteDialogProps) => {
  const [impact, setImpact] = useState<ProjectDeleteImpactResponse | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    getProjectContractDeleteImpact(projectId, contractId, controller.signal)
      .then(setImpact)
      .catch(() => {
        if (!controller.signal.aborted) {
          setImpact(null);
        }
      });
    return () => controller.abort();
  }, [projectId, contractId]);

  const title = Texts.deleteTitle.replace("{name}", contractName);
  const consequences = impact ? formatDeleteImpactConsequences(impact) : [];
  const confirmDisabled = Boolean(impact?.hasProtectedTimesheets && !impact.canForceDelete);
  const useForce = Boolean(impact?.hasProtectedTimesheets && impact.canForceDelete);

  return (
    <ConsequenceDialog
      open
      title={title}
      description={Texts.deleteDescription}
      consequences={consequences}
      confirmLabel={Texts.delete}
      confirmDisabled={confirmDisabled}
      loading={impact === null}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact) return;
        await deleteProjectContract(projectId, contractId, { force: useForce }, signal);
        if (!signal.aborted) {
          onDeleted();
        }
      }}
    />
  );
};
