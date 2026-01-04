import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { Routes } from "@/constants/routes";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import { Suspense } from "react";
import { Await, Outlet, useAsyncValue, useLoaderData, useNavigate } from "react-router";
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
  const navigate = useNavigate();
  const employee = response.employee;

  return (
    <>
      <PageHeader leading={<BackButton onClick={() => navigate(Routes.employees())} />}>
        <PageTitle>{employee.fullName}</PageTitle>
        <PageSubtitle>
          {employee.personalNumber} · {employee.email} · {resolveEmployeeTypeName(employee.employeeTypeId)}
        </PageSubtitle>
      </PageHeader>
      <EmployeeTabs />
    </>
  );
};
