import { Badge } from "@/components/ui/badge";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";

const statusClassName = (status: string) => {
  switch (status) {
    case Texts.statusInProgress:
      return "border-transparent bg-secondary text-secondary-foreground";
    case Texts.statusPendingApproval:
      return "border-amber-200 bg-amber-50 text-amber-900 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-100";
    case Texts.statusApproved:
      return "border-green-200 bg-green-50 text-green-900 dark:border-green-900 dark:bg-green-950 dark:text-green-100";
    default:
      return "";
  }
};

interface TimesheetStatusBadgeProps {
  status: string | null | undefined;
}

export const TimesheetStatusBadge = ({ status }: TimesheetStatusBadgeProps) => {
  if (!status) {
    return (
      <span className="inline-grid">
        <Badge variant="outline" className="invisible col-start-1 row-start-1" aria-hidden="true">
          {Texts.statusInProgress}
        </Badge>
        <span className="col-start-1 row-start-1 flex items-center justify-center text-muted-foreground">{Texts.dash}</span>
      </span>
    );
  }

  return (
    <span className="inline-grid">
      <Badge variant="outline" className="invisible col-start-1 row-start-1" aria-hidden="true">
        {Texts.statusInProgress}
      </Badge>
      <Badge variant="outline" className={cn("col-start-1 row-start-1 justify-center", statusClassName(status))}>
        {status}
      </Badge>
    </span>
  );
};
