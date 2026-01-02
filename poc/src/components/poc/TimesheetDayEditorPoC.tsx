// TimesheetDayEditorPoC.tsx
// ===========================
// Editor pro úpravu konkrétního dne timesheetu

import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { toast } from "sonner";
import { CheckCircle2, AlertTriangle, XCircle, Trash2, ArrowLeft } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import type {
  CombinedDayState,
  CombinedTimesheetState,
  TimeInterval,
  ProjectDayState,
} from "./CombinedTimesheetTableTanStackPoC";
import { generateTimesheet } from "./CombinedTimesheetTableTanStackPoC";

/* =========================================================
   TYPES & HELPERS
========================================================= */

type DayStatus = "ok" | "warning" | "error";

const DAY_NAMES_FULL_CZ = ["Neděle", "Pondělí", "Úterý", "Středa", "Čtvrtek", "Pátek", "Sobota"] as const;

const getDayNameFull = (date: Date) => DAY_NAMES_FULL_CZ[date.getDay()];

const toMinutes = (t: string) => {
  const [h, m] = t.split(":").map(Number);
  return h * 60 + m;
};

const calculateWorkedHours = (arrival?: string, departure?: string, breakStart?: string, breakEnd?: string): number => {
  if (!arrival || !departure) return 0;
  let minutes = toMinutes(departure) - toMinutes(arrival);
  if (breakStart && breakEnd) {
    minutes -= toMinutes(breakEnd) - toMinutes(breakStart);
  }
  return Math.max(0, minutes / 60);
};

const calculateIntervalsHours = (intervals: TimeInterval[]) =>
  intervals.reduce((s, i) => s + Math.max(0, (toMinutes(i.to) - toMinutes(i.from)) / 60), 0);

const calculateProjectsHours = (projects: Record<string, ProjectDayState>): number => {
  return Object.values(projects).reduce((sum, p) => sum + (p.hours || 0), 0);
};

const calculateDayStatus = (day: CombinedDayState): DayStatus => {
  const worked = calculateWorkedHours(
    day.attendance.arrival,
    day.attendance.departure,
    day.attendance.breakStart,
    day.attendance.breakEnd,
  );
  const projects = calculateProjectsHours(day.projects);
  const stag = calculateIntervalsHours(day.attendance.stagIntervals);
  const difference = worked - day.attendance.dailyObligation;
  const isWeekend = day.date.getDay() === 0 || day.date.getDay() === 6;

  // Chyby
  if (difference !== 0) return "error";
  if (projects > worked) return "error";
  if (stag > worked) return "error";

  // Varování
  if (isWeekend && worked > 0) return "warning";

  return "ok";
};

const getStatusIcon = (status: DayStatus) => {
  switch (status) {
    case "ok":
      return <CheckCircle2 className="h-4 w-4 text-green-600" />;
    case "warning":
      return <AlertTriangle className="h-4 w-4 text-yellow-600" />;
    case "error":
      return <XCircle className="h-4 w-4 text-red-600" />;
  }
};

const getStatusText = (status: DayStatus) => {
  switch (status) {
    case "ok":
      return "Vše v pořádku";
    case "warning":
      return "Varování";
    case "error":
      return "Chyba";
  }
};

const getProblems = (day: CombinedDayState): string[] => {
  const problems: string[] = [];
  const worked = calculateWorkedHours(
    day.attendance.arrival,
    day.attendance.departure,
    day.attendance.breakStart,
    day.attendance.breakEnd,
  );
  const projects = calculateProjectsHours(day.projects);
  const stag = calculateIntervalsHours(day.attendance.stagIntervals);
  const difference = worked - day.attendance.dailyObligation;

  if (difference !== 0) {
    problems.push(`Rozdíl: ${difference > 0 ? "+" : ""}${difference.toFixed(1)} h`);
  }
  if (projects > worked) {
    problems.push(`Projektové hodiny (${projects.toFixed(1)} h) > docházka (${worked.toFixed(1)} h)`);
  }
  if (stag > worked) {
    problems.push(`STAG (${stag.toFixed(1)} h) > docházka (${worked.toFixed(1)} h)`);
  }

  return problems;
};

/* =========================================================
   MOCK DATA
========================================================= */

const initialProjects = [
  { id: "project-a", workload: 0.15, name: "Project A" },
  { id: "project-b", workload: 0.2, name: "Project B" },
  { id: "project-c", workload: 0.15, name: "Project C" },
  { id: "project-d", workload: 0.15, name: "Project D" },
  { id: "project-e", workload: 0.15, name: "Project E" },
];

// TODO: V produkci by se data načítala z API podle parametrů
const getMockData = (): CombinedTimesheetState => {
  const initial = generateTimesheet(2026, 1, 1.0, initialProjects);
  const mockData = structuredClone(initial);
  mockData.days[0].attendance.arrival = "08:00";
  mockData.days[0].attendance.departure = "16:00";
  mockData.days[1].attendance.arrival = "08:00";
  mockData.days[1].attendance.departure = "15:30";
  mockData.days[1].projects["project-a"].hours = 4.5;
  mockData.days[1].projects["project-b"].hours = 4.0;
  mockData.days[2].attendance.stagIntervals = [
    { from: "09:00", to: "10:00" },
    { from: "13:00", to: "14:00" },
  ];
  return mockData;
};

/* =========================================================
   DAY EDITOR COMPONENT
========================================================= */

export const TimesheetDayEditor = () => {
  const navigate = useNavigate();
  const params = useParams<{ year: string; month: string; day: string }>();
  
  const year = parseInt(params.year || "2026", 10);
  const month = parseInt(params.month || "1", 10);
  const day = parseInt(params.day || "1", 10);

  const mockData = getMockData();
  const dayDate = new Date(year, month - 1, day);
  const dayData = mockData.days.find(
    (d) =>
      d.date.getFullYear() === year &&
      d.date.getMonth() === month - 1 &&
      d.date.getDate() === day
  );

  if (!dayData) {
    return (
      <div className="p-6">
        <div className="text-lg font-semibold mb-2">Den nenalezen</div>
        <div className="text-muted-foreground mb-4">
          Den {day}. {month}. {year} nebyl nalezen v timesheetu.
        </div>
        <Button onClick={() => navigate("/timesheet")}>Zpět na přehled</Button>
      </div>
    );
  }

  const [editedDay, setEditedDay] = useState<CombinedDayState>(structuredClone(dayData));
  const [stagDialogOpen, setStagDialogOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const arrivalInputRef = useRef<HTMLInputElement>(null);

  const worked = calculateWorkedHours(
    editedDay.attendance.arrival,
    editedDay.attendance.departure,
    editedDay.attendance.breakStart,
    editedDay.attendance.breakEnd,
  );
  const projectsHours = calculateProjectsHours(editedDay.projects);
  const stagHours = calculateIntervalsHours(editedDay.attendance.stagIntervals);
  const difference = worked - editedDay.attendance.dailyObligation;
  const problems = getProblems(editedDay);
  const status = calculateDayStatus(editedDay);

  // Detekce změn
  useEffect(() => {
    const hasChanges = JSON.stringify(editedDay) !== JSON.stringify(dayData);
    setHasChanges(hasChanges);
  }, [editedDay, dayData]);

  // Auto-focus na první input při otevření
  useEffect(() => {
    if (arrivalInputRef.current) {
      arrivalInputRef.current.focus();
    }
  }, []);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl+S nebo Cmd+S pro uložení
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        if (hasChanges && !isSaving) {
          handleSave();
        }
      }
      // Esc pro zavření
      if (e.key === "Escape") {
        if (stagDialogOpen) {
          setStagDialogOpen(false);
        } else {
          handleCancel();
        }
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasChanges, isSaving, stagDialogOpen]);

  // Najde další problémový den pro scroll
  const findNextProblematicDay = (): number | null => {
    const allDays = mockData.days;
    const currentIndex = allDays.findIndex(
      (d) =>
        d.date.getFullYear() === year &&
        d.date.getMonth() === month - 1 &&
        d.date.getDate() === day
    );
    
    for (let i = currentIndex + 1; i < allDays.length; i++) {
      const dayStatus = calculateDayStatus(allDays[i]);
      if (dayStatus === "error" || dayStatus === "warning") {
        return i;
      }
    }
    return null;
  };

  const handleSave = async () => {
    if (isSaving || !hasChanges) return;
    
    setIsSaving(true);
    try {
      // TODO: V produkci by se zde volalo API pro uložení
      // await saveDay(editedDay);
      await new Promise((resolve) => setTimeout(resolve, 500)); // Simulace API call
      
      toast.success("Změny byly úspěšně uloženy");
      
      // Najdi další problémový den
      const nextProblematicIndex = findNextProblematicDay();
      
      // Naviguj zpět na přehled s možností scrollu
      if (nextProblematicIndex !== null) {
        navigate(`/timesheet?scrollTo=${nextProblematicIndex}`);
      } else {
        navigate("/timesheet");
      }
    } catch (error) {
      toast.error("Chyba při ukládání změn");
      console.error("Error saving day:", error);
    } finally {
      setIsSaving(false);
    }
  };

  const handleCancel = () => {
    // Najdi další problémový den
    const nextProblematicIndex = findNextProblematicDay();
    
    // Naviguj zpět na přehled s možností scrollu
    if (nextProblematicIndex !== null) {
      navigate(`/timesheet?scrollTo=${nextProblematicIndex}`);
    } else {
      navigate("/timesheet");
    }
  };

  const handleReset = () => {
    setEditedDay(structuredClone(dayData));
  };

  return (
    <div className="space-y-6 p-6">
      <Card className="gap-0">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <span>📅</span>
              <span>
                {getDayNameFull(editedDay.date)} {editedDay.date.toLocaleDateString("cs-CZ", {
                  day: "numeric",
                  month: "numeric",
                  year: "numeric",
                })}
              </span>
            </CardTitle>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleCancel}
              className="gap-2"
            >
              <ArrowLeft className="h-4 w-4" />
              Zpět na přehled
            </Button>
          </div>
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <span>Stav:</span>
              {getStatusIcon(status)}
              <span className="font-medium">{getStatusText(status)}</span>
            </div>
            {problems.length > 0 && (
              <div>
                <div className="font-medium text-sm mb-1">Problémy:</div>
                <ul className="list-disc list-inside text-sm space-y-1">
                  {problems.map((p, idx) => (
                    <li key={idx}>{p}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-6 pt-2">
            {/* První řada: Docházka a Projekty vedle sebe */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              {/* Sekce 1: Docházka */}
              <Card className="flex flex-col max-h-[350px] gap-0">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">Docházka (zdroj)</CardTitle>
                </CardHeader>
                <CardContent className="space-y-5 overflow-y-auto flex-1 flex flex-col">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="arrival" className="text-sm font-medium text-muted-foreground">
                        Příchod
                      </Label>
                      <Input
                        ref={arrivalInputRef}
                        id="arrival"
                        type="time"
                        value={editedDay.attendance.arrival ?? ""}
                        onChange={(e) =>
                          setEditedDay((d) => {
                            d.attendance.arrival = e.target.value || undefined;
                            return structuredClone(d);
                          })
                        }
                        className="w-full"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="departure" className="text-sm font-medium text-muted-foreground">
                        Odchod
                      </Label>
                      <Input
                        id="departure"
                        type="time"
                        value={editedDay.attendance.departure ?? ""}
                        onChange={(e) =>
                          setEditedDay((d) => {
                            d.attendance.departure = e.target.value || undefined;
                            return structuredClone(d);
                          })
                        }
                        className="w-full"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="breakStart" className="text-sm font-medium text-muted-foreground">
                        Začátek přestávky
                      </Label>
                      <Input
                        id="breakStart"
                        type="time"
                        value={editedDay.attendance.breakStart ?? ""}
                        onChange={(e) =>
                          setEditedDay((d) => {
                            d.attendance.breakStart = e.target.value || undefined;
                            return structuredClone(d);
                          })
                        }
                        className="w-full"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="breakEnd" className="text-sm font-medium text-muted-foreground">
                        Konec přestávky
                      </Label>
                      <Input
                        id="breakEnd"
                        type="time"
                        value={editedDay.attendance.breakEnd ?? ""}
                        onChange={(e) =>
                          setEditedDay((d) => {
                            d.attendance.breakEnd = e.target.value || undefined;
                            return structuredClone(d);
                          })
                        }
                        className="w-full"
                      />
                    </div>
                  </div>
                  <div className="mt-auto pt-4">
                    <Separator />
                    <div className="flex justify-between items-center pt-3">
                      <div className="text-sm">
                        <span className="text-muted-foreground">Celkem:</span>{" "}
                        <span className="font-semibold tabular-nums">{worked.toFixed(1)} h</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Povinnost:</span>{" "}
                        <span className="font-semibold tabular-nums">{editedDay.attendance.dailyObligation.toFixed(1)} h</span>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Sekce 2: Projekty */}
              <Card className="flex flex-col max-h-[350px] gap-0">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">Projekty</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 overflow-y-auto flex-1 flex flex-col">
                  <div className="space-y-2.5">
                    {initialProjects.map((project) => {
                      const projectState = editedDay.projects[project.id];
                      const projectHours = projectState?.hours || 0;
                      const projectObligation = projectState?.obligation || 0;
                      const hasWarning = projectHours > projectObligation;

                      return (
                        <div key={project.id} className="flex justify-between items-center gap-4">
                          <div className="flex items-center gap-2 flex-1 min-w-0">
                            <Label htmlFor={`project-${project.id}`} className="text-sm text-muted-foreground whitespace-nowrap">
                              {project.name}:
                            </Label>
                            {hasWarning && <AlertTriangle className="h-4 w-4 text-yellow-600 shrink-0" />}
                          </div>
                          <div className="flex items-center gap-3">
                            <Input
                              id={`project-${project.id}`}
                              type="number"
                              step="0.1"
                              value={projectHours || ""}
                              onChange={(e) =>
                                setEditedDay((d) => {
                                  if (!d.projects[project.id]) {
                                    d.projects[project.id] = { hours: undefined, obligation: projectObligation };
                                  }
                                  d.projects[project.id].hours = e.target.value ? Number(e.target.value) : undefined;
                                  return structuredClone(d);
                                })
                              }
                              className="w-24"
                              placeholder="0.0"
                            />
                            <span className="text-xs text-muted-foreground whitespace-nowrap tabular-nums">
                              ({projectObligation.toFixed(1)} h)
                            </span>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                  <Separator className="my-3" />
                  <div className="text-sm sticky bottom-0 bg-card pt-2 pb-1">
                    <div className="flex justify-between items-center">
                      <span className="text-muted-foreground">Celkem projekty:</span>
                      <span className="font-semibold tabular-nums">{projectsHours.toFixed(1)} h</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Druhá řada: STAG a Výsledek vedle sebe */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              {/* Sekce 3: STAG */}
              <Card className="flex flex-col max-h-[350px] gap-0">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">Rozvrhové akce STAG</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4 overflow-y-auto flex-1 flex flex-col">
                  <div className="space-y-2">
                    {editedDay.attendance.stagIntervals.length > 0 ? (
                      <div className="flex flex-wrap gap-2">
                        {editedDay.attendance.stagIntervals.map((interval, idx) => (
                          <Badge key={idx} variant="secondary" className="text-xs py-1.5 px-3">
                            {interval.from} – {interval.to}
                          </Badge>
                        ))}
                      </div>
                    ) : (
                      <span className="text-sm text-muted-foreground">Žádné intervaly</span>
                    )}
                  </div>
                  <div className="mt-auto pt-4">
                    <Button variant="outline" onClick={() => setStagDialogOpen(true)} className="self-start">
                      Upravit rozmezí
                    </Button>
                    <Separator className="my-3" />
                    <div className="text-sm">
                      <span className="text-muted-foreground">Celkem STAG:</span>{" "}
                      <span className="font-semibold tabular-nums">{stagHours.toFixed(1)} h</span>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Sekce 4: Výsledek */}
              <Card className="flex flex-col max-h-[350px] gap-0">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">Výsledek po úpravách</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 overflow-y-auto flex-1 flex flex-col">
                  <div className="space-y-2.5">
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-muted-foreground">Docházka:</span>
                      <span className="text-sm font-semibold tabular-nums">{worked.toFixed(1)} h</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-muted-foreground">Projekty:</span>
                      <span className="text-sm font-semibold tabular-nums">{projectsHours.toFixed(1)} h</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-muted-foreground">STAG:</span>
                      <span className="text-sm font-semibold tabular-nums">{stagHours.toFixed(1)} h</span>
                    </div>
                  </div>
                  <div className="mt-auto pt-4">
                    <Separator />
                    <div className="space-y-2.5 pt-3">
                      <div className="flex justify-between items-center">
                        <span className="text-sm text-muted-foreground">Povinnost:</span>
                        <span className="text-sm font-semibold tabular-nums">{editedDay.attendance.dailyObligation.toFixed(1)} h</span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-sm text-muted-foreground">Rozdíl:</span>
                        <span className={`text-sm font-semibold tabular-nums ${difference === 0 ? "text-green-600" : "text-red-600"}`}>
                          {difference > 0 ? "+" : ""}
                          {difference.toFixed(1)} h {difference === 0 && "✅"}
                        </span>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </CardContent>
        <div className="flex justify-end gap-2 px-6 pb-6 pt-4">
          <Button 
            variant="outline" 
            onClick={handleReset}
            disabled={!hasChanges || isSaving}
          >
            Resetovat úpravy
          </Button>
          <Button 
            variant="outline" 
            onClick={handleCancel}
            disabled={isSaving}
          >
            Zrušit
          </Button>
          <Button 
            onClick={handleSave}
            disabled={!hasChanges || isSaving}
          >
            {isSaving ? "Ukládám..." : "Uložit změny"}
          </Button>
        </div>
      </Card>

      {/* Sekundární dialog: Editace STAG rozmezí */}
      <Dialog open={stagDialogOpen} onOpenChange={setStagDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Upravit STAG rozmezí</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            {editedDay.attendance.stagIntervals.map((interval, idx) => (
              <div key={idx} className="flex items-end gap-3">
                <div className="grid grid-cols-2 gap-4 flex-1">
                  <div>
                    <Label>Od</Label>
                    <Input
                      type="time"
                      value={interval.from}
                      onChange={(e) =>
                        setEditedDay((d) => {
                          d.attendance.stagIntervals[idx].from = e.target.value;
                          return structuredClone(d);
                        })
                      }
                    />
                  </div>
                  <div>
                    <Label>Do</Label>
                    <Input
                      type="time"
                      value={interval.to}
                      onChange={(e) =>
                        setEditedDay((d) => {
                          d.attendance.stagIntervals[idx].to = e.target.value;
                          return structuredClone(d);
                        })
                      }
                    />
                  </div>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() =>
                    setEditedDay((d) => {
                      d.attendance.stagIntervals.splice(idx, 1);
                      return structuredClone(d);
                    })
                  }
                  className="shrink-0"
                >
                  <Trash2 className="h-4 w-4 text-destructive" />
                </Button>
              </div>
            ))}
            <Button
              variant="outline"
              onClick={() =>
                setEditedDay((d) => {
                  d.attendance.stagIntervals.push({ from: "09:00", to: "10:00" });
                  return structuredClone(d);
                })
              }
            >
              + Přidat interval
            </Button>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setStagDialogOpen(false)}>
              Zrušit
            </Button>
            <Button onClick={() => setStagDialogOpen(false)}>Uložit</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

