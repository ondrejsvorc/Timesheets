import { createBrowserRouter } from "react-router";
import { App } from "./App";
import { getEmployees } from "./features/employees/api/getEmployees";
import { EmployeesPage } from "./features/employees/EmployeesPage";
import { getProjects } from "./features/projects/api/getProjects";
import { ProjectsPage } from "./features/projects/ProjectsPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      {
        path: "/projects",
        element: <ProjectsPage />,
        loader: getProjects,
      },
      {
        path: "/employees",
        element: <EmployeesPage />,
        loader: getEmployees,
      },
    ],
  },
]);

export const routes = {
  projects: () => "/projects",
  employees: () => "/employees",
  projectDetail: (id: string) => `/projects/${id}`,
};
