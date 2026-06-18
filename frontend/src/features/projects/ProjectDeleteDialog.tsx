import { useEffect, useState } from "react";
import { ConsequenceDialog } from "@/components/shared/dialogs/ConsequenceDialog";
import { Texts } from "@/constants/texts";
import { type DeleteProjectImpactResponse, deleteProject, getProjectDeleteImpact } from "./api";

interface ProjectDeleteDialogProps {
  projectId: string;
  projectName: string;
  onClose: () => void;
  onDeleted: () => void;
}

const formatCount = (template: string, count: number): string => {
  return template.replace("{count}", String(count));
};

const getDeleteConsequences = (impact: DeleteProjectImpactResponse): string[] => {
  const consequences: string[] = [];

  if (impact.contractCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactContracts, impact.contractCount));
  }

  if (impact.positionCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactPositions, impact.positionCount));
  }

  if (impact.draftProjectTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactProjectTimesheetsDraft, impact.draftProjectTimesheetCount));
  }

  if (impact.submittedProjectTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactProjectTimesheetsSubmitted, impact.submittedProjectTimesheetCount));
  }

  if (impact.approvedProjectTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactProjectTimesheetsApproved, impact.approvedProjectTimesheetCount));
  }

  if (impact.submittedAttendanceTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactAttendanceTimesheetsSubmitted, impact.submittedAttendanceTimesheetCount));
  }

  if (impact.approvedAttendanceTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactAttendanceTimesheetsApproved, impact.approvedAttendanceTimesheetCount));
  }

  consequences.push(Texts.deleteImpactAttendancePreserved);

  if (impact.hasProtectedTimesheets) {
    consequences.push(Texts.deleteImpactProtectedBlocked);
  }

  return consequences;
};

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
      consequences={impact ? getDeleteConsequences(impact) : []}
      confirmLabel={Texts.delete}
      confirmDisabled={!impact || !impact.canDelete}
      loading={loading}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact || !impact.canDelete) {
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
