interface WorkedHoursProps {
  value: number;
}

export const WorkedHours = ({ value }: WorkedHoursProps) => {
  return (
    <div className="text-center font-bold tabular-nums">
      {value.toFixed(2)}
    </div>
  );
};