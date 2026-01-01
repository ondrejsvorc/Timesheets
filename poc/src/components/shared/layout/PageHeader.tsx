interface PageHeaderProps {
  leading?: React.ReactNode;
  actions?: React.ReactNode;
  children: React.ReactNode;
}

export const PageHeader = ({ leading, actions, children }: PageHeaderProps) => {
  return (
    <div className="flex items-start justify-between gap-4 mb-6">
      <div className="flex items-start gap-3 min-w-0">
        {leading && <div className="shrink-0 mt-1">{leading}</div>}
        <div className="min-w-0 space-y-1">{children}</div>
      </div>
      {actions && <div className="shrink-0 flex items-center gap-2">{actions}</div>}
    </div>
  );
};

export const PageTitle = ({ children }: { children: React.ReactNode }) => {
  return <h1 className="text-3xl font-semibold leading-tight tracking-tight select-none text-foreground">{children}</h1>;
};

export const PageSubtitle = ({ children }: { children: React.ReactNode }) => {
  return <p className="text-sm text-muted-foreground leading-relaxed">{children}</p>;
};
