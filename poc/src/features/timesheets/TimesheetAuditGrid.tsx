import { Table, TableBody, TableCell, TableRow } from "@/components/ui/table";

type DayIssue = "warning" | "error";

export type AuditDay = {
  day: number;
  issue?: DayIssue;
};

function getStatusClass(status: AuditDay["issue"]): string {
  switch (status) {
    case "warning":
      return "bg-yellow-400";
    case "error":
      return "bg-red-500";
    default:
      return "bg-green-500";
  }
}

type TimesheetAuditGridProps = {
  days: AuditDay[];
  onDayClick: (day: number) => void;
};

export const TimesheetAuditGrid = ({ days, onDayClick }: TimesheetAuditGridProps) => {
  return (
    <div className="rounded-md border p-4 overflow-x-auto">
      <Table className="w-full table-fixed">
        <TableBody>
          <TableRow className="hover:bg-transparent">
            {days.map((day) => (
              <TableCell key={`day-${day.day}`} className="text-center text-sm text-muted-foreground p-2">
                {day.day}
              </TableCell>
            ))}
          </TableRow>
          <TableRow className="hover:bg-transparent">
            {days.map((day) => (
              <TableCell key={`status-${day.day}`} className="p-2 text-center">
                <button
                  type="button"
                  className={`
                    cursor-pointer mx-auto
                    h-6 w-6 rounded-sm
                    ${getStatusClass(day.issue)}
                    transition-transform duration-150 ease-out
                    hover:scale-110
                    focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2
                  `}
                  onClick={() => onDayClick(day.day)}
                  aria-label={`Otevřít den ${day.day}`}
                  title={`Den ${day.day}`}
                />
              </TableCell>
            ))}
          </TableRow>
        </TableBody>
      </Table>
    </div>
  );
};
