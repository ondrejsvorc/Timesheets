interface SubPageHeaderProps {
  children: React.ReactNode;
  actions?: React.ReactNode;
}

export const SubPageHeader = ({ children, actions }: SubPageHeaderProps) => {
  return (
    <div className="flex items-start justify-between gap-4 py-6">
      <div className="min-w-0 space-y-1">{children}</div>
      {actions && <div className="shrink-0 flex items-center gap-2">{actions}</div>}
    </div>
  );
};

export const SubPageTitle = ({ children }: { children: React.ReactNode }) => {
  return <h2 className="text-xl font-semibold leading-tight tracking-tight select-none text-foreground">{children}</h2>;
};

export const SubPageSubtitle = ({ children }: { children: React.ReactNode }) => {
  return <p className="text-sm text-muted-foreground leading-relaxed">{children}</p>;
};
