import { Texts } from "@/common/Texts";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { ProjectsFilterState } from "./hooks/useProjectFilters";

interface FilterProps {
  value: ProjectsFilterState;
  onChange: (value: ProjectsFilterState) => void;
}

export const ProjectsFilter = ({ value, onChange }: FilterProps) => {
  return (
    <div className="flex items-center gap-4 flex-wrap">
      <Input type="text" placeholder={Texts.search} value={value.query} onChange={(e) => onChange({ ...value, query: e.target.value })} className="w-64" />
      <div className="flex items-center gap-3">
        <Checkbox id="onlyActive" checked={value.onlyActive} onCheckedChange={(checked: boolean) => onChange({ ...value, onlyActive: checked })} />
        <Label htmlFor="onlyActive">{Texts.activeOnly}</Label>
      </div>
    </div>
  );
};
