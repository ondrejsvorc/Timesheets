export const formatHours = (value: number): string => {
  const rounded = Number(value.toFixed(2));
  return (Object.is(rounded, -0) ? 0 : rounded).toString().replace(".", ",");
};

export const formatWorkload = (value: number): string =>
  `${Number((value * 100).toFixed(2))
    .toString()
    .replace(".", ",")} %`;
