interface FilterBarProps {
  children: React.ReactNode;
}

export const FilterBar = ({ children }: FilterBarProps) => {
  return <div className="flex items-center justify-between gap-4 mb-6">{children}</div>;
};
