import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import type { TimeRange } from "./Timesheet";

export const ScheduleCell = ({ schedules, onClick }: { schedules: TimeRange[]; onClick: () => void }) => {
  const label = schedules.length === 0 ? "-" : `${schedules.length}×`;
  return (
    <Button type="button" variant="ghost" className="h-8 px-2 text-xs" onClick={onClick}>
      {label}
    </Button>
  );
};

type DraftRange = TimeRange & { id: string };

const createDraftId = () => {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
};

export const ScheduleEditorModal = ({
  isOpen,
  onOpenChange,
  initialSchedules,
  dateLabel,
  onSave,
}: {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  initialSchedules: TimeRange[];
  dateLabel?: string;
  onSave: (newSchedules: TimeRange[]) => void;
}) => {
  const [draft, setDraft] = useState<DraftRange[]>(() => initialSchedules.map((r) => ({ ...r, id: createDraftId() })));

  const normalized = useMemo(
    () =>
      draft.map((r) => ({
        start: r.start.trim(),
        end: r.end.trim(),
      })),
    [draft],
  );

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (open) setDraft(initialSchedules.map((r) => ({ ...r, id: createDraftId() })));
        onOpenChange(open);
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Rozvrh {dateLabel ? `– ${dateLabel}` : ""}</DialogTitle>
        </DialogHeader>

        <div className="space-y-2">
          {draft.map((range, idx) => (
            <div key={range.id} className="flex items-center gap-2">
              <Input
                className="h-8"
                placeholder="HH:MM"
                value={range.start}
                onChange={(e) => setDraft((cur) => cur.map((r, i) => (i === idx ? { ...r, start: e.currentTarget.value } : r)))}
              />
              <span className="text-xs text-muted-foreground">–</span>
              <Input
                className="h-8"
                placeholder="HH:MM"
                value={range.end}
                onChange={(e) => setDraft((cur) => cur.map((r, i) => (i === idx ? { ...r, end: e.currentTarget.value } : r)))}
              />
              <Button type="button" variant="ghost" className="h-8 px-2" onClick={() => setDraft((cur) => cur.filter((_, i) => i !== idx))}>
                ×
              </Button>
            </div>
          ))}
        </div>

        <div className="pt-2">
          <Button
            type="button"
            variant="outline"
            className="h-8"
            onClick={() => setDraft((cur) => [...cur, { id: createDraftId(), start: "", end: "" }])}
          >
            Přidat
          </Button>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Zrušit
          </Button>
          <Button
            type="button"
            onClick={() => {
              onSave(normalized.filter((r) => r.start !== "" || r.end !== ""));
              onOpenChange(false);
            }}
          >
            Uložit
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
