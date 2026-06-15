import { EmptyState } from "@/components/shared/data/EmptyState";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { formatMonthYear } from "@/features/contract/utils/czechMonths";
import { formatDate } from "@/utils/formatDate";
import { formatWorkload } from "../timesheetFormat";
import type { CombinedTimesheetMonthSummary, GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { TimesheetOverviewRowActions } from "./TimesheetOverviewRowActions";

interface TimesheetsOverviewProps {
  overview: GetCombinedTimesheetOverviewResponse;
}

export const TimesheetsOverview = ({ overview }: TimesheetsOverviewProps) => {
  if (overview.items.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="mb-6">
      <SubPageHeader>
        <SubPageTitle>{Texts.employeeTimesheets}</SubPageTitle>
      </SubPageHeader>
      <div className="rounded-md border p-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{Texts.timesheetPart}</TableHead>
              <TableHead>{Texts.timesheetStatus}</TableHead>
              <TableHead>{Texts.contract}</TableHead>
              <TableHead>{Texts.position}</TableHead>
              <TableHead>{Texts.workload}</TableHead>
              <TableHead>{Texts.contractManagers}</TableHead>
              <TableHead>{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {overview.items.map((item) => (
              <TableRow key={`${item.kind}-${item.timesheetId ?? item.label}`}>
                <TableCell>{item.label}</TableCell>
                <TableCell>
                  <TimesheetStatusBadge status={item.status} />
                </TableCell>
                <TableCell>{item.contractName ?? Texts.dash}</TableCell>
                <TableCell>{item.position ?? Texts.dash}</TableCell>
                <TableCell>{formatWorkload(item.workload)}</TableCell>
                <TableCell className="max-w-[20rem] whitespace-normal break-words">{item.managers.length > 0 ? item.managers.join(", ") : Texts.dash}</TableCell>
                <TableCell>
                  <TimesheetOverviewRowActions item={item} overview={overview} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <TimesheetMonthSummary year={overview.year} month={overview.month} summary={overview.summary} />
      </div>
    </div>
  );
};

const TimesheetMonthSummary = ({ year, month, summary }: { year: number; month: number; summary: CombinedTimesheetMonthSummary }) => (
  <div className="border-t mt-4 pt-4">
    <div className="mb-3 space-y-1">
      <h3 className="text-sm font-semibold">{Texts.timesheetMonthSummary}</h3>
      <p className="text-sm text-muted-foreground">
        {formatMonthYear(month, year)} · {formatDate(summary.periodStart)} – {formatDate(summary.periodEnd)}
      </p>
    </div>
    <Table>
      <TableBody>
        <SummaryRow label={Texts.summaryWorkdays} value={summary.workdays} />
        <SummaryRow label={Texts.summaryVacation} value={summary.vacationDays} />
        <SummaryRow label={Texts.summarySickDays} value={summary.sickDays} />
        <SummaryRow label={Texts.summaryHolidays} value={summary.holidays} />
        <SummaryRow label={Texts.summaryTotalWorkload} value={formatWorkload(summary.totalWorkload)} />
      </TableBody>
    </Table>
  </div>
);

const SummaryRow = ({ label, value }: { label: string; value: string | number }) => (
  <TableRow>
    <TableCell>{label}</TableCell>
    <TableCell>{value}</TableCell>
  </TableRow>
);
