import { Link, useAsyncValue, useLoaderData, useLocation, useNavigate, useNavigation } from "react-router";
import { useImmer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "@/components/shared/inputs/MultiSelectComboBox";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { FilterBar, useFilterContext } from "@/components/shared/layout/FilterBar";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/common";
import { formatWorkloadPercent } from "@/utils/format";
import {
  buildEmployeesView,
  buildMonthsView,
  type ContractTimesheetsFilterCriteria,
  type EmployeeGroupView,
  filterToSearchParams,
  type GetContractTimesheetsFilterOptionsResponse,
  type GetContractTimesheetsResponse,
  type MonthGroupView,
  type TimesheetRowView,
} from "./api";
import { CZECH_MONTH_NAMES, formatMonthYear } from "./utils/czechMonths";

export type ContractTimesheetsLoaderData = {
  filter: ContractTimesheetsFilterCriteria;
  filterOptions: GetContractTimesheetsFilterOptionsResponse;
  promise: Promise<GetContractTimesheetsResponse>;
};

export const ContractTimesheets = () => {
  const { filter, filterOptions, promise } = useLoaderData() as ContractTimesheetsLoaderData;
  const location = useLocation();

  return (
    <AwaitContent promise={promise}>
      <ContractTimesheetsContent key={location.search} filter={filter} filterOptions={filterOptions} />
    </AwaitContent>
  );
};

interface ContractTimesheetsContentProps {
  filter: ContractTimesheetsFilterCriteria;
  filterOptions: GetContractTimesheetsFilterOptionsResponse;
}

const ContractTimesheetsContent = ({ filter, filterOptions }: ContractTimesheetsContentProps) => {
  const data = useAsyncValue() as GetContractTimesheetsResponse;
  const navigate = useNavigate();
  const location = useLocation();
  const navigation = useNavigation();
  const [draftFilter, setDraftFilter] = useImmer(filter);
  const isLoading = navigation.state !== "idle";

  const handleFilter = () => {
    navigate({
      pathname: location.pathname,
      search: filterToSearchParams(draftFilter).toString(),
    });
  };

  const monthsView = buildMonthsView(data);
  const employeesView = buildEmployeesView(data);

  return (
    <>
      <FilterBar filter={draftFilter} setFilter={setDraftFilter} actions={<Button onClick={handleFilter}>{Texts.filter}</Button>}>
        <ContractTimesheetsFilterControls options={filterOptions} />
      </FilterBar>
      {draftFilter.groupBy === "Month" ? <TimesheetsByMonth months={monthsView} isLoading={isLoading} /> : <TimesheetsByEmployee employees={employeesView} isLoading={isLoading} />}
    </>
  );
};

function ContractTimesheetsFilterControls({ options }: { options: GetContractTimesheetsFilterOptionsResponse }) {
  const { filter, setFilter } = useFilterContext<ContractTimesheetsFilterCriteria>();
  const yearOptions = options.years;
  const monthOptions = options.months;
  const statusOptions: MultiSelectComboBoxItem[] = options.statuses.map((s) => ({ value: s, label: s }));

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
            disabled={yearOptions.length === 0}
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
              {yearOptions.map((y) => (
                <SelectItem key={y} value={String(y)}>
                  {y}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={String(filter.fromMonth)}
            disabled={monthOptions.length === 0}
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
              {monthOptions.map((m) => (
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
            disabled={yearOptions.length === 0}
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
              {yearOptions.map((y) => (
                <SelectItem key={y} value={String(y)}>
                  {y}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={String(filter.toMonth)}
            disabled={monthOptions.length === 0}
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
              {monthOptions.map((m) => (
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
          items={statusOptions}
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

const overviewLinkClassName = "text-foreground hover:underline underline-offset-4";

interface EmployeeNameLinkProps {
  employeeId: string;
  children: string;
  className?: string;
}

const EmployeeNameLink = ({ employeeId, children, className }: EmployeeNameLinkProps) => {
  const canViewEmployee = useCan(UiAction.employees.view, { employeeId });

  if (!canViewEmployee) {
    return <span className={className}>{children}</span>;
  }

  return (
    <Link to={Routes.employee(employeeId)} className={cn(overviewLinkClassName, className)}>
      {children}
    </Link>
  );
};

interface TimesheetMonthLinkProps {
  employeeId: string;
  year: number;
  month: number;
  children: string;
  className?: string;
}

const TimesheetMonthLink = ({ employeeId, year, month, children, className }: TimesheetMonthLinkProps) => (
  <Link to={Routes.timesheet(employeeId, year, month)} className={cn(overviewLinkClassName, className)}>
    {children}
  </Link>
);

interface TimesheetsByMonthProps {
  months: MonthGroupView[];
  isLoading: boolean;
}

const TimesheetsByMonth = ({ months, isLoading }: TimesheetsByMonthProps) => {
  if (months.length === 0) {
    return isLoading ? <GenericSkeleton /> : <EmptyState />;
  }
  return (
    <div className="space-y-8">
      {months.map((monthGroup) => (
        <div key={`${monthGroup.year}-${monthGroup.month}`} className="space-y-6">
          <div className="mb-3 font-medium text-muted-foreground">{formatMonthYear(monthGroup.month, monthGroup.year)}</div>
          {monthGroup.items.map((employee) => (
            <div key={employee.id} className="rounded-md border p-4">
              <div className="mb-3 flex flex-wrap items-center justify-between gap-2 font-medium">
                <EmployeeNameLink employeeId={employee.id}>{employee.fullName}</EmployeeNameLink>
                <TimesheetMonthLink employeeId={employee.id} year={monthGroup.year} month={monthGroup.month} className="text-sm text-muted-foreground">
                  {Texts.viewTimesheet}
                </TimesheetMonthLink>
              </div>
              {employee.timesheets.length === 0 ? (
                <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
              ) : (
                <Table className="table-fixed w-full">
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-1/3">{Texts.position}</TableHead>
                      <TableHead className="w-1/3">{Texts.timesheetStatus}</TableHead>
                      <TableHead className="w-1/3">{Texts.workload}</TableHead>
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
  item: { id: string; position: string; workload: number; status: string };
}

function TimesheetItemRow({ item }: TimesheetItemRowProps) {
  return (
    <TableRow>
      <TableCell className="truncate" title={item.position}>
        {item.position}
      </TableCell>
      <TableCell>
        <TimesheetStatusBadge status={item.status} />
      </TableCell>
      <TableCell>{formatWorkloadPercent(item.workload)}</TableCell>
    </TableRow>
  );
}

/** Seskupí výkazy zaměstnance po měsících (year, month), seřazené chronologicky. */
function groupTimesheetsByMonth(timesheets: TimesheetRowView[]): { year: number; month: number; items: TimesheetRowView[] }[] {
  const withPeriod = timesheets.filter((t): t is TimesheetRowView & { year: number; month: number } => t.year != null && t.month != null);
  const byKey = new Map<string, TimesheetRowView[]>();
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
    .sort((a, b) => (a.year !== b.year ? a.year - b.year : a.month - b.month));
}

interface TimesheetsByEmployeeProps {
  employees: EmployeeGroupView[];
  isLoading: boolean;
}

const TimesheetsByEmployee = ({ employees, isLoading }: TimesheetsByEmployeeProps) => {
  if (employees.length === 0) {
    return isLoading ? <GenericSkeleton /> : <EmptyState />;
  }
  return (
    <div className="space-y-8">
      {employees.map((emp) => {
        const months = groupTimesheetsByMonth(emp.timesheets);
        return (
          <div key={emp.id} className="space-y-6">
            <div className="mb-3 font-medium">
              <EmployeeNameLink employeeId={emp.id}>{emp.fullName}</EmployeeNameLink>
            </div>
            {months.length === 0 ? (
              <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
            ) : (
              months.map((monthGroup) => {
                return (
                  <div key={`${monthGroup.year}-${monthGroup.month}`} className="rounded-md border p-4">
                    <div className="mb-3 font-medium">
                      <TimesheetMonthLink employeeId={emp.id} year={monthGroup.year} month={monthGroup.month}>
                        {formatMonthYear(monthGroup.month, monthGroup.year)}
                      </TimesheetMonthLink>
                    </div>
                    <Table className="table-fixed w-full">
                      <TableHeader>
                        <TableRow>
                          <TableHead className="w-1/3">{Texts.position}</TableHead>
                          <TableHead className="w-1/3">{Texts.timesheetStatus}</TableHead>
                          <TableHead className="w-1/3">{Texts.workload}</TableHead>
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
