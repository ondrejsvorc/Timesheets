import type { FetcherWithComponents } from "react-router";
import { canConfirmProtectedDelete, FetcherConsequenceDialog, forceProtectedDelete } from "@/components/shared/dialogs/FetcherConsequenceDialog";
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

export const ProjectDeleteDialog = ({ projectId, projectName, fetcher, onClose, onDeleted }: ProjectDeleteDialogProps) => (
  <FetcherConsequenceDialog
    fetcher={fetcher}
    title={Texts.deleteTitle.replace("{name}", projectName)}
    description={Texts.deleteDescription}
    confirmLabel={Texts.delete}
    formatConsequences={formatProjectDeleteImpactConsequences}
    canConfirm={canConfirmProtectedDelete}
    onClose={onClose}
    onConfirm={async (impact, signal) => {
      await deleteProject(projectId, { force: forceProtectedDelete(impact) }, signal);
      if (!signal.aborted) {
        onDeleted();
      }
    }}
  />
);
