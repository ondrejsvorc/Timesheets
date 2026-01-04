import { Card, CardContent } from "@/components/ui/card";

export type Summary = {
  worked: number;
  obligation: number;
  breakdown: {
    core: number;
    projects: number;
    night: number;
    stag: number;
  };
  days: {
    total: number;
    holidays: number;
    vacation: number;
    sick: number;
  };
};

const formatHours = (value: number): string => {
  const isInteger: boolean = Number.isInteger(value);
  return isInteger ? `${value} h` : `${value.toFixed(2)} h`;
};

export const TimesheetSummary = () => {
  const summary: Summary = {
    // hlavní metrika
    worked: 180,
    obligation: 180,

    // rozpad odpracovaných hodin
    breakdown: {
      core: 100, // kmenový úvazek
      projects: 80, // zakázky
      night: 8, // z toho (podmnožina worked)
      stag: 18.5, // externí srovnání
    },

    // denní statistiky
    days: {
      total: 21, // pracovní dny v měsíci
      holidays: 1, // státní svátek
      vacation: 0, // dovolená
      sick: 0, // nemoc
    },
  };

  const difference: number = summary.worked - summary.obligation;
  const differenceClass: string = difference === 0 ? "text-green-600" : "text-red-600";

  return (
    <Card>
      <CardContent className="space-y-6">
        <div>
          <div className="text-muted-foreground text-sm">Odpracováno / Měsíční povinnost</div>
          <div className="text-2xl font-bold">
            {formatHours(summary.worked)} / {formatHours(summary.obligation)}
          </div>
          <div className={`text-sm font-semibold ${differenceClass}`}>
            Rozdíl: {difference > 0 ? "+" : ""}
            {formatHours(difference)}
          </div>
        </div>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
          <div>
            <div className="text-muted-foreground">Z toho kmenový úvazek</div>
            <div className="font-medium">{formatHours(summary.breakdown.core)}</div>
          </div>

          <div>
            <div className="text-muted-foreground">Z toho zakázky</div>
            <div className="font-medium">{formatHours(summary.breakdown.projects)}</div>
          </div>

          <div>
            <div className="text-muted-foreground">Z toho noční práce</div>
            <div className="font-medium">{formatHours(summary.breakdown.night)}</div>
          </div>

          <div>
            <div className="text-muted-foreground">Rozvrh (STAG)</div>
            <div className="font-medium">{formatHours(summary.breakdown.stag)}</div>
          </div>
        </div>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm border-t pt-4">
          <div>
            <div className="text-muted-foreground">Pracovní dny</div>
            <div className="font-semibold">{summary.days.total}</div>
          </div>

          <div>
            <div className="text-muted-foreground">Svátky</div>
            <div className="font-semibold">{summary.days.holidays}</div>
          </div>

          <div>
            <div className="text-muted-foreground">Dovolená</div>
            <div className="font-semibold">{summary.days.vacation}</div>
          </div>

          <div>
            <div className="text-muted-foreground">Nemoc</div>
            <div className="font-semibold">{summary.days.sick}</div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};
