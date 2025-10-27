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

  function handleTimeChange(index: number, field: "clockIn" | "clockOut" | "breakStart" | "breakEnd", value: string) {
    if (!timesheet) return;

    const updatedDays = timesheet.days.map((day, i) => {
      if (i !== index) return day;

      const updatedDay = { ...day, [field]: value };
      const { clockIn, clockOut } = updatedDay;

      if (clockIn && clockOut) {
        const diff = computeHours(clockIn, clockOut);
        updatedDay.hoursWithoutBreak = diff;
      } else {
        updatedDay.hoursWithoutBreak = undefined;
      }

      return updatedDay;
    });

    const total = updatedDays.reduce((sum, d) => sum + (d.hoursWithoutBreak ?? 0), 0);

    setTimesheet({
      ...timesheet,
      days: updatedDays,
      totalHoursWithoutBreak: total,
    });
  }

  function computeHours(clockIn: string, clockOut: string): number {
    const [inH = 0, inM = 0, inS = 0] = clockIn.split(":").map(Number);
    const [outH = 0, outM = 0, outS = 0] = clockOut.split(":").map(Number);

    const start = inH * 3600 + inM * 60 + inS;
    const end = outH * 3600 + outM * 60 + outS;
    const diffSec = Math.max(0, end - start);

    return +(diffSec / 3600).toFixed(2);
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
                <th>Datum</th>
                <th>Příchod</th>
                <th>Odchod</th>
                <th>Začátek pauzy</th>
                <th>Konec pauzy</th>
                <th>Jiné přerušení (úvazek)</th>
                <th>Celkem od - do bez přestávky</th>
              </tr>
            </thead>
            <tbody>
              {timesheet.days.map((d, i) => (
                <tr key={d.date}>
                  <td>{d.date}</td>

                  <td>
                    <input
                      type="time"
                      value={d.clockIn ?? ""}
                      onChange={(e) => handleTimeChange(i, "clockIn", e.target.value)}
                    />
                  </td>

                  <td>
                    <input
                      type="time"
                      value={d.clockOut ?? ""}
                      onChange={(e) => handleTimeChange(i, "clockOut", e.target.value)}
                    />
                  </td>

                  <td>
                    <input
                      type="time"
                      value={d.breakStart ?? ""}
                      onChange={(e) => handleTimeChange(i, "breakStart", e.target.value)}
                    />
                  </td>

                  <td>
                    <input
                      type="time"
                      value={d.breakEnd ?? ""}
                      onChange={(e) => handleTimeChange(i, "breakEnd", e.target.value)}
                    />
                  </td>

                  <td>
                    <input
                      type="text"
                      value={d.otherInterruption ?? ""}
                      onChange={(e) =>
                        setTimesheet((prev) => {
                          if (!prev) return prev;
                          const updatedDays = [...prev.days];
                          updatedDays[i] = { ...updatedDays[i], otherInterruption: e.target.value };
                          return { ...prev, days: updatedDays } as AttendanceTimesheet;
                        })
                      }
                    />
                  </td>

                  <td>{d.hoursWithoutBreak ?? ""}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <p>
            <strong>Součet:</strong> {timesheet.totalHoursWithoutBreak} /{" "}
            {timesheet.totalHoursObligation}
          </p>
        </>
      )}
    </div>
  );
}
