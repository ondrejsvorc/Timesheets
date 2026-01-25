import { Pencil, Plus, Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "../ui/dialog";
import { TimeSmartInput } from "./TimeSmartInput";
import type { TimeRange } from "./Timesheet";

interface ScheduleCellProps {
  schedules: TimeRange[];
  onClick: () => void; // Místo vnitřního dialogu jen voláme akci
}

export const ScheduleCell = ({ schedules = [], onClick }: ScheduleCellProps) => {
  const hasSchedules = schedules.length > 0;

  return (
    <div className="flex items-center justify-center min-h-[40px] w-full py-1 cursor-pointer" onClick={onClick}>
      {hasSchedules ? (
        <div className="flex items-center gap-1 group w-full">
          <div className="flex flex-wrap gap-1 justify-center flex-1 overflow-hidden">
            {schedules.map((s, i) => (
              <Badge
                key={i}
                variant="secondary"
                className="text-[10px] px-1.5 h-5 font-mono bg-blue-50 text-blue-700 border-blue-200 whitespace-nowrap"
              >
                {s.start}-{s.end}
              </Badge>
            ))}
          </div>
          <Button variant="ghost" size="icon" className="size-6 opacity-0 group-hover:opacity-100 transition-opacity shrink-0">
            <Pencil className="size-3" />
          </Button>
        </div>
      ) : (
        <Button
          variant="ghost"
          size="sm"
          className="h-7 px-2 text-[10px] text-slate-400 hover:text-blue-600 hover:bg-blue-50 border border-dashed border-slate-200"
        >
          <Plus className="size-3 mr-1" /> Rozvrh
        </Button>
      )}
    </div>
  );
};

interface ScheduleEditorModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  initialSchedules: TimeRange[];
  onSave: (newSchedules: TimeRange[]) => void;
  dateLabel?: string;
}

export const ScheduleEditorModal = ({ isOpen, onOpenChange, initialSchedules, onSave, dateLabel }: ScheduleEditorModalProps) => {
  const [tempSchedules, setTempSchedules] = useState<TimeRange[]>([]);

  useEffect(() => {
    if (isOpen) {
      setTempSchedules(initialSchedules.length > 0 ? initialSchedules : [{ start: "", end: "" }]);
    }
  }, [isOpen, initialSchedules]);

  const handleSave = () => {
    const valid = tempSchedules.filter((s) => s.start.trim() !== "" && s.end.trim() !== "");
    onSave(valid);
    onOpenChange(false);
  };

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[400px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Pencil className="size-4 text-blue-600" />
            Rozvrh STAG <span className="text-slate-400 font-normal">{dateLabel}</span>
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-3 py-4 max-h-[50vh] overflow-y-auto px-1">
          {tempSchedules.map((interval, index) => (
            <div key={index} className="flex items-center gap-2 group animate-in fade-in slide-in-from-top-1">
              <div className="grid grid-cols-2 gap-2 flex-1">
                <TimeSmartInput
                  value={interval.start}
                  onChange={(val) => {
                    const next = [...tempSchedules];
                    next[index] = { ...next[index], start: val };
                    setTempSchedules(next);
                  }}
                />
                <TimeSmartInput
                  value={interval.end}
                  onChange={(val) => {
                    const next = [...tempSchedules];
                    next[index] = { ...next[index], end: val };
                    setTempSchedules(next);
                  }}
                />
              </div>
              <Button
                variant="ghost"
                size="icon"
                className="size-8 text-slate-400 hover:text-red-600 hover:bg-red-50"
                onClick={() => setTempSchedules(tempSchedules.filter((_, i) => i !== index))}
              >
                <Trash2 className="size-4" />
              </Button>
            </div>
          ))}

          <Button
            variant="outline"
            size="sm"
            className="w-full border-dashed py-5 border-2 hover:border-blue-400 hover:bg-blue-50 text-slate-500"
            onClick={() => setTempSchedules([...tempSchedules, { start: "", end: "" }])}
          >
            <Plus className="size-4 mr-2" /> Přidat další interval
          </Button>
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={() => onOpenChange(false)}>
            Zrušit
          </Button>
          <Button onClick={handleSave} className="bg-blue-600 hover:bg-blue-700 text-white">
            Uložit rozvrh
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
