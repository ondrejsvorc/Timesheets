import { MoreVertical } from "lucide-react";
import { Texts } from "../common/Texts";
import type { ProjectItem } from "./ProjectsPage";

export const ProjectCard = ({ project }: { project: ProjectItem }) => {
  const isActive = project.endDate === null || new Date(project.endDate) > new Date();

  return (
    <div className="relative border border-gray-300 rounded-lg p-4 bg-white flex flex-col gap-2 cursor-pointer">
      <div className="flex items-start justify-between">
        <div className="text-lg font-semibold text-gray-900">
          {project.name}
        </div>

        <button className="text-gray-500 hover:text-black">
          <MoreVertical className="w-4 h-4" />
        </button>
      </div>

      <div className="text-sm text-gray-600">
        {Texts.projectId}: {project.id}
      </div>

      <div className="text-sm text-gray-600">
        {project.startDate} - {project.endDate ?? "—"}
      </div>

      <div className="text-sm text-gray-600">
        {Texts.contracts}: {project.contractCount}
      </div>

      <div className="mt-2">
        <span className={`px-2 py-1 rounded text-xs ${isActive ? "bg-green-100 text-green-800" : "bg-gray-200 text-gray-600"}`}>
          {isActive ? Texts.active : Texts.inactive}
        </span>
      </div>
    </div>
  );
};
