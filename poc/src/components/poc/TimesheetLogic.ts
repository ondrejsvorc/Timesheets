import type { Attendance, TimeRange, Timesheet, TimesheetDay } from "./Timesheet";

/**
 * Converts "HH:mm" time format to minutes.
 */
const toMinutes = (time: string): number => {
  const [hours, minutes] = (time || "").split(":").map(Number);
  return (hours || 0) * 60 + (minutes || 0);
};

export const TimesheetLogic = {
  calculateWorkedHours: (attendance: Attendance): number => {
    // Kontrola, zda jsou časy vyplněné (prázdný string, ne 0 minut!)
    if (!attendance.clockIn || !attendance.clockOut) {
      return 0;
    }

    const clockIn = toMinutes(attendance.clockIn);
    const clockOut = toMinutes(attendance.clockOut);

    // Pokud je odchod menší než příchod, znamená to, že směna pokračuje přes půlnoc
    // (např. příchod 22:00, odchod 06:00 = odchod je další den ráno = 8 hodin)
    // (např. příchod 08:00, odchod 00:00 = odchod je další den o půlnoci = 16 hodin)
    let actualClockOut = clockOut;
    if (clockOut < clockIn) {
      actualClockOut = clockOut + 24 * 60; // Přidáme 24 hodin
      const workedMinutes = actualClockOut - clockIn;
      // Pokud by noční směna byla víc než 12 hodin, považujeme to za chybu (odchod před příchodem)
      // Např. 16:00 - 08:00 = 16h je neplatné, ale 22:00 - 06:00 = 8h je OK
      if (workedMinutes > 12 * 60) {
        return 0; // Neplatné - více než 12 hodin noční směny
      }
    }

    // Pokud jsou si rovné, je to neplatné
    if (actualClockOut <= clockIn) {
      return 0;
    }

    const workedMinutes = actualClockOut - clockIn;

    // Výpočet přestávky - pouze pokud je validní
    let breakMinutes = 0;
    if (attendance.breakStart && attendance.breakEnd) {
      const breakStart = toMinutes(attendance.breakStart);
      const breakEnd = toMinutes(attendance.breakEnd);
      
      // Pokud je konec přestávky < začátek, může to být přestávka přes půlnoc
      if (breakEnd < breakStart) {
        // Přestávka přes půlnoc - zkontrolujeme, zda je to validní
        // Pokud by přestávka přes půlnoc byla víc než 12 hodin, je to nevalidní
        // (např. 12:30-12:00 není přestávka přes půlnoc, ale nevalidní přestávka)
        const breakDuration = breakEnd + 24 * 60 - breakStart;
        if (breakDuration <= 12 * 60) {
          // Validní přestávka přes půlnoc (max 12h)
          breakMinutes = breakDuration;
        }
        // Pokud je breakDuration > 12h, je to nevalidní (např. 12:30-12:00), takže breakMinutes zůstane 0
      } else {
        // Normální přestávka - konec musí být později než začátek
        if (breakEnd > breakStart) {
          breakMinutes = breakEnd - breakStart;
        }
        // Pokud breakEnd <= breakStart, je to nevalidní, takže breakMinutes zůstane 0
      }
    }

    const workedMinutesWithoutBreak = workedMinutes - breakMinutes;
    const workedHours = Number((workedMinutesWithoutBreak / 60).toFixed(2));

    return workedHours;
  },

  formatWorkedHoursToHuman: (hours: number): string => {
    if (hours <= 0) return "0";

    const totalMinutes = Math.round(hours * 60);
    const wholeHours = Math.floor(totalMinutes / 60);
    const remainingMinutes = totalMinutes % 60;

    const parts: string[] = [];
    if (wholeHours > 0) parts.push(`${wholeHours}h`);
    if (remainingMinutes > 0) parts.push(`${remainingMinutes}m`);

    return parts.join(" ") || "0";
  },

  /**
   * Vypočítá počet nočních hodin (22:00 - 05:59) v pracovní době
   */
  calculateNightHours: (attendance: Attendance): number => {
    if (!attendance.clockIn || !attendance.clockOut) {
      return 0;
    }

    const clockIn = toMinutes(attendance.clockIn);
    const clockOut = toMinutes(attendance.clockOut);

    const nightStart = 22 * 60; // 22:00 = 1320 minut
    const nightEnd = 5 * 60 + 59; // 05:59 = 359 minut

    // Pokud je odchod < příchod, znamená to noční směnu přes půlnoc
    let actualClockOut = clockOut;
    const isNightShift = clockOut < clockIn;
    if (isNightShift) {
      actualClockOut = clockOut + 24 * 60;
      // Pokud je to víc než 12h, není to validní noční směna
      if (actualClockOut - clockIn > 12 * 60) {
        return 0;
      }
    }

    let nightMinutes = 0;

    // Noční čas je 22:00-05:59 (přes půlnoc)
    // Projdeme každou minutu pracovní doby a zkontrolujeme, zda je v nočním čase
    for (let minute = clockIn; minute < actualClockOut; minute++) {
      const minuteOfDay = minute % (24 * 60); // Minuta v rámci dne (0-1439)
      
      // Zkontrolujeme, zda je minuta v nočním čase (22:00-05:59)
      if (minuteOfDay >= nightStart || minuteOfDay <= nightEnd) {
        nightMinutes++;
      }
    }

    // Odečteme přestávku, pokud zasahuje do nočního času (22:00-05:59)
    if (attendance.breakStart && attendance.breakEnd) {
      const breakStart = toMinutes(attendance.breakStart);
      const breakEnd = toMinutes(attendance.breakEnd);
      
      // Pokud je konec přestávky < začátek, znamená to přestávku přes půlnoc
      let actualBreakEnd = breakEnd;
      if (breakEnd < breakStart) {
        actualBreakEnd = breakEnd + 24 * 60;
      }
      
      // Projdeme každou minutu přestávky a zkontrolujeme, zda je v nočním čase
      for (let minute = breakStart; minute < actualBreakEnd; minute++) {
        const minuteOfDay = minute % (24 * 60);
        // Zkontrolujeme, zda je minuta v nočním čase (22:00-05:59)
        if (minuteOfDay >= nightStart || minuteOfDay <= nightEnd) {
          nightMinutes--;
        }
      }
    }

    return Number((Math.max(0, nightMinutes) / 60).toFixed(2));
  },

  calculateSchedulesTotal: (schedules: TimeRange[]): number => {
    if (!schedules || schedules.length === 0) return 0;

    const totalMinutes = schedules.reduce((acc, range) => {
      const start = toMinutes(range.start);
      const end = toMinutes(range.end);

      if (range.start && range.end && end > start) {
        return acc + (end - start);
      }
      return acc;
    }, 0);

    return Number((totalMinutes / 60).toFixed(2));
  },

  calculateMonthlyFund: (timesheet: Timesheet): number => {
    const workingDaysCount = timesheet.days.filter((day) => !day.isWeekend && !day.isHoliday).length;
    const standardDayHours = 8;
    const totalFundHours = workingDaysCount * (standardDayHours * timesheet.totalWorkload);
    return Number(totalFundHours.toFixed(2));
  },

  calculateMonthlyTotalWorked: (days: TimesheetDay[]): number => {
    return days.reduce((sum, day) => {
      return sum + TimesheetLogic.calculateWorkedHours(day.attendance);
    }, 0);
  },

  calculateMonthlyTotalAllocated: (days: TimesheetDay[]): number => {
    return days.reduce((sum, day) => {
      const dayAllocated = day.coreHours + Object.values(day.projectHours).reduce((a, b) => a + (b || 0), 0);
      return sum + dayAllocated;
    }, 0);
  },

  calculateWorkloadFund: (timesheet: Timesheet, workload: number): number => {
    const workingDaysCount = timesheet.days.filter((day) => !day.isWeekend && !day.isHoliday).length;
    const standardDayHours = 8;
    return Number((workingDaysCount * (standardDayHours * workload)).toFixed(2));
  },

  isCoreHoursValid: (day: TimesheetDay): boolean => {
    const stagTotal = TimesheetLogic.calculateSchedulesTotal(day.attendance.schedules);
    // Kmen musí být >= STAG rozvrh
    return day.coreHours >= stagTotal;
  },

  distributeRemainingHours: (day: TimesheetDay, timesheet: Timesheet) => {
    // 1. Zjistíme cílovou kapacitu dne (docházka nebo 8h fallback)
    let targetTotal = TimesheetLogic.calculateWorkedHours(day.attendance);
    if (targetTotal === 0 && !day.isWeekend && !day.isHoliday) {
      targetTotal = 8 * timesheet.totalWorkload;
    }

    // 2. Spočítáme, co už uživatel vyplnil (tohle zůstane netknuté)
    const currentAllocated = (day.coreHours || 0) + Object.values(day.projectHours).reduce((sum, val) => sum + (val || 0), 0);

    // Rozdíl, který musíme "dogenerovat"
    let delta = Number((targetTotal - currentAllocated).toFixed(2));

    // Pokud už je vše vyplněno (nebo víc), nic neděláme
    if (delta <= 0.01) return null;

    return (onUpdate: any) => {
      onUpdate((draft: TimesheetDay) => {
        // --- KROK 1: DOPLNĚNÍ KMENE (STAG) ---
        // Doplňujeme Kmen JEN pokud je v něm nula nebo méně než vyžaduje STAG
        const stagHours = TimesheetLogic.calculateSchedulesTotal(day.attendance.schedules);
        if (draft.coreHours < stagHours && delta > 0) {
          const needed = Number((stagHours - draft.coreHours).toFixed(2));
          const toAdd = Math.min(needed, delta);
          draft.coreHours = Number((draft.coreHours + toAdd).toFixed(2));
          delta = Number((delta - toAdd).toFixed(2));
        }

        if (delta <= 0) return;

        // --- KROK 2: IDENTIFIKACE SKUTEČNĚ PRÁZDNÝCH PROJEKTŮ ---
        // Vybereme jen ty projekty, kde je nula nebo undefined (uživatel je nevyplnil)
        const emptyProjectIds = timesheet.projects.filter((p) => !draft.projectHours[p.id] || draft.projectHours[p.id] === 0).map((p) => p.id);

        if (emptyProjectIds.length === 0) {
          // Pokud jsou všechny projekty už vyplněné, zbytek "přilepíme" ke kmeni
          draft.coreHours = Number((draft.coreHours + delta).toFixed(2));
          return;
        }

        // --- KROK 3: NÁHODNÝ VÝBĚR Z PRÁZDNÝCH ---
        // Aby to bylo lidské, nevybereme vždycky všechny prázdné
        const selectedIds = emptyProjectIds.filter(() => Math.random() > 0.3);
        const finalTargets = selectedIds.length > 0 ? selectedIds : [emptyProjectIds[0]];

        let runningDelta = delta;

        finalTargets.forEach((id, index) => {
          const isLast = index === finalTargets.length - 1;
          let share: number;

          if (isLast) {
            share = runningDelta;
          } else {
            const pDef = timesheet.projects.find((p) => p.id === id);
            const weight = pDef ? pDef.workload : 1;
            const totalWeight = timesheet.projects.filter((p) => finalTargets.includes(p.id)).reduce((s, p) => s + p.workload, 0);

            const rawShare = (weight / totalWeight) * delta;
            // Držíme se minima 1h, pokud to runningDelta dovolí
            share = Math.max(1.0, Math.round(rawShare * 2) / 2);
            share = Math.min(share, runningDelta - (finalTargets.length - 1 - index) * 0.5);
          }

          if (share > 0) {
            draft.projectHours[id] = Number(share.toFixed(2));
            runningDelta = Number((runningDelta - share).toFixed(2));
          }
        });

        // --- KROK 4: FINÁLNÍ DOPLNĚNÍ ---
        // Pokud i po tomhle zbyla nějaká setina, hodíme ji do kmene
        if (runningDelta > 0) {
          draft.coreHours = Number((draft.coreHours + runningDelta).toFixed(2));
        }
      });
    };
  },

  distributeMonthlyHours: (timesheet: Timesheet, onUpdateDay: (date: string, recipe: any) => void) => {
    const monthlyFund = TimesheetLogic.calculateMonthlyFund(timesheet);

    const localData: Record<string, { core: number; projects: Record<string, number>; cap: number }> = {};
    let coreUsed = 0;
    const pUsed: Record<string, number> = {};
    timesheet.projects.forEach((p) => {
      pUsed[p.id] = 0;
    });

    timesheet.days.forEach((day) => {
      let cap = TimesheetLogic.calculateWorkedHours(day.attendance);
      if (cap === 0 && !day.isWeekend && !day.isHoliday) cap = 8 * timesheet.totalWorkload;

      localData[day.date] = { core: day.coreHours || 0, projects: { ...day.projectHours }, cap };
      coreUsed = Number((coreUsed + (day.coreHours || 0)).toFixed(2));
      timesheet.projects.forEach((p) => {
        pUsed[p.id] = Number((pUsed[p.id] + (day.projectHours[p.id] || 0)).toFixed(2));
      });
    });

    let coreWallet = Number((Math.max(0, monthlyFund * (timesheet.core.workload / timesheet.totalWorkload)) - coreUsed).toFixed(2));
    const pWallets = timesheet.projects.map((p) => ({
      id: p.id,
      rem: Number((Math.max(0, monthlyFund * (p.workload / timesheet.totalWorkload)) - pUsed[p.id]).toFixed(2)),
    }));

    const workingDays = timesheet.days.filter((d) => !d.isWeekend && !d.isHoliday);

    // --- FÁZE 1: NÁHODNÉ "LIDSKÉ" BLOKY (pouze násobky 0.5h) ---
    // Zvýšíme počet iterací a snížíme max velikost bloku, aby se to víc drobilo
    const maxIterations = 800;
    for (let i = 0; i < maxIterations; i++) {
      // Filtrujeme peněženky, které mají alespoň 0.5h
      const active = pWallets.filter((w) => w.rem >= 0.5).map((w) => ({ id: w.id, isCore: false }));
      if (coreWallet >= 0.5) active.push({ id: "core", isCore: true });
      if (active.length === 0) break;

      const day = workingDays[Math.floor(Math.random() * workingDays.length)];
      const target = active[Math.floor(Math.random() * active.length)];
      const d = localData[day.date];

      const allocated = Number((d.core + Object.values(d.projects).reduce((a, b) => a + (b || 0), 0)).toFixed(2));
      const space = Number((d.cap - allocated).toFixed(2));

      // Chceme bloky 0.5h až 4.5h (aby jeden projekt nesežral celý 8h den)
      if (space >= 0.5) {
        const wallet = target.isCore ? { rem: coreWallet } : pWallets.find((w) => w.id === target.id)!;

        // Vygenerujeme násobek 0.5
        let take = (Math.floor(Math.random() * 9) + 1) * 0.5; // 0.5, 1.0, ... 4.5
        take = Number(Math.min(take, space, wallet.rem).toFixed(2));

        // Pokud "take" není násobek 0.5, zkusíme ho zaokrouhlit dolů na 0.5 (pokud to jde)
        if (take % 0.5 !== 0 && take > 0.5) {
          take = Math.floor(take * 2) / 2;
        }

        if (take >= 0.5) {
          if (target.isCore) {
            d.core = Number((d.core + take).toFixed(2));
            coreWallet = Number((coreWallet - take).toFixed(2));
          } else {
            d.projects[target.id] = Number(((d.projects[target.id] || 0) + take).toFixed(2));
            const w = pWallets.find((pw) => pw.id === target.id)!;
            w.rem = Number((w.rem - take).toFixed(2));
          }
        }
      }
    }

    // --- FÁZE 2: MATEMATICKÉ DOČIŠTĚNÍ (zbytky pod 0.5h) ---
    // Tady už jdeme "na krev", aby to sedělo na setiny
    const finalCleanup = (id: string, isCore: boolean) => {
      let rem = isCore ? coreWallet : pWallets.find((w) => w.id === id)!.rem;
      if (rem <= 0) return;

      // Seřadíme dny náhodně, aby ty "divné" setiny nebyly vždycky na začátku měsíce
      const randomCleanupDays = [...workingDays].sort(() => Math.random() - 0.5);

      for (const day of randomCleanupDays) {
        const d = localData[day.date];
        const allocated = Number((d.core + Object.values(d.projects).reduce((a, b) => a + (b || 0), 0)).toFixed(2));
        const space = Number((d.cap - allocated).toFixed(2));

        if (space > 0) {
          const take = Number(Math.min(space, rem).toFixed(2));
          if (isCore) {
            d.core = Number((d.core + take).toFixed(2));
            coreWallet = Number((coreWallet - take).toFixed(2));
          } else {
            d.projects[id] = Number(((d.projects[id] || 0) + take).toFixed(2));
            pWallets.find((w) => w.id === id)!.rem = Number((pWallets.find((w) => w.id === id)!.rem - take).toFixed(2));
          }
          rem = Number((rem - take).toFixed(2));
        }
        if (rem <= 0) break;
      }
    };

    pWallets.forEach((w) => finalCleanup(w.id, false));
    finalCleanup("core", true);

    // --- FÁZE 3: PROPIS ---
    Object.entries(localData).forEach(([date, data]) => {
      onUpdateDay(date, (draft: TimesheetDay) => {
        draft.coreHours = Number(data.core.toFixed(2));
        Object.entries(data.projects).forEach(([pid, val]) => {
          draft.projectHours[pid] = Number(val.toFixed(2));
        });
      });
    });
  },

  getDelta: (day: TimesheetDay): number => {
    const worked = TimesheetLogic.calculateWorkedHours(day.attendance);
    const allocated = day.coreHours + Object.values(day.projectHours).reduce((sum, h) => sum + h, 0);
    return Number((worked - allocated).toFixed(2));
  },

  isValidTime: (time: string): boolean => {
    const regex = /^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/;
    return regex.test(time);
  },

  formatSmartTime: (value: string): string => {
    const clean = value.replace(/\D/g, "");
    if (!clean) return "";

    let h = 0;
    let m = 0;

    if (clean.length <= 2) {
      h = parseInt(clean);
    } else {
      h = parseInt(clean.slice(0, -2));
      m = parseInt(clean.slice(-2));
    }

    h = Math.min(h, 23);
    m = Math.min(m, 59);

    return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}`;
  },

  getMonthlyTotalForProject: (days: TimesheetDay[], projectId: string) => {
    return days.reduce((sum, day) => sum + (day.projectHours[projectId] || 0), 0);
  },
};
