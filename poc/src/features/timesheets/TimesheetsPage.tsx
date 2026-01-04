import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { type AuditDay, TimesheetAuditGrid } from "./TimesheetAuditGrid";
import { TimesheetSummary } from "./TimesheetSummary";

// /timesheets/{employeeId}/{year}/{month}
export const TimesheetsPage = () => {
  const days: AuditDay[] = [
    { day: 1 },
    { day: 2 },
    { day: 3 },
    { day: 4, issue: "warning" },
    { day: 5 },
    { day: 6 },
    { day: 7 },
    { day: 8 },
    { day: 9 },
    { day: 10, issue: "warning" },
    { day: 11 },
    { day: 12 },
    { day: 13 },
    { day: 14, issue: "warning" },
    { day: 15 },
    { day: 16 },
    { day: 17 },
    { day: 18 },
    { day: 19, issue: "warning" },
    { day: 20 },
    { day: 21 },
    { day: 22 },
    { day: 23 },
    { day: 24, issue: "error" },
    { day: 25 },
    { day: 26 },
    { day: 27 },
    { day: 28, issue: "error" },
    { day: 29 },
    { day: 30 },
    { day: 31 },
  ];

  const employeeId = "000";
  const year = 2026;
  const month = "leden";

  return (
    <>
      <PageHeader leading={<BackButton onClick={() => {}} />}>
        <PageTitle>Správa výkazů</PageTitle>
        <PageSubtitle>Ing. Jan Novák / 2154 / 01/2025</PageSubtitle>
      </PageHeader>
      <div className="space-y-6">
        <TimesheetSummary />
        <TimesheetAuditGrid
          days={days}
          onDayClick={(day: number): void => {
            throw new Error("Function not implemented.");
          }}
        />
      </div>
    </>
  );
};
