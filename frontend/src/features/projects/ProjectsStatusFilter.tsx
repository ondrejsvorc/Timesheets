import { useFilterContext } from "@/components/shared/layout/FilterBar";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Texts } from "@/constants/texts";
import type { ProjectStatusFilter, ProjectsFilterCriteria } from "./hooks/useProjectsFilter";

export const ProjectsStatusFilter = () => {
  const { filter, setFilter } = useFilterContext<ProjectsFilterCriteria>();

  return (
    <Select
      value={filter.status}
      onValueChange={(value) =>
        setFilter((draft) => {
          draft.status = value as ProjectStatusFilter;
        })
      }
    >
      <SelectTrigger className="w-44">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="active">{Texts.activeOnly}</SelectItem>
        <SelectItem value="archived">{Texts.archivedOnly}</SelectItem>
        <SelectItem value="all">{Texts.allProjects}</SelectItem>
      </SelectContent>
    </Select>
  );
};
