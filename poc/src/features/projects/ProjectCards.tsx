import type { ProjectItem } from "./api/shared/projectItem";
import { ProjectCard } from "./ProjectCard";

interface ProjectCardsProps {
  projects: ProjectItem[];
}

export const ProjectCards = ({ projects }: ProjectCardsProps) => {
  if (projects.length === 0) {
    return <div className="text-center py-12 text-muted-foreground">Žádné projekty nenalezeny.</div>;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {projects.map((project) => (
        <ProjectCard key={project.id} project={project} />
      ))}
    </div>
  );
};
