import { lazy, Suspense, useState } from "react";
import { Await, Navigate, useAsyncValue, useLoaderData, useRevalidator } from "react-router";
import { toast } from "sonner";
import { useImmer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import type { EmployeeItem, GetEmployeesResponse } from "@/features/employees/api/getEmployees";
import { type EmployeesFilterCriteria, useEmployeesFilter } from "@/features/employees/hooks/useEmployeesFilters";
import { createFilterControls } from "@/utils/createFilterControls";
import { updateEmployeeGlobalManager } from "./api/updateEmployeeGlobalManager";

const EmployeeRolesPageContentLazy = lazy(async () => ({
  default: EmployeeRolesPageContent,
}));

export const EmployeeRolesPage = () => {
  const canManageRoles = useCan(UiAction.nav.employeeRoles);
  const { promise } = useLoaderData() as { promise: Promise<GetEmployeesResponse> };

  if (!canManageRoles) {
    return <Navigate to={Routes.projects()} replace />;
  }

  return (
    <>
      <PageHeader>
        <PageTitle>{Texts.employeeRoles}</PageTitle>
        <PageSubtitle>{Texts.employeeRolesDescription}</PageSubtitle>
      </PageHeader>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <EmployeeRolesPageContentLazy />
        </Await>
      </Suspense>
    </>
  );
};

const { FilterSearchInput } = createFilterControls<EmployeesFilterCriteria>();

const EmployeeRolesPageContent = () => {
  const response = useAsyncValue() as GetEmployeesResponse;
  const revalidator = useRevalidator();
  const [employees, setEmployees] = useImmer(response.employees);
  const { filter, setFilter, filtered } = useEmployeesFilter(employees);
  const [pendingEmployeeId, setPendingEmployeeId] = useState<string | null>(null);

  const handleToggle = async (employee: EmployeeItem, checked: boolean) => {
    setPendingEmployeeId(employee.id);
    const controller = new AbortController();

    try {
      await updateEmployeeGlobalManager(employee.id, { isGlobalManager: checked }, controller.signal);
      setEmployees((draft) => {
        const item = draft.find((entry) => entry.id === employee.id);
        if (item) {
          item.isGlobalManager = checked;
        }
      });
      revalidator.revalidate();
      toast.success(Texts.actionSuccessful);
    } catch {
      toast.error(Texts.actionFailed);
    } finally {
      setPendingEmployeeId(null);
    }
  };

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter}>
        <FilterSearchInput placeholder={Texts.searchByNameEmailOrNumber} />
      </FilterBar>
      {filtered.length === 0 ? (
        <EmptyState />
      ) : (
        <div className="rounded-md border p-4">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{Texts.personalNumber}</TableHead>
                <TableHead>{Texts.fullName}</TableHead>
                <TableHead>{Texts.email}</TableHead>
                <TableHead>{Texts.globalManager}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((employee) => {
                const checkboxId = `global-manager-${employee.id}`;
                const isPending = pendingEmployeeId === employee.id;

                return (
                  <TableRow key={employee.id}>
                    <TableCell>{employee.personalNumber ?? Texts.dash}</TableCell>
                    <TableCell>{employee.fullName}</TableCell>
                    <TableCell>{employee.email ?? Texts.dash}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Checkbox
                          id={checkboxId}
                          checked={employee.isGlobalManager}
                          disabled={isPending}
                          onCheckedChange={(checked) => {
                            void handleToggle(employee, checked === true);
                          }}
                        />
                        <Label htmlFor={checkboxId} className="sr-only">
                          {Texts.globalManager}
                        </Label>
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}
    </>
  );
};
