import React, { useState } from "react";

type AttendanceTimesheet = {
  employeePersonalNumber: number;
  employeeName?: string;
  year: number;
  month: number;
  days: AttendanceDay[];
  totalHoursWithoutBreak: number;
  totalHoursObligation: number;
};

type AttendanceDay = {
  date: string;
  clockIn?: string;
  clockOut?: string;
  breakStart?: string;
  breakEnd?: string;
  otherInterruption?: string;
  hoursWithoutBreak?: number;
  isHoliday: boolean;
  isWeekend: boolean;
  isWorkDay: boolean;
};

export const AttendanceTimesheetImporter = () => {
  const [file, setFile] = useState<File | null>(null);
  const [timesheet, setTimesheet] = useState<AttendanceTimesheet | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setTimesheet(null);

    if (!file) {
      setError("Vyberte soubor (.xls nebo .xlsx).");
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    try {
      setLoading(true);
      const response = await fetch("https://localhost:7167/api/timesheets/attendance/import", {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const text = await response.text();
        setError(`Chyba: ${text}`);
        return;
      }

      const data = await response.json();
      setTimesheet(data.timesheet);
    } catch (err: any) {
      setError(`Chyba při komunikaci se serverem: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ padding: "1rem" }}>
      <h1>Import výkazu pracovní doby</h1>

      <form onSubmit={handleSubmit}>
        <input
          type="file"
          accept=".xls,.xlsx"
          onChange={(e) => setFile(e.target.files?.[0] || null)}
        />
        <button type="submit" disabled={loading}>
          {loading ? "Nahrávám..." : "Importovat"}
        </button>
      </form>

      {error && <p style={{ color: "red" }}>{error}</p>}

      {timesheet && (
        <>
          <h2>
            {timesheet.employeeName} ({timesheet.month}/{timesheet.year})
          </h2>

          <table border={1} cellPadding={4} cellSpacing={0}>
            <thead>
              <tr>
                <th>Den</th>
                <th>Příchod</th>
                <th>Odchod</th>
                <th>Začátek přestávky</th>
                <th>Konec přestávky</th>
                <th>Jiné přerušení (úvazek)</th>
                <th>Celkem od - do bez přestávky na jídlo</th>
              </tr>
            </thead>
            <tbody>
              {timesheet.days.map((d) => (
                <tr key={d.date}>
                  <td>{d.date}</td>
                  <td>{d.clockIn ?? ""}</td>
                  <td>{d.clockOut ?? ""}</td>
                  <td>{d.breakStart ?? ""}</td>
                  <td>{d.breakEnd ?? ""}</td>
                  <td>{d.otherInterruption ?? ""}</td>
                  <td>{d.hoursWithoutBreak ?? ""}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <p>
            <strong>Součet:</strong>{" "}
            {timesheet.totalHoursWithoutBreak} / {timesheet.totalHoursObligation}
          </p>
        </>
      )}
    </div>
  );
}
