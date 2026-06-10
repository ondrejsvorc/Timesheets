import type { Draft } from "immer";
import { useId } from "react";
import { useFilterContext } from "@/components/shared/layout/FilterBar";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export const createFilterControls = <TFilter extends { query: string }>() => {
  const FilterSearchInput = ({ placeholder }: { placeholder?: string }) => {
    const { filter, setFilter } = useFilterContext<TFilter>();

    return (
      <Input
        type="text"
        value={filter.query}
        placeholder={placeholder}
        onChange={(e) =>
          setFilter((draft) => {
            draft.query = e.target.value;
          })
        }
        className="w-64"
      />
    );
  };

  const FilterCheckbox = ({
    field,
    label,
    exclusiveWith,
  }: {
    field: BooleanKeys<TFilter>;
    label?: string;
    exclusiveWith?: BooleanKeys<TFilter>[];
  }) => {
    const { filter, setFilter } = useFilterContext<TFilter>();
    const id = useId();
    const checked = filter[field] === true;

    return (
      <div className="flex h-9 items-center gap-3">
        <Checkbox
          id={id}
          checked={checked}
          onCheckedChange={(value) =>
            setFilter((draft) => {
              const nextChecked = value === true;
              setBooleanField(draft, field, nextChecked);
              if (nextChecked && exclusiveWith) {
                for (const otherField of exclusiveWith) {
                  setBooleanField(draft, otherField, false);
                }
              }
            })
          }
        />
        {label && (
          <Label htmlFor={id} className="cursor-pointer">
            {label}
          </Label>
        )}
      </div>
    );
  };

  return { FilterSearchInput, FilterCheckbox };
};

type BooleanKeys<T> = {
  [K in keyof T]: T[K] extends boolean ? K : never;
}[keyof T];

const setBooleanField = <T, K extends BooleanKeys<T>>(draft: Draft<T>, key: K, value: boolean) => {
  (draft as { [P in K]: boolean })[key] = value;
};
