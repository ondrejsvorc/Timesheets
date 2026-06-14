import { lazy } from "react";
import { Navigate, useAsyncValue, useLoaderData, useRevalidator } from "react-router";
import { useImmer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import type { GetEmployeesResponse } from "./api/getEmployees";
import { EmployeesTable } from "./EmployeesTable";
import { type EmployeesFilterCriteria, useEmployeesFilter } from "./hooks/useEmployeesFilters";

const EmployeesPageContentLazy = lazy(async () => ({
  default: EmployeesPageContent,
}));

export const EmployeesPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetEmployeesResponse>;
  };
  const canListEmployees = useCan(UiAction.employees.list);

  if (!canListEmployees) {
    return <Navigate to={Routes.projects()} replace />;
  }

  return (
    <>
      <PageHeader>
        <PageTitle>{Texts.employees}</PageTitle>
      </PageHeader>
      <AwaitContent promise={promise}>
        <EmployeesPageContentLazy />
      </AwaitContent>
    </>
  );
};

const { FilterSearchInput } = createFilterControls<EmployeesFilterCriteria>();

const EmployeesPageContent = () => {
  const response = useAsyncValue() as GetEmployeesResponse;
  const revalidator = useRevalidator();
  const [employees, setEmployees] = useImmer(response.employees);
  const { filter, setFilter, filtered } = useEmployeesFilter(employees);

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <EmployeesTable
        employees={filtered}
        onEmployeeTypeSaved={(employeeId, employeeTypeId) => {
          setEmployees((draft) => {
            const employee = draft.find((e) => e.id === employeeId);
            if (employee) {
              employee.employeeTypeId = employeeTypeId;
            }
          });
          revalidator.revalidate();
        }}
      />
    </>
  );
};
