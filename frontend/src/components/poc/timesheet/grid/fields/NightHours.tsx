interface NightHoursProps {
  value: number;
}

export const NightHours = ({ value }: NightHoursProps) => {
  return (
    <div className="text-center tabular-nums text-slate-600">
      {value.toFixed(2)}
    </div>
  );
};