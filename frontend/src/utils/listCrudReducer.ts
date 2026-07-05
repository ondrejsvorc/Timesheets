import { compareIds } from "./common";

export type ListCrudAction<TItem extends { id: string }> = { type: "add"; item: TItem } | { type: "update"; item: TItem } | { type: "delete"; id: string };

export const listCrudReducer = <TItem extends { id: string }>(draft: TItem[], action: ListCrudAction<TItem>) => {
  switch (action.type) {
    case "add":
      draft.push(action.item);
      break;
    case "update": {
      const index = draft.findIndex((entry) => compareIds(entry.id, action.item.id));
      if (index !== -1) {
        draft[index] = action.item;
      }
      break;
    }
    case "delete": {
      const index = draft.findIndex((entry) => compareIds(entry.id, action.id));
      if (index !== -1) {
        draft.splice(index, 1);
      }
      break;
    }
  }
};
