export const formatHours = (value: number): string => {
  const rounded = Number(value.toFixed(2));
  return (Object.is(rounded, -0) ? 0 : rounded).toString().replace(".", ",");
};
