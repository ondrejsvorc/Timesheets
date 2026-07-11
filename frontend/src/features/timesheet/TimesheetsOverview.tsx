import { EmptyState } from "@/components/shared/data/EmptyState";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { formatWorkload } from "@/utils/format";
import type { GetTimesheetOverviewResponse, TimesheetMonthSummary } from "./api";
import { TimesheetOverviewRowActions } from "./TimesheetOverviewRowActions";

interface TimesheetsOverviewProps {
  overview: GetTimesheetOverviewResponse;
}

export const TimesheetsOverview = ({ overview }: TimesheetsOverviewProps) => {
  if (overview.items.length === 0) {
    return <EmptyState />;
  }

  return (
    <>
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
                  <TableCell>{item.contractRegistrationNumber ?? Texts.dash}</TableCell>
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
      </div>

      <div className="mb-6">
        <SubPageHeader>
          <SubPageTitle>{Texts.timesheetDaysOverview}</SubPageTitle>
        </SubPageHeader>
        <TimesheetDaysOverview summary={overview.summary} />
      </div>
    </>
  );
};

const TimesheetDaysOverview = ({ summary }: { summary: TimesheetMonthSummary }) => (
  <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
    <DayStatCard label={Texts.summaryWorkdays} value={summary.workdays} />
    <DayStatCard label={Texts.summaryVacation} value={summary.vacationDays} />
    <DayStatCard label={Texts.summarySickDays} value={summary.sickDays} />
    <DayStatCard label={Texts.summaryHolidays} value={summary.holidays} />
  </div>
);

const DayStatCard = ({ label, value }: { label: string; value: number }) => (
  <div className="rounded-md border p-4">
    <div className="text-2xl font-semibold tabular-nums">{value}</div>
    <div className="mt-1 text-sm text-muted-foreground">{label}</div>
  </div>
);
