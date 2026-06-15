import { EmptyState } from "@/components/shared/data/EmptyState";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { SubPageHeader, SubPageSubtitle, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
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
              <TableHead>{Texts.timesheet}</TableHead>
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
                <TableCell className="font-medium">{item.label}</TableCell>
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
      </div>
      <TimesheetMonthSummary year={overview.year} month={overview.month} summary={overview.summary} />
    </div>
  );
};

const TimesheetMonthSummary = ({ year, month, summary }: { year: number; month: number; summary: CombinedTimesheetMonthSummary }) => (
  <div className="mb-6">
    <SubPageHeader>
      <SubPageTitle>{Texts.timesheetMonthSummary}</SubPageTitle>
      <SubPageSubtitle>
        {formatMonthYear(month, year)} · {formatDate(summary.periodStart)} – {formatDate(summary.periodEnd)}
      </SubPageSubtitle>
    </SubPageHeader>
    <div className="rounded-md border p-4 max-w-md">
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
  </div>
);

const SummaryRow = ({ label, value }: { label: string; value: string | number }) => (
  <TableRow>
    <TableCell className="text-muted-foreground">{label}</TableCell>
    <TableCell className="text-right font-medium tabular-nums">{value}</TableCell>
  </TableRow>
);
