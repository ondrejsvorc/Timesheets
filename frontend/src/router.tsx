import { createBrowserRouter, type LoaderFunctionArgs, type Params, redirect } from "react-router";
import { App } from "./App";
import { denyUnless, ensureAuthenticated, loadCurrentUser, resolveHomePath } from "./auth/routeGuards";
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
import { getCombinedTimesheet, getCombinedTimesheetOverview, getTimesheetComments } from "./features/timesheet/api";
import { TimesheetPage, type TimesheetPageData } from "./features/timesheet/TimesheetPage";

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

const redirectToLogin = ({ request }: LoaderFunctionArgs) => {
  const url = new URL(request.url);
  const returnToRaw = url.searchParams.get("returnTo") ?? "/";
  const returnTo = returnToRaw.startsWith("/redirecting") ? "/" : returnToRaw;
  goToLogin(returnTo);
  return null;
};

const rootLoader = async ({ request }: LoaderFunctionArgs) => {
  const user = await ensureAuthenticated(request);
  const { pathname } = new URL(request.url);
  if (pathname === "/" || pathname === "") {
    throw redirect(resolveHomePath(user));
  }
  return user;
};

const projectsLoader = async ({ request }: LoaderFunctionArgs) => {
  await denyUnless(UiAction.nav.projects, {}, request);
  return { promise: getProjects() };
};

const projectLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const projectId = requireProjectId(params);
  await denyUnless(UiAction.projects.view, { projectId }, request);
  return { promise: getProject(projectId) };
};

const projectContractsLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const projectId = requireProjectId(params);
  await denyUnless(UiAction.projects.view, { projectId }, request);
  return { promise: getProjectContracts(projectId) };
};

const projectManagersLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const projectId = requireProjectId(params);
  await denyUnless(UiAction.projectManagers.view, { projectId }, request);
  return { promise: getProjectManagers(projectId) };
};

const projectContractsManagersLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const projectId = requireProjectId(params);
  await denyUnless(UiAction.contractManagers.view, { projectId }, request);
  return { promise: getProjectContractsManagers(projectId) };
};

const contractPageLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const { projectId, contractId } = requireContractParams(params);
  await denyUnless(UiAction.contracts.view, { projectId, contractId }, request);
  const promise = getProjectContract(projectId, contractId).then((contract) => {
    if (!contract) throw redirect(Routes.project(projectId));
    return contract;
  });
  return { promise };
};

const contractTimesheetsLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const { projectId, contractId } = requireContractParams(params);
  const user = await loadCurrentUser();
  if (!user || !can(user.permissions, user.id, UiAction.timesheet.listContract, { contractId, projectId })) {
    if (user && can(user.permissions, user.id, UiAction.contractEmployees.view, { contractId })) {
      throw redirect(Routes.contractEmployees(projectId, contractId));
    }
    await denyUnless(UiAction.timesheet.listContract, { contractId, projectId }, request);
  }
  return loadContractTimesheetsPage(projectId, contractId, request);
};

const contractEmployeesLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const { projectId, contractId } = requireContractParams(params);
  await denyUnless(UiAction.contractEmployees.view, { contractId, projectId }, request);
  return { promise: getContractEmployees(projectId, contractId) };
};

const employeesLoader = async ({ request }: LoaderFunctionArgs) => {
  await denyUnless(UiAction.employees.list, {}, request);
  return { promise: getEmployees() };
};

const employeeRolesLoader = async ({ request }: LoaderFunctionArgs) => {
  await denyUnless(UiAction.nav.employeeRoles, {}, request);
  return { promise: getEmployees() };
};

const employeeLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const employeeId = requireEmployeeId(params);
  await denyUnless(UiAction.employees.view, { employeeId }, request);
  return { promise: getEmployee(employeeId) };
};

const employeeTimesheetsLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const employeeId = requireEmployeeId(params);
  await denyUnless(UiAction.employees.view, { employeeId }, request);
  return loadEmployeeTimesheetsPage(employeeId, request);
};

const employeePositionsLoader = async ({ params, request }: LoaderFunctionArgs) => {
  const employeeId = requireEmployeeId(params);
  await denyUnless(UiAction.employees.view, { employeeId }, request);
  return { promise: getEmployeePositions(employeeId) };
};

const timesheetLoader = async ({ request }: LoaderFunctionArgs) => {
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
    loader: rootLoader,
    children: [
      {
        path: "projects",
        element: <ProjectsPage />,
        loader: projectsLoader,
      },
      {
        path: "projects/:id",
        element: <ProjectPage />,
        loader: projectLoader,
        children: [
          {
            index: true,
            element: <ProjectContracts />,
            loader: projectContractsLoader,
          },
          {
            path: "project-managers",
            element: <ProjectManagers />,
            loader: projectManagersLoader,
          },
          {
            path: "contracts-managers",
            element: <ProjectContractsManagers />,
            loader: projectContractsManagersLoader,
          },
        ],
      },
      {
        path: "projects/:id/contracts/:contractId",
        element: <ContractPage />,
        loader: contractPageLoader,
        children: [
          {
            index: true,
            element: <ContractTimesheets />,
            loader: contractTimesheetsLoader,
          },
          {
            path: "employees",
            element: <ContractEmployees />,
            loader: contractEmployeesLoader,
          },
        ],
      },
      {
        path: "employees",
        element: <EmployeesPage />,
        loader: employeesLoader,
      },
      {
        path: "employees/roles",
        element: <EmployeeRolesPage />,
        loader: employeeRolesLoader,
      },
      {
        path: "employees/:id",
        element: <EmployeePage />,
        loader: employeeLoader,
        children: [
          {
            index: true,
            element: <EmployeeTimesheets />,
            loader: employeeTimesheetsLoader,
          },
          {
            path: "positions",
            element: <EmployeePositions />,
            loader: employeePositionsLoader,
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
        loader: timesheetLoader,
      },
    ],
  },
]);
