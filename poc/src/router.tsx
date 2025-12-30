import { createBrowserRouter } from "react-router";
import { App } from "./App";
import { ProjectsPage } from "./features/projects/ProjectsPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      {
        index: true,
        element: <ProjectsPage />,
      },
    ],
  },
]);
