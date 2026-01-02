import { createBrowserRouter, type Params, redirect } from "react-router";
import { App } from "./App";
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

const requireProjectId = (params: Params) => {
  if (!params.id) {
    throw redirect("/projects");
  }
  return params.id;
};

export const router = createBrowserRouter([
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
        path: "employees",
        element: <EmployeesPage />,
        loader: getEmployees,
      },
    ],
  },
]);
