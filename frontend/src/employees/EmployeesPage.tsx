import { Texts } from "../common/Texts";
import { AddEmployeeButton } from "./AddEmployeeButton";
import type { GetEmployeesResponse } from "./api/getEmployees";
import { EmployeesFilter } from "./EmployeesFilter";
import { EmployeesTable } from "./EmployeesTable";
import { useLoaderData } from "react-router";

export const EmployeesPage = () => {
  const { employees } = useLoaderData() as GetEmployeesResponse;

  return (
    <div className="w-full">
      <h1 className="text-2xl font-semibold mb-6 select-none">
        {Texts.employees}
      </h1>
      <div className="flex items-center justify-between mb-6">
        <EmployeesFilter />
        <AddEmployeeButton />
      </div>
      <EmployeesTable employees={employees} />
    </div>
  );
};

