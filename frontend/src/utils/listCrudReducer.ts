import { compareIds } from "@/utils/compareIds";

export const listCrudAdd = <TItem extends { id: string }>(draft: TItem[], item: TItem) => {
  draft.push(item);
};

export const listCrudUpdate = <TItem extends { id: string }>(draft: TItem[], item: TItem) => {
  const index = draft.findIndex((entry) => compareIds(entry.id, item.id));
  if (index !== -1) {
    draft[index] = item;
  }
};

export const listCrudDelete = <TItem extends { id: string }>(draft: TItem[], id: string) => {
  const index = draft.findIndex((entry) => compareIds(entry.id, id));
  if (index !== -1) {
    draft.splice(index, 1);
  }
};
