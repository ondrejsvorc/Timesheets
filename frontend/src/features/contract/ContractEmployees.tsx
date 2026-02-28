import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { ActionDropdownMenu, EditAction } from "@/components/shared/menus/ActionDropdownMenu";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { format, isBefore, parseISO, startOfDay } from "date-fns";
import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import type { EmployeeItem, GetContractEmployeesResponse, PositionItem } from "./api/getContractEmployees";
import { type ContractEmployeesFilterCriteria, useContractEmployeesFilter } from "./hooks/useContractEmployeesFilter";

export const ContractEmployees = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetContractEmployeesResponse>;
  };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={promise}>
        <ContractEmployeesContent />
      </Await>
    </Suspense>
  );
};

const { FilterSearchInput } = createFilterControls<ContractEmployeesFilterCriteria>();

const ContractEmployeesContent = () => {
  const response = useAsyncValue() as GetContractEmployeesResponse;
  const { filter, setFilter, filtered } = useContractEmployeesFilter(response.employees);

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>{Texts.employees}</SubPageTitle>
      </SubPageHeader>
      <FilterBar filter={filter} setFilter={setFilter} actions={<AddButton onClick={() => {}}>{Texts.addEmployee}</AddButton>}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractEmployeesList employees={filtered} />
    </>
  );
};

interface ContractEmployeesListProps {
  employees: EmployeeItem[];
}

const ContractEmployeesList = ({ employees }: ContractEmployeesListProps) => {
  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="space-y-6">
      {employees.map((employee) => (
        <EmployeeSection key={employee.id} employee={employee} />
      ))}
    </div>
  );
};

interface EmployeeSectionProps {
  employee: EmployeeItem;
}

const EmployeeSection = ({ employee }: EmployeeSectionProps) => {
  return (
    <div className="rounded-md border p-4">
      <div className="mb-3 font-medium text-foreground">
        {employee.fullName} · {employee.personalNumber} · {employee.employeeType}
      </div>
      {employee.positions.length === 0 ? (
        <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{Texts.position}</TableHead>
              <TableHead>{Texts.from}</TableHead>
              <TableHead>{Texts.to}</TableHead>
              <TableHead>{Texts.status}</TableHead>
              <TableHead>{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {employee.positions.map((position, index) => (
              <PositionRow key={`${employee.id}-${index}`} position={position} />
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
};

interface PositionRowProps {
  position: PositionItem;
}

const PositionRow = ({ position }: PositionRowProps) => {
  const active = isPositionActive(position);

  return (
    <TableRow className="cursor-pointer">
      <TableCell>{position.position ?? Texts.dash}</TableCell>
      <TableCell>{formatDate(position.startDate)}</TableCell>
      <TableCell>{position.endDate ? formatDate(position.endDate) : Texts.dash}</TableCell>
      <TableCell>{active ? Texts.active : Texts.inactive}</TableCell>
      <TableCell>
        <ActionDropdownMenu>
          <EditAction onClick={() => {}} />
        </ActionDropdownMenu>
      </TableCell>
    </TableRow>
  );
};

/** Aktivní, pokud není endDate nebo endDate je dnes či v budoucnu. */
/** TODO: Posílat příznak z backendu, protože zde může být chyba kvůli časové zóně klienta. */
function isPositionActive(position: PositionItem): boolean {
  if (!position.endDate) return true;
  const today = startOfDay(new Date());
  const end = parseISO(position.endDate);
  return !isBefore(end, today);
}

function formatDate(iso: string): string {
  try {
    return format(parseISO(iso), "dd.MM.yyyy");
  } catch {
    return iso;
  }
}
