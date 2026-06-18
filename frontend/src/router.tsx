import { createBrowserRouter, type Params, redirect } from "react-router";
import { App } from "./App";
import type { CurrentUserPermissions } from "./auth/api/getCurrentUserPermissions";
import { denyUnless, loadAuthContext, resolveHomePath } from "./auth/routeGuards";
import { can, UiAction } from "./auth/uiPermissions";
import { ErrorPage } from "./components/shared/errors/ErrorPage";
import { LoadingScreen } from "./components/shared/layout/LoadingScreen";
import { goToLogin } from "./constants/api";
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
import { getProject, getProjectContracts, getProjectContractsManagers } from "./features/project/api";
import { ProjectContracts } from "./features/project/ProjectContracts";
import { ProjectContractsManagers } from "./features/project/ProjectContractsManagers";
import { ProjectPage } from "./features/project/ProjectPage";
import { getProjects } from "./features/projects/api";
import { ProjectsPage } from "./features/projects/ProjectsPage";
import { getCombinedTimesheet } from "./features/timesheet/timesheet/api/getCombinedTimesheet";
import { getCombinedTimesheetOverview } from "./features/timesheet/timesheet/api/getCombinedTimesheetOverview";
import { getTimesheetComments } from "./features/timesheet/timesheet/api/getTimesheetComments";
import { TimesheetPage, type TimesheetPageData } from "./features/timesheet/timesheet/TimesheetPage";

export type CurrentUser = {
  id: string;
  fullName: string;
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
  const url = new URL(request.url);
  const returnTo = url.pathname + url.search;

  let auth: Awaited<ReturnType<typeof loadAuthContext>>;
  try {
    auth = await loadAuthContext();
  } catch {
    throw redirect(`/redirecting?returnTo=${encodeURIComponent(returnTo)}`);
  }

  if (!auth.currentUser) {
    throw redirect(`/redirecting?returnTo=${encodeURIComponent(returnTo)}`);
  }

  if (url.pathname === "/" || url.pathname === "") {
    throw redirect(resolveHomePath(auth));
  }

  return auth;
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
    hydrateFallbackElement: <LoadingScreen message={Texts.redirectingToLogin} />,
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
              const auth = await loadAuthContext();
              if (!can(auth.permissions, auth.currentUser?.id, UiAction.timesheet.listContract, { contractId, projectId })) {
                if (can(auth.permissions, auth.currentUser?.id, UiAction.contractEmployees.view, { contractId })) {
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
