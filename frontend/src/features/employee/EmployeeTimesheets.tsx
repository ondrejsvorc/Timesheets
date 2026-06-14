import { type Draft, produce } from "immer";
import { ChevronDown, Upload } from "lucide-react";
import { useMemo, useState } from "react";
import { useAsyncValue, useLoaderData, useLocation, useNavigate, useRevalidator } from "react-router";
import { Can } from "@/auth/Can";
import { UiAction } from "@/auth/uiPermissions";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { FilterBar, useFilterContext } from "@/components/shared/layout/FilterBar";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { CZECH_MONTH_NAMES } from "@/features/contract/utils/czechMonths";
import { type EmployeeTimesheetsFilterCriteria, filterToSearchParams, type GetEmployeeTimesheetsResponse } from "./api/getEmployeeTimesheets";
import { UploadTimesheetsDialog } from "./UploadTimesheetsDialog";

export type EmployeeTimesheetsLoaderData = {
  filter: EmployeeTimesheetsFilterCriteria;
  promise: Promise<GetEmployeeTimesheetsResponse>;
};

export const EmployeeTimesheets = () => {
  const { filter, promise } = useLoaderData() as EmployeeTimesheetsLoaderData;

  return (
    <AwaitContent promise={promise}>
      <EmployeeTimesheetsContent filter={filter} />
    </AwaitContent>
  );
};

interface EmployeeTimesheetsContentProps {
  filter: EmployeeTimesheetsFilterCriteria;
}

const EmployeeTimesheetsContent = ({ filter }: EmployeeTimesheetsContentProps) => {
  const response = useAsyncValue() as GetEmployeeTimesheetsResponse;
  const navigate = useNavigate();
  const location = useLocation();
  const revalidator = useRevalidator();
  const [isUploadDialogOpen, setIsUploadDialogOpen] = useState(false);
  const [onlyUnapproved, setOnlyUnapproved] = useState(false);
  const availableYears = response.availableYears;
  const availableMonths = useMemo(() => getAvailableMonthsForYear(response.availableMonths, filter.year), [response.availableMonths, filter.year]);

  const setFilter = (updater: (draft: Draft<EmployeeTimesheetsFilterCriteria>) => void) => {
    const next = produce(filter, updater);
    navigate({
      pathname: location.pathname,
      search: filterToSearchParams(next).toString(),
    });
  };

  const handleUploadClick = () => {
    setIsUploadDialogOpen(true);
  };

  let filteredMonths = response.months.filter((m) => m.year === filter.year);
  const selectedMonths = filter.months;
  if (selectedMonths !== null && selectedMonths.length > 0) filteredMonths = filteredMonths.filter((m) => selectedMonths.includes(m.month));
  if (onlyUnapproved) {
    filteredMonths = filteredMonths.filter((m) => m.status !== Texts.statusApproved);
  }

  if (filteredMonths.length === 0) {
    return (
      <>
        <FilterBar
          filter={filter}
          setFilter={setFilter}
          actions={
            <Can action={UiAction.timesheet.import} context={{ employeeId: response.employeeId }}>
              <Button variant="outline" onClick={handleUploadClick}>
                <Upload className="mr-2 h-4 w-4" />
                {Texts.importFiles}
              </Button>
            </Can>
          }
        >
          <EmployeeTimesheetsFilterControls availableYears={availableYears} availableMonths={availableMonths} onlyUnapproved={onlyUnapproved} onOnlyUnapprovedChange={setOnlyUnapproved} />
        </FilterBar>
        <EmptyState />
        {isUploadDialogOpen && <UploadTimesheetsDialog open={isUploadDialogOpen} onClose={() => setIsUploadDialogOpen(false)} onSuccess={() => revalidator.revalidate()} />}
      </>
    );
  }

  return (
    <>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={
          <Can action={UiAction.timesheet.import} context={{ employeeId: response.employeeId }}>
            <Button variant="outline" onClick={handleUploadClick}>
              <Upload className="mr-2 h-4 w-4" />
              {Texts.importFiles}
            </Button>
          </Can>
        }
      >
        <EmployeeTimesheetsFilterControls availableYears={availableYears} availableMonths={availableMonths} onlyUnapproved={onlyUnapproved} onOnlyUnapprovedChange={setOnlyUnapproved} />
      </FilterBar>
      <div className="rounded-md border p-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{Texts.month}</TableHead>
              <TableHead>{Texts.timesheetStatus}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredMonths.map((m) => {
              const disabled = !m.hasAttendanceImport;
              return (
                <TooltipProvider key={`${m.year}-${m.month}`} delayDuration={150}>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <TableRow
                        className={disabled ? "opacity-50" : "cursor-pointer hover:bg-muted/50"}
                        onClick={() => {
                          if (!disabled) navigate(Routes.timesheet(response.employeeId, m.year, m.month));
                        }}
                      >
                        <TableCell className="font-medium">
                          {CZECH_MONTH_NAMES[m.month]} {m.year}
                        </TableCell>
                        <TableCell>
                          <TimesheetStatusBadge status={m.status} />
                        </TableCell>
                      </TableRow>
                    </TooltipTrigger>
                    {disabled && <TooltipContent side="top">{Texts.importAttendanceFirstForMonth}</TooltipContent>}
                  </Tooltip>
                </TooltipProvider>
              );
            })}
          </TableBody>
        </Table>
      </div>
      {isUploadDialogOpen && <UploadTimesheetsDialog open={isUploadDialogOpen} onClose={() => setIsUploadDialogOpen(false)} onSuccess={() => revalidator.revalidate()} />}
    </>
  );
};

interface EmployeeTimesheetsFilterControlsProps {
  availableYears: number[];
  availableMonths: number[];
  onlyUnapproved: boolean;
  onOnlyUnapprovedChange: (value: boolean) => void;
}

function EmployeeTimesheetsFilterControls({ availableYears, availableMonths, onlyUnapproved, onOnlyUnapprovedChange }: EmployeeTimesheetsFilterControlsProps) {
  const { filter, setFilter } = useFilterContext<EmployeeTimesheetsFilterCriteria>();
  const [monthPopoverOpen, setMonthPopoverOpen] = useState(false);
  const selectedYear = availableYears.includes(filter.year) ? String(filter.year) : undefined;

  const handleMonthToggle = (month: number | null) => {
    setFilter((draft) => {
      if (month === null) {
        draft.months = null;
      } else if (draft.months === null) {
        draft.months = [month];
      } else if (draft.months.includes(month)) {
        draft.months = draft.months.filter((m) => m !== month);
        if (draft.months.length === 0) {
          draft.months = null;
        }
      } else {
        draft.months = [...draft.months, month];
      }
    });
  };

  const isAllMonthsSelected = filter.months === null;
  const selectedMonthsCount = filter.months?.length ?? 0;
  const singleSelectedMonth = filter.months?.[0];

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.year}</Label>
        <Select
          value={selectedYear}
          disabled={availableYears.length === 0}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.year = Number.parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[100px]">
            <SelectValue placeholder={Texts.noItems} />
          </SelectTrigger>
          <SelectContent>
            {availableYears.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.month}</Label>
        <Popover open={monthPopoverOpen} onOpenChange={setMonthPopoverOpen}>
          <PopoverTrigger asChild>
            <Button variant="outline" className="w-[180px] justify-between" disabled={availableMonths.length === 0}>
              <span className="truncate">
                {availableMonths.length === 0
                  ? Texts.noItems
                  : isAllMonthsSelected
                    ? Texts.allMonths
                    : selectedMonthsCount === 0
                      ? Texts.selectMonths
                      : selectedMonthsCount === 1 && singleSelectedMonth !== undefined
                        ? CZECH_MONTH_NAMES[singleSelectedMonth]
                        : Texts.monthsCount.replace("{count}", String(selectedMonthsCount))}
              </span>
              <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-[200px] p-2" align="start">
            <div className="space-y-1">
              <button type="button" className="flex w-full items-center space-x-2 rounded-sm px-2 py-1.5 text-left hover:bg-accent" onClick={() => handleMonthToggle(null)}>
                <Checkbox checked={isAllMonthsSelected} className="pointer-events-none" />
                <span className="text-sm font-normal">{Texts.allMonths}</span>
              </button>
              <div className="border-t my-1" />
              {availableMonths.map((month) => {
                const isSelected = filter.months?.includes(month) ?? false;
                return (
                  <button type="button" key={month} className="flex w-full items-center space-x-2 rounded-sm px-2 py-1.5 text-left hover:bg-accent" onClick={() => handleMonthToggle(month)}>
                    <Checkbox checked={isSelected} className="pointer-events-none" />
                    <span className="text-sm font-normal">{CZECH_MONTH_NAMES[month]}</span>
                  </button>
                );
              })}
            </div>
          </PopoverContent>
        </Popover>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-sm text-transparent select-none" aria-hidden="true">
          .
        </Label>
        <div className="flex h-9 items-center gap-3">
          <Checkbox id="only-unapproved" checked={onlyUnapproved} onCheckedChange={(checked) => onOnlyUnapprovedChange(checked === true)} />
          <Label htmlFor="only-unapproved" className="cursor-pointer text-sm leading-none">
            {Texts.onlyMonthsWithUnapprovedTimesheets}
          </Label>
        </div>
      </div>
    </>
  );
}

function getAvailableMonthsForYear(availableMonths: number[], _year: number) {
  return availableMonths;
}
