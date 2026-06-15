import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import { canConfirmDelete } from "@/utils/deleteImpactConsequences";
import { deleteProject } from "./api/deleteProject";
import { type DeleteProjectImpactResponse, formatProjectDeleteImpactConsequences, getProjectDeleteImpact } from "./api/projectDeleteImpact";

interface ProjectDeleteDialogProps {
  projectId: string;
  projectName: string;
  onClose: () => void;
  onDeleted: () => void;
}

export const ProjectDeleteDialog = ({ projectId, projectName, onClose, onDeleted }: ProjectDeleteDialogProps) => {
  const [impact, setImpact] = useState<DeleteProjectImpactResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    getProjectDeleteImpact(projectId, controller.signal)
      .then(setImpact)
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [projectId]);

  return (
    <ConsequenceDialog
      open
      title={Texts.deleteTitle.replace("{name}", projectName)}
      description={Texts.deleteDescription}
      consequences={impact ? formatProjectDeleteImpactConsequences(impact) : []}
      confirmLabel={Texts.delete}
      confirmDisabled={!impact || !canConfirmDelete(impact)}
      loading={loading}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact || !canConfirmDelete(impact)) {
          return;
        }
        await deleteProject(projectId, signal);
        if (!signal.aborted) {
          onDeleted();
        }
      }}
    />
  );
};
