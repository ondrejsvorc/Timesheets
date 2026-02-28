import { createBrowserRouter, type Params, redirect } from "react-router";
import { App } from "./App";
import { TimesheetPage } from "./components/poc/TimesheetPage";
import { ErrorPage } from "./components/shared/errors/ErrorPage";
import { Routes } from "./constants/routes";
import { getContractEmployees } from "./features/contract/api/getContractEmployees";
import { getProjectContract } from "./features/contract/api/getProjectContract";
import { getContractTimesheets } from "./features/contract/api/getContractTimesheets";
import { ContractPage } from "./features/contract/ContractPage";
import { getEmployee } from "./features/employee/api/getEmployee";
import { getEmployeePositions } from "./features/employee/api/getEmployeePositions";
import { EmployeePage } from "./features/employee/EmployeePage";
import { EmployeePositions } from "./features/employee/EmployeePositions";
import { getEmployees } from "./features/employees/api/getEmployees";
import { EmployeesPage } from "./features/employees/EmployeesPage";
import { getProject } from "./features/project/api/getProject";
import { getProjectContracts } from "./features/project/api/getProjectContracts";
import { getProjectContractsManagers } from "./features/project/api/getProjectContractsManagers";
import { ProjectContracts } from "./features/project/ProjectContracts";
import { ProjectContractsManagers } from "./features/project/ProjectContractsManagers";
import { ProjectPage } from "./features/project/ProjectPage";
import { getProjects } from "./features/projects/api/getProjects";
import { ProjectsPage } from "./features/projects/ProjectsPage";
import { ContractTimesheets } from "./features/contract/ContractTimesheets";
import { ContractEmployees } from "./features/contract/ContractEmployees";

const requireProjectId = (params: Params) => {
  if (!params.id) {
    throw redirect("/projects");
  }
  return params.id;
};

const requireEmployeeId = (params: Params) => {
  if (!params.id) {
    throw redirect("/employees");
  }
  return params.id;
};

const requireContractParams = (params: Params) => {
  if (!params.id || !params.contractId) {
    throw redirect(params.id ? Routes.project(params.id) : "/projects");
  }
  return { projectId: params.id, contractId: params.contractId };
};

export const router = createBrowserRouter([
  {
    path: "*",
    element: <ErrorPage />,
  },
  {
    path: "/",
    element: <App />,
    children: [
      {
        path: "projects",
        element: <ProjectsPage />,
        loader: getProjects,
      },
      {
        path: "projects/:id",
        element: <ProjectPage />,
        loader: ({ params }) => getProject(requireProjectId(params)),
        children: [
          {
            index: true,
            element: <ProjectContracts />,
            loader: ({ params }) => getProjectContracts(requireProjectId(params)),
          },
          {
            path: "contracts-managers",
            element: <ProjectContractsManagers />,
            loader: ({ params }) => getProjectContractsManagers(requireProjectId(params)),
          },
        ],
      },
      {
        path: "projects/:id/contracts/:contractId",
        element: <ContractPage />,
        loader: ({ params }) => {
          const { projectId, contractId } = requireContractParams(params);
          const promise = getProjectContract(projectId, contractId).promise.then((contract) => {
            if (!contract) throw redirect(Routes.project(projectId));
            return contract;
          });
          return { promise };
        },
        children: [
          {
            index: true,
            element: <ContractTimesheets />,
            loader: ({ params }) => {
              const { projectId, contractId } = requireContractParams(params);
              return getContractTimesheets(projectId, contractId);
            },
          },
          {
            path: "employees",
            element: <ContractEmployees />,
            loader: ({ params }) => {
              const { projectId, contractId } = requireContractParams(params);
              return getContractEmployees(projectId, contractId);
            },
          },
        ],
      },
      {
        path: "employees",
        element: <EmployeesPage />,
        loader: getEmployees,
      },
      {
        path: "employees/:id",
        element: <EmployeePage />,
        loader: ({ params }) => getEmployee(requireEmployeeId(params)),
        children: [
          {
            index: true,
            element: <EmployeePositions />,
            loader: ({ params }) => getEmployeePositions(requireEmployeeId(params)),
          },
          // {
          //   path: "timesheets",
          //   element: <EmployeeTimesheets />,
          //   loader: ({ params }) => getEmployeeTimesheets(requireEmployeeId(params)),
          // },
        ],
      },
      {
        path: "timesheet",
        element: <TimesheetPage />,
      },
    ],
  },
]);
