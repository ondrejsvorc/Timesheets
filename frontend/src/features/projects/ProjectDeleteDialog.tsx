import type { FetcherWithComponents } from "react-router";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import { deleteProject } from "./api/deleteProject";
import { type DeleteProjectImpactResponse, formatProjectDeleteImpactConsequences } from "./api/projectDeleteImpact";

interface ProjectDeleteDialogProps {
  projectId: string;
  projectName: string;
  fetcher: FetcherWithComponents<DeleteProjectImpactResponse>;
  onClose: () => void;
  onDeleted: () => void;
}

export const ProjectDeleteDialog = ({ projectId, projectName, fetcher, onClose, onDeleted }: ProjectDeleteDialogProps) => {
  const impact = fetcher.data;
  const loading = fetcher.state === "loading";

  const title = Texts.deleteTitle.replace("{name}", projectName);
  const consequences = impact ? formatProjectDeleteImpactConsequences(impact) : [];
  const confirmDisabled = !impact || Boolean(impact.hasProtectedTimesheets && !impact.canForceDelete);
  const useForce = Boolean(impact?.hasProtectedTimesheets && impact.canForceDelete);

  return (
    <ConsequenceDialog
      open
      title={title}
      description={Texts.deleteDescription}
      consequences={consequences}
      confirmLabel={Texts.delete}
      confirmDisabled={confirmDisabled}
      loading={loading}
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
