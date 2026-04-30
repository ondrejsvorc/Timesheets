import { Suspense } from "react";
import { Await, Outlet, useAsyncValue, useLoaderData } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { Routes } from "@/constants/routes";
import { useBackFromLocationState } from "@/hooks/useBackFromLocationState";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import type { GetEmployeeResponse } from "./api/getEmployee";
import { EmployeeTabs } from "./EmployeeTabs";

export const EmployeePage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetEmployeeResponse>;
  };

  return (
    <>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <EmployeePageHeader />
        </Await>
      </Suspense>
      <Suspense fallback={<GenericSkeleton />}>
        <Outlet />
      </Suspense>
    </>
  );
};

const EmployeePageHeader = () => {
  const response = useAsyncValue() as GetEmployeeResponse;
  const employee = response.employee;
  const handleBack = useBackFromLocationState(Routes.employees());

  return (
    <>
      <PageHeader leading={<BackButton onClick={handleBack} />}>
        <PageTitle>{employee.fullName}</PageTitle>
        <PageSubtitle>
          {employee.personalNumber} · {employee.email} · {resolveEmployeeTypeName(employee.employeeTypeId)}
        </PageSubtitle>
      </PageHeader>
      <EmployeeTabs />
    </>
  );
};
