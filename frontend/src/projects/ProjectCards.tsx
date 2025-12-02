import type { ProjectItem } from "./api/getProjects";
import { ProjectCard } from "./ProjectCard";

export const ProjectCards = ({ projects }: { projects: ProjectItem[] }) => {
  return (
    <div className="grid grid-cols-3 gap-6">
      {projects.map(p => <ProjectCard key={p.id} project={p} />)}
    </div>
  );
};
