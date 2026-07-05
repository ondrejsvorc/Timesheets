import { compareIds } from "./common";

export type ListCrudState<TItem, TDeleteKey = string> = {
  items: TItem[];
  pendingDelete: TDeleteKey | null;
};

export type ListCrudAction<TItem, TDeleteKey = string> =
  | { type: "add"; item: TItem }
  | { type: "update"; item: TItem }
  | { type: "requestDelete"; key: TDeleteKey }
  | { type: "confirmDelete" }
  | { type: "cancelDelete" };

export const listCrudState = <TItem, TDeleteKey = string>(items: TItem[]): ListCrudState<TItem, TDeleteKey> => ({
  items,
  pendingDelete: null,
});

export const createListCrudReducer =
  <TItem, TDeleteKey>(matchesDelete: (item: TItem, key: TDeleteKey) => boolean, matchesUpdate?: (item: TItem, updated: TItem) => boolean) =>
  (draft: ListCrudState<TItem, TDeleteKey>, action: ListCrudAction<TItem, TDeleteKey>) => {
    switch (action.type) {
      case "add":
        draft.items.push(action.item);
        break;
      case "update": {
        const match = matchesUpdate ?? ((item, updated) => compareIds((item as { id: string }).id, (updated as { id: string }).id));
        const index = draft.items.findIndex((entry) => match(entry, action.item));
        if (index !== -1) {
          draft.items[index] = action.item;
        }
        break;
      }
      case "requestDelete":
        draft.pendingDelete = action.key;
        break;
      case "cancelDelete":
        draft.pendingDelete = null;
        break;
      case "confirmDelete": {
        if (draft.pendingDelete === null) {
          return;
        }
        const key = draft.pendingDelete;
        const index = draft.items.findIndex((entry) => matchesDelete(entry, key));
        if (index !== -1) {
          draft.items.splice(index, 1);
        }
        draft.pendingDelete = null;
        break;
      }
    }
  };

export const listCrudReducer = createListCrudReducer<{ id: string }, string>((item, id) => compareIds(item.id, id));
