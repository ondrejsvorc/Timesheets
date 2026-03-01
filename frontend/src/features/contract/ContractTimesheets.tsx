import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar, useFilterContext } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { ActionDropdownMenu, EditAction } from "@/components/shared/menus/ActionDropdownMenu";
import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "@/components/shared/inputs/MultiSelectComboBox";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Texts } from "@/constants/texts";
import { CheckCircle, XCircle } from "lucide-react";
import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigation } from "react-router";
import type {
  EmployeeGroup,
  GetContractTimesheetsResponse,
  MonthGroup,
} from "./api/getContractTimesheets";
import { CZECH_MONTH_NAMES, formatMonthYear } from "./utils/czechMonths";
import {
  type ContractTimesheetsFilterCriteria,
  useContractTimesheetsFilter,
} from "./hooks/useContractTimesheetsFilter";

const TIMESHEET_STATUS_OPTIONS: MultiSelectComboBoxItem[] = [
  { value: Texts.statusInProgress, label: Texts.statusInProgress },
  { value: Texts.statusPendingApproval, label: Texts.statusPendingApproval },
  { value: Texts.statusApproved, label: Texts.statusApproved },
];

const CURRENT_YEAR = new Date().getFullYear();
const YEAR_OPTIONS = [CURRENT_YEAR - 2, CURRENT_YEAR - 1, CURRENT_YEAR, CURRENT_YEAR + 1];
const MONTH_OPTIONS = Array.from({ length: 12 }, (_, i) => i + 1);

export const ContractTimesheets = () => {
  const loaderData = useLoaderData() as { promise: Promise<GetContractTimesheetsResponse> };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={loaderData.promise}>
        <ContractTimesheetsContent />
      </Await>
    </Suspense>
  );
};

const ContractTimesheetsContent = () => {
  const response = useAsyncValue() as GetContractTimesheetsResponse;
  const navigation = useNavigation();
  const { filter, setFilter } = useContractTimesheetsFilter();
  const isLoading = navigation.state === "loading";

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>Výkazy</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter}>
        <ContractTimesheetsFilterControls />
      </FilterBar>
      {filter.groupBy === "Month" ? (
        <TimesheetsByMonth
          months={response.months}
          employeesCount={response.employees.length}
          isLoading={isLoading}
        />
      ) : (
        <TimesheetsByEmployee
          employees={response.employees}
          monthsCount={response.months.length}
          isLoading={isLoading}
        />
      )}
    </>
  );
};

function ContractTimesheetsFilterControls() {
  const { filter, setFilter } = useFilterContext<ContractTimesheetsFilterCriteria>();

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.grouping}</Label>
        <Select
          value={filter.groupBy}
          onValueChange={(v: "Employee" | "Month") =>
            setFilter((draft) => {
              draft.groupBy = v;
            })
          }
        >
          <SelectTrigger className="w-[220px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Employee">{Texts.groupByEmployee}</SelectItem>
            <SelectItem value="Month">{Texts.groupByMonthAndEmployee}</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.periodFrom}</Label>
        <div className="flex gap-2">
        <Select
          value={String(filter.fromYear)}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.fromYear = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[88px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {YEAR_OPTIONS.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={String(filter.fromMonth)}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.fromMonth = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[120px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {MONTH_OPTIONS.map((m) => (
              <SelectItem key={m} value={String(m)}>
                {CZECH_MONTH_NAMES[m]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        </div>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.periodTo}</Label>
        <div className="flex gap-2">
        <Select
          value={String(filter.toYear)}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.toYear = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[88px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {YEAR_OPTIONS.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={String(filter.toMonth)}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.toMonth = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[120px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {MONTH_OPTIONS.map((m) => (
              <SelectItem key={m} value={String(m)}>
                {CZECH_MONTH_NAMES[m]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        </div>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.status}</Label>
        <MultiSelectComboBox
          value={filter.statuses ?? []}
          items={TIMESHEET_STATUS_OPTIONS}
          placeholder={Texts.status}
          maxVisibleItems={2}
          onChange={(value) =>
            setFilter((draft) => {
              draft.statuses = value.length ? value : undefined;
            })
          }
        />
      </div>
    </>
  );
}

function workloadPercent(workload: number | null): string {
  if (workload == null) return Texts.dash;
  return `${Math.round(workload * 100)}%`;
}

function ApprovalIcon({ approved }: { approved: boolean }) {
  return approved ? (
    <CheckCircle className="size-5 text-green-600" aria-hidden />
  ) : (
    <XCircle className="size-5 text-destructive" aria-hidden />
  );
}

interface TimesheetsByMonthProps {
  months: MonthGroup[];
  employeesCount: number;
  isLoading: boolean;
}

const TimesheetsByMonth = ({ months, employeesCount, isLoading }: TimesheetsByMonthProps) => {
  if (months.length === 0) {
    const isStaleEmpty = employeesCount > 0;
    return isStaleEmpty || isLoading ? <GenericSkeleton /> : <EmptyState />;
  }
  return (
    <div className="space-y-8">
      {months.map((monthGroup) => (
        <div key={`${monthGroup.year}-${monthGroup.month}`} className="space-y-6">
          <div className="font-medium text-foreground">
            {formatMonthYear(monthGroup.month, monthGroup.year)}
          </div>
          {monthGroup.items.map((employee) => (
            <div key={employee.id} className="rounded-md border p-4">
              <div className="mb-3 flex items-center gap-2 font-medium text-foreground">
                <ApprovalIcon approved={employee.allTimesheetsApproved} />
                <span>
                  {employee.fullName} · {employee.personalNumber ?? Texts.dash} · {employee.employeeType}
                </span>
              </div>
              {employee.timesheets.length === 0 ? (
                <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{Texts.position}</TableHead>
                      <TableHead>{Texts.workload}</TableHead>
                      <TableHead>{Texts.timesheetStatus}</TableHead>
                      <TableHead>{Texts.actions}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {employee.timesheets.map((item) => (
                      <TimesheetItemRow key={item.id} item={item} />
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          ))}
        </div>
      ))}
    </div>
  );
};

interface TimesheetItemRowProps {
  item: { id: string; position: string | null; workload: number | null; status: string };
}

function TimesheetItemRow({ item }: TimesheetItemRowProps) {
  return (
    <TableRow className="cursor-pointer">
      <TableCell>{item.position ?? Texts.dash}</TableCell>
      <TableCell>{workloadPercent(item.workload)}</TableCell>
      <TableCell>{item.status}</TableCell>
      <TableCell>
        <ActionDropdownMenu>
          <EditAction onClick={() => {}} />
        </ActionDropdownMenu>
      </TableCell>
    </TableRow>
  );
}

/** Seskupí výkazy zaměstnance po měsících (year, month), seřazené chronologicky. */
function groupTimesheetsByMonth(
  timesheets: Array<{ year?: number; month?: number; id: string; position: string | null; workload: number | null; status: string }>,
): { year: number; month: number; items: typeof timesheets }[] {
  const withPeriod = timesheets.filter((t): t is typeof t & { year: number; month: number } => t.year != null && t.month != null);
  const byKey = new Map<string, typeof withPeriod>();
  for (const t of withPeriod) {
    const key = `${t.year}-${t.month}`;
    const list = byKey.get(key) ?? [];
    list.push(t);
    byKey.set(key, list);
  }
  return Array.from(byKey.entries())
    .map(([key, items]) => {
      const [y, m] = key.split("-").map(Number) as [number, number];
      return { year: y, month: m, items };
    })
    .sort((a, b) => a.year !== b.year ? a.year - b.year : a.month - b.month);
}

interface TimesheetsByEmployeeProps {
  employees: EmployeeGroup[];
  monthsCount: number;
  isLoading: boolean;
}

const TimesheetsByEmployee = ({ employees, monthsCount, isLoading }: TimesheetsByEmployeeProps) => {
  if (employees.length === 0) {
    const isStaleEmpty = monthsCount > 0;
    return isStaleEmpty || isLoading ? <GenericSkeleton /> : <EmptyState />;
  }
  return (
    <div className="space-y-8">
      {employees.map((emp) => {
        const months = groupTimesheetsByMonth(emp.timesheets);
        return (
        <div key={emp.id} className="space-y-6">
          <div className="font-medium text-foreground">
            {emp.fullName} · {emp.personalNumber ?? Texts.dash} · {emp.employeeType}
          </div>
          {months.length === 0 ? (
            <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
          ) : (
            months.map((monthGroup) => {
              const monthApproved = monthGroup.items.every(
                (item) => item.status === Texts.statusApproved,
              );
              return (
              <div key={`${monthGroup.year}-${monthGroup.month}`} className="rounded-md border p-4">
                <div className="mb-3 flex items-center gap-2 font-medium text-foreground">
                  <ApprovalIcon approved={monthApproved} />
                  {formatMonthYear(monthGroup.month, monthGroup.year)}
                </div>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{Texts.position}</TableHead>
                      <TableHead>{Texts.workload}</TableHead>
                      <TableHead>{Texts.timesheetStatus}</TableHead>
                      <TableHead>{Texts.actions}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {monthGroup.items.map((item) => (
                      <TimesheetItemRow key={item.id} item={item} />
                    ))}
                  </TableBody>
                </Table>
              </div>
              );
            })
          )}
        </div>
        );
      })}
    </div>
  );
};
