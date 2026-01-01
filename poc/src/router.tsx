import { createBrowserRouter } from "react-router";
import { App } from "./App";
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
        loader: async () => await getProjects(),
      },
    ],
  },
]);

export const routes = {
  projects: () => "/projects",
  employees: () => "/employees",
  projectDetail: (id: string) => `/projects/${id}`,
};
