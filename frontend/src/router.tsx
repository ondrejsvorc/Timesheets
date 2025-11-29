import { createBrowserRouter } from "react-router";
import { ProjectsPage } from "./projects/ProjectsPage";
import { Layout } from "./Layout";
import { getProjects } from "./projects/api/getProjects";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        index: true,
        element: <ProjectsPage />,
        loader: async () => await getProjects(),
      }
    ],
  },
]);