import { createBrowserRouter, type Params, redirect } from "react-router";
import { App } from "./App";
import { ErrorPage } from "./components/shared/errors/ErrorPage";
import { BaseUrl } from "./constants/api";
import { Routes } from "./constants/routes";
import { getContractEmployees } from "./features/contract/api/getContractEmployees";
import { getProjectContract } from "./features/contract/api/getProjectContract";
import { ContractEmployees } from "./features/contract/ContractEmployees";
import { ContractPage } from "./features/contract/ContractPage";
import { ContractTimesheets } from "./features/contract/ContractTimesheets";
import { getEmployee } from "./features/employee/api/getEmployee";
import { getEmployeePositions } from "./features/employee/api/getEmployeePositions";
import { getEmployeeTimesheets } from "./features/employee/api/getEmployeeTimesheets";
import { EmployeePage } from "./features/employee/EmployeePage";
import { EmployeePositions } from "./features/employee/EmployeePositions";
import { EmployeeTimesheets } from "./features/employee/EmployeeTimesheets";
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
import { getCombinedTimesheet } from "./features/timesheet/timesheet/api/getCombinedTimesheet";
import { getCombinedTimesheetOverview } from "./features/timesheet/timesheet/api/getCombinedTimesheetOverview";
import { TimesheetPage } from "./features/timesheet/timesheet/TimesheetPage";

export type CurrentUser = {
  id: string;
  fullName: string;
  email: string;
  employeeType: string | null;
  personalNumber: string;
  titleBefore: string | null;
  titleAfter: string | null;
};

const requireAuth = async ({ request }: { request: Request }) => {
  const returnTo = new URL(request.url).pathname + new URL(request.url).search;

  try {
    const response = await fetch(`${BaseUrl}/auth/currentUser`, { credentials: "include" });
    if (response.ok) {
      const currentUser = (await response.json()) as CurrentUser;
      return { currentUser };
    }

    // Only redirect to OIDC login when we're actually unauthenticated/forbidden.
    // Other statuses (e.g. 404 "Employee not found") should not cause a login loop.
    if (response.status !== 401 && response.status !== 403) {
      return { currentUser: null };
    }
  } catch {
    // ignore and fall back to login redirect
  }

  // Must be a full page navigation so `/auth/login` is handled by the backend (OIDC challenge).
  window.location.assign(`${BaseUrl}/auth/login?returnUrl=${encodeURIComponent(returnTo)}`);
  return { currentUser: null };
};

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
    id: "root",
    path: "/",
    element: <App />,
    loader: requireAuth,
    children: [
      {
        index: true,
        loader: () => redirect("/projects"),
      },
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
          {
            path: "timesheets",
            element: <EmployeeTimesheets />,
            loader: ({ params }) => getEmployeeTimesheets(requireEmployeeId(params)),
          },
        ],
      },
      {
        path: "timesheet",
        element: <TimesheetPage />,
        loader: ({ request }) => {
          const url = new URL(request.url);
          const employeeId = url.searchParams.get("employeeId");
          const year = Number(url.searchParams.get("year"));
          const month = Number(url.searchParams.get("month"));

          if (!employeeId || !Number.isInteger(year) || !Number.isInteger(month) || month < 1 || month > 12) {
            throw redirect(Routes.employees());
          }

          return {
            employeePromise: getEmployee(employeeId).promise,
            overviewPromise: getCombinedTimesheetOverview(employeeId, year, month).promise,
            timesheetPromise: getCombinedTimesheet(employeeId, year, month).promise,
          };
        },
      },
    ],
  },
]);
