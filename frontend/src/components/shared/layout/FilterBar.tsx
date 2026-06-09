import type { Draft } from "immer";
import { createContext, useContext } from "react";

export interface FilterContextValue<TFilter> {
  filter: TFilter;
  setFilter: (updater: (draft: Draft<TFilter>) => void) => void;
}

const FilterContext = createContext<FilterContextValue<unknown> | null>(null);

export const useFilterContext = <TFilter,>() => {
  const ctx = useContext(FilterContext);
  if (!ctx) {
    throw new Error("useFilterContext must be used inside FilterBar");
  }
  return ctx as FilterContextValue<TFilter>;
};

interface FilterBarProps<TFilter> {
  filter: TFilter;
  setFilter: (updater: (draft: Draft<TFilter>) => void) => void;
  children: React.ReactNode;
  actions?: React.ReactNode;
}

export const FilterBar = <TFilter,>({ filter, setFilter, children, actions }: FilterBarProps<TFilter>) => {
  return (
    <FilterContext.Provider value={{ filter, setFilter }}>
      <div className="flex items-end justify-between gap-4 mb-6">
        <div className="flex items-end gap-4 flex-wrap">{children}</div>
        {actions && <div className="flex shrink-0 items-center gap-2">{actions}</div>}
      </div>
    </FilterContext.Provider>
  );
};
