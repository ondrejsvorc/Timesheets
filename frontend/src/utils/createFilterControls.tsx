import type { Draft } from "immer";
import { useId } from "react";
import { useFilterContext } from "@/components/shared/layout/FilterBar";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { cn } from "@/utils/cn";

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

  const FilterSelect = <TValue extends string>({
    field,
    options,
    className,
  }: {
    field: StringKeys<TFilter>;
    options: ReadonlyArray<{ value: TValue; label: string }>;
    className?: string;
  }) => {
    const { filter, setFilter } = useFilterContext<TFilter>();

    return (
      <Select
        value={filter[field] as string}
        onValueChange={(value) =>
          setFilter((draft) => {
            (draft as { [P in typeof field]: string })[field] = value;
          })
        }
      >
        <SelectTrigger className={cn("w-44", className)}>
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    );
  };

  return { FilterSearchInput, FilterCheckbox, FilterSelect };
};

type StringKeys<T> = {
  [K in keyof T]: T[K] extends string ? K : never;
}[keyof T];

type BooleanKeys<T> = {
  [K in keyof T]: T[K] extends boolean ? K : never;
}[keyof T];

const setBooleanField = <T, K extends BooleanKeys<T>>(draft: Draft<T>, key: K, value: boolean) => {
  (draft as { [P in K]: boolean })[key] = value;
};
