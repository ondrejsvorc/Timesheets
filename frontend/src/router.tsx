import { createBrowserRouter, type Params, redirect } from "react-router";
import { App } from "./App";
import type { CurrentUser } from "./auth/api";
import { denyUnless, loadCurrentUser, resolveHomePath } from "./auth/routeGuards";
import { can, UiAction } from "./auth/uiPermissions";
import { ErrorPage } from "./components/shared/errors/ErrorPage";
import { LoadingScreen } from "./components/shared/layout/LoadingScreen";
import { goToLogin } from "./constants/api";
import { Routes } from "./constants/routes";
import { Texts } from "./constants/texts";
import { EmployeeRolesPage } from "./features/admin/EmployeeRolesPage";
import { getContractEmployees, getProjectContract, loadContractTimesheetsPage } from "./features/contract/api";
import { ContractEmployees } from "./features/contract/ContractEmployees";
import { ContractPage } from "./features/contract/ContractPage";
import { ContractTimesheets } from "./features/contract/ContractTimesheets";
import { getEmployee, getEmployeePositions, loadEmployeeTimesheetsPage } from "./features/employee/api";
import { EmployeePage } from "./features/employee/EmployeePage";
import { EmployeePositions } from "./features/employee/EmployeePositions";
import { EmployeeTimesheets } from "./features/employee/EmployeeTimesheets";
import { getEmployees } from "./features/employees/api";
import { EmployeesPage } from "./features/employees/EmployeesPage";
import { getProject, getProjectContracts, getProjectContractsManagers, getProjectManagers } from "./features/project/api";
import { ProjectContracts } from "./features/project/ProjectContracts";
import { ProjectContractsManagers } from "./features/project/ProjectContractsManagers";
import { ProjectManagers } from "./features/project/ProjectManagers";
import { ProjectPage } from "./features/project/ProjectPage";
import { getProjects } from "./features/projects/api";
import { ProjectsPage } from "./features/projects/ProjectsPage";
import { getCombinedTimesheet, getCombinedTimesheetOverview, getTimesheetComments } from "./features/timesheet/timesheet/api";
import { TimesheetPage, type TimesheetPageData } from "./features/timesheet/timesheet/TimesheetPage";

const requireAuth = async ({ request }: { request: Request }): Promise<CurrentUser> => {
  const url = new URL(request.url);
  const returnTo = url.pathname + url.search;

  let user: CurrentUser | null;
  try {
    user = await loadCurrentUser();
  } catch {
    throw redirect(`/redirecting?returnTo=${encodeURIComponent(returnTo)}`);
  }

  if (!user) {
    throw redirect(`/redirecting?returnTo=${encodeURIComponent(returnTo)}`);
  }

  if (url.pathname === "/" || url.pathname === "") {
    throw redirect(resolveHomePath(user));
  }

  return user;
};

const redirectToLogin = ({ request }: { request: Request }) => {
  const url = new URL(request.url);
  const returnToRaw = url.searchParams.get("returnTo") ?? "/";
  const returnTo = returnToRaw.startsWith("/redirecting") ? "/" : returnToRaw;
  goToLogin(returnTo);
  return null;
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
    path: "/redirecting",
    element: <LoadingScreen message={Texts.redirectingToLogin} />,
    loader: redirectToLogin,
  },
  {
    id: "root",
    path: "/",
    element: <App />,
    hydrateFallbackElement: <LoadingScreen />,
    loader: requireAuth,
    children: [
      {
        path: "projects",
        element: <ProjectsPage />,
        loader: async ({ request }) => {
          await denyUnless(UiAction.nav.projects, {}, request);
          return { promise: getProjects() };
        },
      },
      {
        path: "projects/:id",
        element: <ProjectPage />,
        loader: async ({ params, request }) => {
          const projectId = requireProjectId(params);
          await denyUnless(UiAction.projects.view, { projectId }, request);
          return { promise: getProject(projectId) };
        },
        children: [
          {
            index: true,
            element: <ProjectContracts />,
            loader: async ({ params, request }) => {
              const projectId = requireProjectId(params);
              await denyUnless(UiAction.projects.view, { projectId }, request);
              return { promise: getProjectContracts(projectId) };
            },
          },
          {
            path: "project-managers",
            element: <ProjectManagers />,
            loader: async ({ params, request }) => {
              const projectId = requireProjectId(params);
              await denyUnless(UiAction.projectManagers.view, { projectId }, request);
              return { promise: getProjectManagers(projectId) };
            },
          },
          {
            path: "contracts-managers",
            element: <ProjectContractsManagers />,
            loader: async ({ params, request }) => {
              const projectId = requireProjectId(params);
              await denyUnless(UiAction.contractManagers.view, { projectId }, request);
              return { promise: getProjectContractsManagers(projectId) };
            },
          },
        ],
      },
      {
        path: "projects/:id/contracts/:contractId",
        element: <ContractPage />,
        loader: async ({ params, request }) => {
          const { projectId, contractId } = requireContractParams(params);
          await denyUnless(UiAction.contracts.view, { projectId, contractId }, request);
          const promise = getProjectContract(projectId, contractId).then((contract) => {
            if (!contract) throw redirect(Routes.project(projectId));
            return contract;
          });
          return { promise };
        },
        children: [
          {
            index: true,
            element: <ContractTimesheets />,
            loader: async ({ params, request }) => {
              const { projectId, contractId } = requireContractParams(params);
              const user = await loadCurrentUser();
              if (!user || !can(user.permissions, user.id, UiAction.timesheet.listContract, { contractId, projectId })) {
                if (user && can(user.permissions, user.id, UiAction.contractEmployees.view, { contractId })) {
                  throw redirect(Routes.contractEmployees(projectId, contractId));
                }
                await denyUnless(UiAction.timesheet.listContract, { contractId, projectId }, request);
              }
              return loadContractTimesheetsPage(projectId, contractId, request);
            },
          },
          {
            path: "employees",
            element: <ContractEmployees />,
            loader: async ({ params, request }) => {
              const { projectId, contractId } = requireContractParams(params);
              await denyUnless(UiAction.contractEmployees.view, { contractId, projectId }, request);
              return { promise: getContractEmployees(projectId, contractId) };
            },
          },
        ],
      },
      {
        path: "employees",
        element: <EmployeesPage />,
        loader: async ({ request }) => {
          await denyUnless(UiAction.employees.list, {}, request);
          return { promise: getEmployees() };
        },
      },
      {
        path: "employees/roles",
        element: <EmployeeRolesPage />,
        loader: async ({ request }) => {
          await denyUnless(UiAction.nav.employeeRoles, {}, request);
          return { promise: getEmployees() };
        },
      },
      {
        path: "employees/:id",
        element: <EmployeePage />,
        loader: async ({ params, request }) => {
          const employeeId = requireEmployeeId(params);
          await denyUnless(UiAction.employees.view, { employeeId }, request);
          return { promise: getEmployee(employeeId) };
        },
        children: [
          {
            index: true,
            element: <EmployeeTimesheets />,
            loader: async ({ params, request }) => {
              const employeeId = requireEmployeeId(params);
              await denyUnless(UiAction.employees.view, { employeeId }, request);
              return loadEmployeeTimesheetsPage(employeeId, request);
            },
          },
          {
            path: "positions",
            element: <EmployeePositions />,
            loader: async ({ params, request }) => {
              const employeeId = requireEmployeeId(params);
              await denyUnless(UiAction.employees.view, { employeeId }, request);
              return { promise: getEmployeePositions(employeeId) };
            },
          },
        ],
      },
      {
        path: "employees/:id/timesheets",
        loader: ({ params }) => redirect(Routes.employee(requireEmployeeId(params))),
      },
      {
        path: "timesheet",
        element: <TimesheetPage />,
        loader: async ({ request }) => {
          const url = new URL(request.url);
          const employeeId = url.searchParams.get("employeeId");
          const year = Number(url.searchParams.get("year"));
          const month = Number(url.searchParams.get("month"));

          if (!employeeId || !Number.isInteger(year) || !Number.isInteger(month) || month < 1 || month > 12) {
            throw redirect(Routes.employees());
          }

          await denyUnless(UiAction.timesheet.view, { employeeId }, request);

          return {
            promise: Promise.all([
              getEmployee(employeeId),
              getCombinedTimesheetOverview(employeeId, year, month),
              getCombinedTimesheet(employeeId, year, month),
              getTimesheetComments(employeeId, year, month),
            ]).then(([employee, overview, timesheetData, comments]) => ({ employee, overview, timesheetData, comments }) satisfies TimesheetPageData),
          };
        },
      },
    ],
  },
]);
