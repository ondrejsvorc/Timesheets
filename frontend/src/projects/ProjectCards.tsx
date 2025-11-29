import { ProjectCard } from "./ProjectCard";
import type { ProjectItem } from "./ProjectsPage";

export const ProjectCards = ({ projects }: { projects: ProjectItem[] }) => {
  return (
    <div className="grid grid-cols-3 gap-6">
      {projects.map(p => <ProjectCard key={p.id} project={p} />)}
    </div>
  );
};
