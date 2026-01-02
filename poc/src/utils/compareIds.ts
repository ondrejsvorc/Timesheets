export const compareIds = (a: string | null | undefined, b: string | null | undefined): boolean => {
  if (!a || !b) {
    return false;
  }

  return a.toLowerCase() === b.toLowerCase();
};
