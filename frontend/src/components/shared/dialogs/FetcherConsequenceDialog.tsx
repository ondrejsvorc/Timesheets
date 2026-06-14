import type { FetcherWithComponents } from "react-router";
import { Texts } from "@/constants/texts";
import { ConsequenceDialog } from "./ConsequenceDialog";

export interface ProtectedDeleteImpact {
  hasProtectedTimesheets: boolean;
  canForceDelete: boolean;
}

export const canConfirmProtectedDelete = (impact: ProtectedDeleteImpact) => !impact.hasProtectedTimesheets || impact.canForceDelete;
export const forceProtectedDelete = (impact: ProtectedDeleteImpact) => impact.hasProtectedTimesheets && impact.canForceDelete;

interface FetcherConsequenceDialogProps<TImpact> {
  fetcher: FetcherWithComponents<TImpact>;
  title: string;
  description: string;
  confirmLabel: string;
  formatConsequences: (impact: TImpact) => string[];
  canConfirm: (impact: TImpact) => boolean;
  onClose: () => void;
  onConfirm: (impact: TImpact, signal: AbortSignal) => Promise<void>;
}

export const FetcherConsequenceDialog = <TImpact,>({
  fetcher,
  title,
  description,
  confirmLabel,
  formatConsequences,
  canConfirm,
  onClose,
  onConfirm,
}: FetcherConsequenceDialogProps<TImpact>) => {
  const impact = fetcher.data;
  const loading = fetcher.state === "loading";

  return (
    <ConsequenceDialog
      open
      title={title}
      description={description}
      consequences={impact ? formatConsequences(impact) : []}
      confirmLabel={confirmLabel}
      confirmDisabled={!impact || !canConfirm(impact)}
      loading={loading}
      loadingContent={<p className="text-sm text-muted-foreground">{Texts.deleteImpactLoading}</p>}
      onCancel={onClose}
      onConfirm={async (_event, signal) => {
        if (!impact || !canConfirm(impact)) return;
        await onConfirm(impact, signal);
      }}
    />
  );
};
