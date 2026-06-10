import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import { deleteProject } from "./api/deleteProject";
import { formatDeleteImpactConsequences, getProjectDeleteImpact, type ProjectDeleteImpactResponse } from "./api/projectDeleteImpact";

interface ProjectDeleteDialogProps {
  projectId: string;
  projectName: string;
  onClose: () => void;
  onDeleted: () => void;
}

export const ProjectDeleteDialog = ({ projectId, projectName, onClose, onDeleted }: ProjectDeleteDialogProps) => {
  const [impact, setImpact] = useState<ProjectDeleteImpactResponse | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    getProjectDeleteImpact(projectId, controller.signal)
      .then(setImpact)
      .catch(() => {
        if (!controller.signal.aborted) {
          setImpact(null);
        }
      });
    return () => controller.abort();
  }, [projectId]);

  const title = Texts.deleteProjectTitle.replace("{name}", projectName);
  const consequences = impact ? formatDeleteImpactConsequences(impact, "project") : [];
  const confirmDisabled = Boolean(impact?.hasProtectedTimesheets && !impact.canForceDelete);
  const useForce = Boolean(impact?.hasProtectedTimesheets && impact.canForceDelete);

  return (
    <ConsequenceDialog
      open
      title={title}
      description={Texts.deleteProjectDescription}
      consequences={consequences}
      confirmLabel={Texts.deleteProjectConfirm}
      confirmDisabled={confirmDisabled}
      loading={impact === null}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact) return;
        await deleteProject(projectId, { force: useForce }, signal);
        if (!signal.aborted) {
          onDeleted();
        }
      }}
    />
  );
};
