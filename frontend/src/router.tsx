import { createBrowserRouter, type Params, redirect } from "react-router";
import { App } from "./App";
import { type CurrentUserPermissions, getCurrentUserPermissions } from "./auth/api/getCurrentUserPermissions";
import { denyUnless } from "./auth/routeGuards";
import { can, UiAction } from "./auth/uiPermissions";
import { ErrorPage } from "./components/shared/errors/ErrorPage";
import { FullscreenLoader } from "./components/shared/layout/FullscreenLoader";
import { BaseUrl } from "./constants/api";
import { Routes } from "./constants/routes";
import { Texts } from "./constants/texts";
import { EmployeeRolesPage } from "./features/admin/EmployeeRolesPage";
import { getContractEmployees } from "./features/contract/api/getContractEmployees";
import { loadContractTimesheetsPage } from "./features/contract/api/getContractTimesheets";
import { getProjectContract } from "./features/contract/api/getProjectContract";
import { ContractEmployees } from "./features/contract/ContractEmployees";
import { ContractPage } from "./features/contract/ContractPage";
import { ContractTimesheets } from "./features/contract/ContractTimesheets";
import { getEmployee } from "./features/employee/api/getEmployee";
import { getEmployeePositions } from "./features/employee/api/getEmployeePositions";
import { loadEmployeeTimesheetsPage } from "./features/employee/api/getEmployeeTimesheets";
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
import { getTimesheetComments } from "./features/timesheet/timesheet/api/getTimesheetComments";
import { TimesheetPage } from "./features/timesheet/timesheet/TimesheetPage";
import { resourceRoutes } from "./router/resourceRoutes";

export type CurrentUser = {
  id: string;
  fullName: string;
  email: string;
  employeeType: string | null;
  personalNumber: string;
  titleBefore: string | null;
  titleAfter: string | null;
};

export type RootLoaderData = {
  currentUser: CurrentUser | null;
  permissions: CurrentUserPermissions | null;
};

const requireAuth = async ({ request }: { request: Request }): Promise<RootLoaderData> => {
  const returnTo = new URL(request.url).pathname + new URL(request.url).search;

  try {
    const [userResponse, permissionsResult] = await Promise.all([
      fetch(`${BaseUrl}/auth/currentUser`, { credentials: "include" }),
      getCurrentUserPermissions().catch(() => null),
    ]);

    if (userResponse.ok) {
      const currentUser = (await userResponse.json()) as CurrentUser;
      return { currentUser, permissions: permissionsResult };
    }

    // Only redirect to OIDC login when we're actually unauthenticated/forbidden.
    // Other statuses (e.g. 404 "Employee not found") should not cause a login loop.
    if (userResponse.status !== 401 && userResponse.status !== 403) {
      return { currentUser: null, permissions: permissionsResult };
    }
  } catch {
    // ignore and fall back to login redirect
  }

  // Important: throw a redirect so React Router cancels other loaders for the current navigation.
  // The /redirecting route will then do the full-page navigation to backend OIDC login.
  throw redirect(`/redirecting?returnTo=${encodeURIComponent(returnTo)}`);
};

const redirectToLogin = ({ request }: { request: Request }) => {
  const url = new URL(request.url);
  const returnToRaw = url.searchParams.get("returnTo") ?? "/";
  const returnTo = returnToRaw.startsWith("/redirecting") ? "/" : returnToRaw;
  window.location.assign(`${BaseUrl}/auth/login?returnUrl=${encodeURIComponent(returnTo)}`);
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
    element: <FullscreenLoader ariaLabel={Texts.redirectingToLogin} />,
    loader: redirectToLogin,
  },
  {
    id: "root",
    path: "/",
    element: <App />,
    hydrateFallbackElement: <FullscreenLoader ariaLabel={Texts.redirectingToLogin} />,
    loader: requireAuth,
    children: [
      ...resourceRoutes,
      {
        index: true,
        loader: async () => {
          const permissions = await getCurrentUserPermissions().catch(() => null);
          if (!can(permissions, undefined, UiAction.nav.projects)) {
            const userResponse = await fetch(`${BaseUrl}/auth/currentUser`, { credentials: "include" });
            if (userResponse.ok) {
              const currentUser = (await userResponse.json()) as CurrentUser;
              throw redirect(Routes.employee(currentUser.id));
            }
          }
          throw redirect(Routes.projects());
        },
      },
      {
        path: "projects",
        element: <ProjectsPage />,
        loader: async () => {
          await denyUnless(UiAction.nav.projects);
          return getProjects();
        },
      },
      {
        path: "projects/:id",
        element: <ProjectPage />,
        loader: async ({ params }) => {
          const projectId = requireProjectId(params);
          await denyUnless(UiAction.projects.view, { projectId });
          return getProject(projectId);
        },
        children: [
          {
            index: true,
            element: <ProjectContracts />,
            loader: async ({ params }) => {
              const projectId = requireProjectId(params);
              await denyUnless(UiAction.projects.view, { projectId });
              return getProjectContracts(projectId);
            },
          },
          {
            path: "contracts-managers",
            element: <ProjectContractsManagers />,
            loader: async ({ params }) => {
              const projectId = requireProjectId(params);
              await denyUnless(UiAction.contractManagers.view, { projectId });
              return getProjectContractsManagers(projectId);
            },
          },
        ],
      },
      {
        path: "projects/:id/contracts/:contractId",
        element: <ContractPage />,
        loader: async ({ params }) => {
          const { projectId, contractId } = requireContractParams(params);
          await denyUnless(UiAction.contracts.view, { projectId, contractId });
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
            loader: async ({ params, request }) => {
              const { projectId, contractId } = requireContractParams(params);
              await denyUnless(UiAction.contracts.view, { projectId, contractId });
              return loadContractTimesheetsPage(projectId, contractId, request);
            },
          },
          {
            path: "employees",
            element: <ContractEmployees />,
            loader: async ({ params }) => {
              const { projectId, contractId } = requireContractParams(params);
              await denyUnless(UiAction.contractEmployees.view, { contractId, projectId });
              return getContractEmployees(projectId, contractId);
            },
          },
        ],
      },
      {
        path: "employees",
        element: <EmployeesPage />,
        loader: async () => {
          await denyUnless(UiAction.employees.list);
          return getEmployees();
        },
      },
      {
        path: "employees/roles",
        element: <EmployeeRolesPage />,
        loader: async () => {
          await denyUnless(UiAction.nav.employeeRoles);
          return getEmployees();
        },
      },
      {
        path: "employees/:id",
        element: <EmployeePage />,
        loader: async ({ params }) => {
          const employeeId = requireEmployeeId(params);
          await denyUnless(UiAction.employees.view, { employeeId });
          return getEmployee(employeeId);
        },
        children: [
          {
            index: true,
            element: <EmployeeTimesheets />,
            loader: async ({ params, request }) => {
              const employeeId = requireEmployeeId(params);
              await denyUnless(UiAction.employees.view, { employeeId });
              return loadEmployeeTimesheetsPage(employeeId, request);
            },
          },
          {
            path: "positions",
            element: <EmployeePositions />,
            loader: async ({ params }) => {
              const employeeId = requireEmployeeId(params);
              await denyUnless(UiAction.employees.view, { employeeId });
              return getEmployeePositions(employeeId);
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

          await denyUnless(UiAction.timesheet.view, { employeeId });

          return {
            employeePromise: getEmployee(employeeId).promise,
            overviewPromise: getCombinedTimesheetOverview(employeeId, year, month).promise,
            timesheetPromise: getCombinedTimesheet(employeeId, year, month).promise,
            commentsPromise: getTimesheetComments(employeeId, year, month).promise,
          };
        },
      },
    ],
  },
]);
