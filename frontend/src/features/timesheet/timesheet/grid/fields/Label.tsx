interface LabelProps {
  label: string;
}

export const Label = ({ label }: LabelProps) => {
  return <div className="text-center font-medium">{label}</div>;
};
