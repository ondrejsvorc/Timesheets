import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";
import type { EmployeesFilterState } from "./hooks/useEmployeeFilters";

interface FilterProps {
  value: EmployeesFilterState;
  onChange: (value: EmployeesFilterState) => void;
}

export const EmployeesFilter = ({ value, onChange }: FilterProps) => {
  return (
    <div className="flex items-center gap-4 flex-wrap">
      <Input
        type="text"
        placeholder={Texts.search}
        value={value.query}
        onChange={(e) => onChange({ ...value, query: e.target.value })}
        className="w-64"
      />
    </div>
  );
};
