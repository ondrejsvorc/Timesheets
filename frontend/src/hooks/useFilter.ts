import type { Draft } from "immer";
import { matchSorter, rankings } from "match-sorter";
import { useMemo } from "react";
import { useImmer } from "use-immer";

export interface FilterCriteria {
  query: string;
}

export interface FilterProps<TFilter> {
  filter: TFilter;
  setFilter: (updater: (draft: Draft<TFilter>) => void) => void;
}

interface FilterOptions<T, TFilter extends FilterCriteria> {
  items: T[];
  initialFilter: TFilter;
  keys: Array<(item: T) => string>;
  predicates?: Array<(item: T, filter: TFilter) => boolean>;
}

export const useFilter = <T, TFilter extends FilterCriteria>(options: FilterOptions<T, TFilter>) => {
  const { items, initialFilter, keys, predicates = [] } = options;
  const [filter, setFilter] = useImmer<TFilter>(initialFilter);

  const filtered = useMemo(() => {
    const base = predicates.reduce((acc, predicate) => acc.filter((item) => predicate(item, filter)), items);
    const query = filter.query.trim();
    if (!query) {
      return base;
    }
    return matchSorter(base, query, {
      keys,
      threshold: rankings.CONTAINS,
      keepDiacritics: false,
    });
  }, [items, filter, keys, predicates]);

  return { filter, setFilter, filtered };
};
