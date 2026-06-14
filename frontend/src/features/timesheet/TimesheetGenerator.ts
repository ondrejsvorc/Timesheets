// ==============================
// Types
// ==============================

type ProjectMap = Record<string, number>;

type MonthlyTargets = {
  core: number;
  projects: ProjectMap;
};

type RemainingTargets = {
  core: number;
  projects: ProjectMap;
};

type Allocation = {
  core: number;
  projects: ProjectMap;
};

type EditableCells = {
  coreByDate: Record<string, boolean>;
  projectsByDate: Record<string, Record<string, boolean>>;
};

// ==============================
// Configuration
// ==============================

import type { Timesheet, TimesheetDay } from "./Timesheet";
import { dayHasBusinessTripInterruption, dayHasCoreOnlyInterruption, toMinutes } from "./timesheetUtils";

export interface TimesheetGenerationConfig {
  roundingStep: number;
  defaultDailyWorkHours: number;
}

const PREFERRED_STEPS_CENTS = [100, 50, 5, 1] as const; // 1h, 0.5h, 0.05h, 0.01h

// ==============================
// Public API
// ==============================

export const generateTimesheetData = (timesheet: Timesheet, config: TimesheetGenerationConfig): void => {
  const editable = captureEditableCells(timesheet);
  const targets: MonthlyTargets = computeMonthlyTargets(timesheet, config);
  const remaining: RemainingTargets = computeRemainingTargets(timesheet, targets);

  allocateMonth(timesheet, remaining, config, editable);
  reconcileMonthRemainders(timesheet, remaining, config, editable);
  enforceExactMonthlyTotals(timesheet, targets, config, editable);
};

// ==============================
// Monthly layer
// ==============================

const computeMonthlyTargets = (timesheet: Timesheet, config: TimesheetGenerationConfig): MonthlyTargets => {
  const totalHours: number = computeTotalMonthlyHours(timesheet, config);

  const projectTargets: ProjectMap = {};
  let allocatedToProjects: number = 0;

  for (const project of timesheet.projects) {
    const hours: number = normalizeWorkloadRatio(project.workload) * totalHours;
    projectTargets[project.id] = hours;
    allocatedToProjects += hours;
  }

  const coreHours: number = Math.max(0, totalHours - allocatedToProjects);

  return {
    core: coreHours,
    projects: projectTargets,
  };
};

const computeRemainingTargets = (timesheet: Timesheet, targets: MonthlyTargets): RemainingTargets => {
  let usedCore: number = 0;
  const usedProjects: ProjectMap = {};

  for (const project of timesheet.projects) {
    usedProjects[project.id] = 0;
  }

  for (const day of timesheet.days) {
    usedCore += day.coreHours ?? 0;

    const entries = Object.entries(day.projectHours) as [string, number][];
    for (const [projectId, value] of entries) {
      usedProjects[projectId] = (usedProjects[projectId] ?? 0) + value;
    }
  }

  const remainingProjects: ProjectMap = {};
  for (const projectId of Object.keys(targets.projects)) {
    remainingProjects[projectId] = Math.max(0, targets.projects[projectId] - (usedProjects[projectId] ?? 0));
  }

  return {
    core: Math.max(0, targets.core - usedCore),
    projects: remainingProjects,
  };
};

const allocateMonth = (timesheet: Timesheet, remaining: RemainingTargets, config: TimesheetGenerationConfig, editable: EditableCells): void => {
  const orderedDays = getDaysByPriority(timesheet.days);
  for (const day of orderedDays) {
    const allocation: Allocation = computeDayAllocation(day, remaining, timesheet, config);
    applyAllocation(day, allocation, remaining, timesheet, config, editable);
  }
};

// ==============================
// Day layer
// ==============================

const computeDayAllocation = (day: TimesheetDay, remaining: RemainingTargets, timesheet: Timesheet, config: TimesheetGenerationConfig): Allocation => {
  if (hasInterruption(day)) {
    return allocateFromInterruption(day, timesheet, config);
  }

  if (hasBusinessTripInterruption(day)) {
    return { core: 0, projects: {} };
  }

  const capacity: number = computeDayCapacity(day, config);
  const alreadyFilled: number = sumDayHours(day);
  const free: number = capacity - alreadyFilled;

  if (free <= 0) {
    return { core: 0, projects: {} };
  }

  const currentCore = day.coreHours ?? 0;
  const stagMinCore = computeStagHours(day);
  const coreMinNeed = Math.max(0, stagMinCore - currentCore);
  const corePreAllocated = Math.min(coreMinNeed, free, remaining.core);
  const freeAfterCoreMin = free - corePreAllocated;

  const weights: ProjectMap = buildDynamicWeights(remaining, timesheet);
  const distributed: ProjectMap = freeAfterCoreMin > 0 ? distribute(freeAfterCoreMin, weights) : {};

  return {
    core: corePreAllocated + (distributed.core ?? 0),
    projects: extractProjectDistribution(distributed, timesheet),
  };
};

// ==============================
// Rules
// ==============================

const hasInterruption = (day: TimesheetDay): boolean => {
  return day.attendance.interruptions !== "" && !dayHasBusinessTripInterruption(day);
};

const hasBusinessTripInterruption = dayHasBusinessTripInterruption;

const hasCoreOnlyInterruption = dayHasCoreOnlyInterruption;

const allocateFromInterruption = (day: TimesheetDay, timesheet: Timesheet, config: TimesheetGenerationConfig): Allocation => {
  const hours: number = Math.min(12, computeInterruptionHours(day, config));
  const totalCents = toCents(hours);

  if (hasCoreOnlyInterruption(day)) {
    return {
      core: fromCents(totalCents),
      projects: {},
    };
  }

  const projectsWorkloadSum = timesheet.projects.reduce((sum, p) => sum + normalizeWorkloadRatio(p.workload), 0);
  const coreWorkload = Math.max(0, normalizeWorkloadRatio(timesheet.totalWorkload) - projectsWorkloadSum);
  const totalWorkload = coreWorkload + projectsWorkloadSum;
  if (totalWorkload <= 0) {
    return { core: 0, projects: {} };
  }

  const projects: ProjectMap = {};
  let allocatedProjectCents = 0;
  for (const project of timesheet.projects) {
    if (project.lockedAt) continue;
    const ratio = normalizeWorkloadRatio(project.workload);
    const cents = Math.max(0, Math.round((totalCents * ratio) / totalWorkload));
    projects[project.id] = fromCents(cents);
    allocatedProjectCents += cents;
  }

  return {
    core: fromCents(Math.max(0, totalCents - allocatedProjectCents)),
    projects,
  };
};

const computeDayCapacity = (day: TimesheetDay, config: TimesheetGenerationConfig): number => {
  const attendanceHours: number = computeAttendanceHours(day);
  if (hasAnyAttendanceInput(day)) {
    // Pokud je docházka vyplněná, bereme ji jako zdroj pravdy.
    // Nevalidní/nekonzistentní docházka => 0 kapacita pro generování (bez fallbacku na 8h).
    return Math.min(12, Math.max(0, attendanceHours));
  }
  return Math.min(12, Math.max(0, config.defaultDailyWorkHours));
};

// ==============================
// Distribution
// ==============================

const buildDynamicWeights = (remaining: RemainingTargets, timesheet: Timesheet): ProjectMap => {
  const weights: ProjectMap = {
    core: Math.max(remaining.core, 0),
  };

  for (const [projectId, value] of Object.entries(remaining.projects)) {
    const project = timesheet.projects.find((p) => p.id === projectId);
    if (project?.lockedAt) {
      weights[projectId] = 0;
      continue;
    }
    weights[projectId] = Math.max(value, 0);
  }

  return weights;
};

const distribute = (remaining: number, weights: ProjectMap): ProjectMap => {
  const result: ProjectMap = {};
  const totalWeight: number = Object.values(weights).reduce((a: number, b: number) => a + b, 0);

  if (remaining <= 0 || totalWeight <= 0) {
    return result;
  }

  const targetCents = toCents(remaining);
  let allocatedCents = 0;
  const keys: string[] = Object.keys(weights);

  for (const [key, weight] of Object.entries(weights)) {
    const raw: number = (weight / totalWeight) * remaining;
    const preferred = roundPreferredDown(raw);
    const cents = Math.min(toCents(preferred), Math.max(0, targetCents - allocatedCents));
    result[key] = fromCents(cents);
    allocatedCents += cents;
  }

  let diffCents = targetCents - allocatedCents;
  if (diffCents <= 0 || keys.length === 0) {
    return result;
  }

  // Deterministic refill: prefer whole/half/0.05/0.01 increments.
  let guard = 0;
  while (diffCents > 0 && guard < 20000) {
    let progressed = false;
    for (const step of PREFERRED_STEPS_CENTS) {
      if (step > diffCents) continue;
      for (const key of keys) {
        if (step > diffCents) break;
        result[key] = fromCents(toCents(result[key] ?? 0) + step);
        diffCents -= step;
        progressed = true;
      }
    }
    if (!progressed) break;
    guard++;
  }

  return result;
};

// ==============================
// Apply
// ==============================

const applyAllocation = (day: TimesheetDay, allocation: Allocation, remaining: RemainingTargets, timesheet: Timesheet, config: TimesheetGenerationConfig, editable: EditableCells): void => {
  if (allocation.core > 0 && editable.coreByDate[day.date]) {
    const free = getDayFreeCapacity(day, config);
    const valueCents = Math.min(1200, Math.min(toCents(allocation.core), Math.min(toCents(remaining.core), toCents(free))));
    if (valueCents > 0) {
      day.coreHours = fromCents(toCents(day.coreHours ?? 0) + valueCents);
      remaining.core = fromCents(toCents(remaining.core) - valueCents);
    }
  }

  for (const [projectId, value] of Object.entries(allocation.projects)) {
    const project = timesheet.projects.find((p) => p.id === projectId);
    if (project?.lockedAt) {
      continue;
    }
    if (!editable.projectsByDate[day.date]?.[projectId]) continue;
    const free = getDayFreeCapacity(day, config);
    const appliedCents = Math.min(1200, Math.min(toCents(value), Math.min(toCents(remaining.projects[projectId] ?? 0), toCents(free))));
    if (appliedCents <= 0) continue;
    day.projectHours[projectId] = fromCents(toCents(day.projectHours[projectId] ?? 0) + appliedCents);
    remaining.projects[projectId] = fromCents(toCents(remaining.projects[projectId] ?? 0) - appliedCents);
  }
};

const reconcileMonthRemainders = (timesheet: Timesheet, remaining: RemainingTargets, config: TimesheetGenerationConfig, editable: EditableCells): void => {
  const orderedDays = getDaysByPriority(timesheet.days);
  // Core first.
  let coreLeft = toCents(remaining.core);
  if (coreLeft > 0) {
    for (const day of orderedDays) {
      if (coreLeft <= 0) break;
      if (!editable.coreByDate[day.date]) continue;
      const freeCents = toCents(getDayFreeCapacity(day, config));
      if (freeCents <= 0) continue;
      const add = Math.min(coreLeft, freeCents);
      if (add <= 0) continue;
      day.coreHours = fromCents(toCents(day.coreHours ?? 0) + add);
      coreLeft -= add;
    }
  }
  remaining.core = fromCents(coreLeft);

  // Then projects.
  for (const project of timesheet.projects) {
    if (project.lockedAt) continue;
    const projectId = project.id;
    let left = toCents(remaining.projects[projectId] ?? 0);
    if (left <= 0) continue;

    for (const day of orderedDays) {
      if (left <= 0) break;
      if (hasInterruption(day) || hasBusinessTripInterruption(day)) continue;
      if (!editable.projectsByDate[day.date]?.[projectId]) continue;

      const freeCents = toCents(getDayFreeCapacity(day, config));
      if (freeCents <= 0) continue;

      const add = Math.min(left, freeCents);
      if (add <= 0) continue;
      day.projectHours[projectId] = fromCents(toCents(day.projectHours[projectId] ?? 0) + add);
      left -= add;
    }
    remaining.projects[projectId] = fromCents(left);
  }
};

const enforceExactMonthlyTotals = (timesheet: Timesheet, targets: MonthlyTargets, config: TimesheetGenerationConfig, editable: EditableCells): void => {
  const orderedDays = getDaysByPriority(timesheet.days);

  const targetCore = toCents(targets.core);
  const currentCore = toCents(sumCoreHours(timesheet.days));
  if (currentCore < targetCore) {
    const missing = targetCore - currentCore;
    addCoreCents(orderedDays, missing, config, editable);
  } else if (currentCore > targetCore) {
    const excess = currentCore - targetCore;
    removeCoreCents(orderedDays, excess, editable);
  }

  for (const project of timesheet.projects) {
    if (project.lockedAt) continue;
    const projectId = project.id;
    const target = toCents(targets.projects[projectId] ?? 0);
    const current = toCents(sumProjectHours(timesheet.days, projectId));
    if (current < target) {
      const missing = target - current;
      addProjectCents(orderedDays, projectId, missing, config, editable);
    } else if (current > target) {
      const excess = current - target;
      removeProjectCents(orderedDays, projectId, excess, editable);
    }
  }

  // Final cent-level rebalance between core and projects (same-day transfers, day total unchanged).
  rebalanceProjectCoreSplits(timesheet, targets, editable);
};

const addCoreCents = (orderedDays: TimesheetDay[], cents: number, config: TimesheetGenerationConfig, editable: EditableCells): void => {
  let left = cents;
  for (const day of orderedDays) {
    if (left <= 0) break;
    if (!editable.coreByDate[day.date]) continue;
    const free = toCents(getDayFreeCapacity(day, config));
    if (free <= 0) continue;
    const add = Math.min(left, free);
    if (add <= 0) continue;
    day.coreHours = fromCents(toCents(day.coreHours ?? 0) + add);
    left -= add;
  }
};

const addProjectCents = (orderedDays: TimesheetDay[], projectId: string, cents: number, config: TimesheetGenerationConfig, editable: EditableCells): void => {
  let left = cents;
  for (const day of orderedDays) {
    if (left <= 0) break;
    if (hasInterruption(day) || hasBusinessTripInterruption(day)) continue;
    if (!editable.projectsByDate[day.date]?.[projectId]) continue;
    const free = toCents(getDayFreeCapacity(day, config));
    if (free <= 0) continue;
    const add = Math.min(left, free);
    if (add <= 0) continue;
    day.projectHours[projectId] = fromCents(toCents(day.projectHours[projectId] ?? 0) + add);
    left -= add;
  }
};

const removeCoreCents = (orderedDays: TimesheetDay[], cents: number, editable: EditableCells): void => {
  let left = cents;
  for (const day of [...orderedDays].reverse()) {
    if (left <= 0) break;
    if (!editable.coreByDate[day.date]) continue;
    const current = toCents(day.coreHours ?? 0);
    if (current <= 0) continue;
    const remove = Math.min(left, current);
    day.coreHours = fromCents(current - remove);
    left -= remove;
  }
};

const removeProjectCents = (orderedDays: TimesheetDay[], projectId: string, cents: number, editable: EditableCells): void => {
  let left = cents;
  for (const day of [...orderedDays].reverse()) {
    if (left <= 0) break;
    if (!editable.projectsByDate[day.date]?.[projectId]) continue;
    const current = toCents(day.projectHours[projectId] ?? 0);
    if (current <= 0) continue;
    const remove = Math.min(left, current);
    day.projectHours[projectId] = fromCents(current - remove);
    left -= remove;
  }
};

const rebalanceProjectCoreSplits = (timesheet: Timesheet, targets: MonthlyTargets, editable: EditableCells): void => {
  const orderedDays = getDaysByPriority(timesheet.days);

  for (const project of timesheet.projects) {
    if (project.lockedAt) continue;
    const projectId = project.id;
    let diff = toCents(targets.projects[projectId] ?? 0) - toCents(sumProjectHours(timesheet.days, projectId));
    if (diff === 0) continue;

    if (diff > 0) {
      // Need more project hours: transfer from core -> project in same day.
      for (const day of orderedDays) {
        if (diff <= 0) break;
        if (!editable.coreByDate[day.date] || !editable.projectsByDate[day.date]?.[projectId]) continue;
        if (hasInterruption(day) || hasBusinessTripInterruption(day)) continue;
        const core = toCents(day.coreHours ?? 0);
        const proj = toCents(day.projectHours[projectId] ?? 0);
        const projHeadroom = Math.max(0, 1200 - proj);
        const move = Math.min(diff, core, projHeadroom);
        if (move <= 0) continue;
        day.coreHours = fromCents(core - move);
        day.projectHours[projectId] = fromCents(proj + move);
        diff -= move;
      }
    } else {
      // Need less project hours: transfer from project -> core in same day.
      diff = Math.abs(diff);
      for (const day of [...orderedDays].reverse()) {
        if (diff <= 0) break;
        if (!editable.coreByDate[day.date] || !editable.projectsByDate[day.date]?.[projectId]) continue;
        const core = toCents(day.coreHours ?? 0);
        const proj = toCents(day.projectHours[projectId] ?? 0);
        const coreHeadroom = Math.max(0, 1200 - core);
        const move = Math.min(diff, proj, coreHeadroom);
        if (move <= 0) continue;
        day.projectHours[projectId] = fromCents(proj - move);
        day.coreHours = fromCents(core + move);
        diff -= move;
      }
    }
  }
};

// ==============================
// Helpers
// ==============================

const sumDayHours = (day: TimesheetDay): number => {
  let sum: number = day.coreHours ?? 0;

  for (const value of Object.values(day.projectHours)) {
    sum += value;
  }

  return sum;
};

const extractProjectDistribution = (distribution: ProjectMap, timesheet: Timesheet): ProjectMap => {
  const result: ProjectMap = {};

  for (const project of timesheet.projects) {
    result[project.id] = distribution[project.id] ?? 0;
  }

  return result;
};

const computeTotalMonthlyHours = (timesheet: Timesheet, config: TimesheetGenerationConfig): number => {
  const workloadRatio = normalizeWorkloadRatio(timesheet.totalWorkload);
  const workableDays = timesheet.days.filter((day) => !day.isWeekend && !day.isHoliday).length;
  return workableDays * config.defaultDailyWorkHours * workloadRatio;
};

const computeAttendanceHours = (day: TimesheetDay): number => {
  if (day.attendance.clockIn === "" || day.attendance.clockOut === "") {
    return 0;
  }

  const clockIn = toMinutes(day.attendance.clockIn);
  const clockOut = toMinutes(day.attendance.clockOut);

  let actualClockOut = clockOut;
  if (clockOut < clockIn) {
    actualClockOut = clockOut + 24 * 60;
  }

  // Dlouhé směny typu 08:00 -> 07:00 (23h) jsou nevalidní vstup, ne kandidát pro autogeneraci.
  if (actualClockOut - clockIn > 12 * 60) {
    return 0;
  }

  if (actualClockOut <= clockIn) {
    return 0;
  }

  const workedMinutes = actualClockOut - clockIn;

  let breakMinutes = 0;
  if (day.attendance.breakStart && day.attendance.breakEnd) {
    let breakStart = toMinutes(day.attendance.breakStart);
    let breakEnd = toMinutes(day.attendance.breakEnd);
    if (breakEnd < breakStart) {
      breakEnd += 24 * 60;
    }
    if (breakStart < clockIn) {
      breakStart += 24 * 60;
      breakEnd += 24 * 60;
    }
    breakMinutes = Math.max(0, breakEnd - breakStart);
  }

  return Math.max(0, (workedMinutes - breakMinutes) / 60);
};

const hasAnyAttendanceInput = (day: TimesheetDay): boolean => {
  return Boolean(day.attendance.clockIn || day.attendance.clockOut);
};

const computeInterruptionHours = (day: TimesheetDay, config: TimesheetGenerationConfig): number => {
  const attendanceHours: number = computeAttendanceHours(day);

  if (attendanceHours > 0) {
    return attendanceHours;
  }

  return config.defaultDailyWorkHours;
};

const normalizeWorkloadRatio = (value: number): number => {
  if (!Number.isFinite(value)) return 0;
  if (value > 1) return value / 100;
  if (value < 0) return 0;
  return value;
};

const getDayFreeCapacity = (day: TimesheetDay, config: TimesheetGenerationConfig): number => {
  const capacity = computeDayCapacity(day, config);
  const used = sumDayHours(day);
  return Math.max(0, capacity - used);
};

const getDaysByPriority = (days: TimesheetDay[]): TimesheetDay[] => {
  const interruption: TimesheetDay[] = [];
  const withAttendance: TimesheetDay[] = [];
  const others: TimesheetDay[] = [];

  for (const day of days) {
    if (hasInterruption(day)) {
      interruption.push(day);
      continue;
    }
    const hasAttendance = computeAttendanceHours(day) > 0;
    if (hasAttendance) {
      withAttendance.push(day);
      continue;
    }
    others.push(day);
  }

  return [...interruption, ...withAttendance, ...others];
};

const sumCoreHours = (days: TimesheetDay[]): number => {
  return days.reduce((sum, day) => sum + (day.coreHours ?? 0), 0);
};

const sumProjectHours = (days: TimesheetDay[], projectId: string): number => {
  return days.reduce((sum, day) => sum + (day.projectHours[projectId] ?? 0), 0);
};

const toCents = (value: number): number => Math.max(0, Math.round(value * 100));
const fromCents = (value: number): number => Number((Math.max(0, value) / 100).toFixed(2));

const roundPreferredDown = (value: number): number => {
  const cents = toCents(value);
  const whole = Math.floor(cents / 100) * 100;
  if (whole > 0) return fromCents(whole);
  const half = Math.floor(cents / 50) * 50;
  if (half > 0) return fromCents(half);
  const five = Math.floor(cents / 5) * 5;
  if (five > 0) return fromCents(five);
  return fromCents(cents);
};

const computeStagHours = (day: TimesheetDay): number => {
  if (!day.attendance.schedules?.length) return 0;
  let minutes = 0;
  for (const s of day.attendance.schedules) {
    if (!s.start || !s.end) continue;
    const start = toMinutes(s.start);
    const end = toMinutes(s.end);
    if (end > start) minutes += end - start;
  }
  return Math.min(12, minutes / 60);
};

const captureEditableCells = (timesheet: Timesheet): EditableCells => {
  const coreByDate: Record<string, boolean> = {};
  const projectsByDate: Record<string, Record<string, boolean>> = {};

  for (const day of timesheet.days) {
    coreByDate[day.date] = (day.coreHours ?? 0) <= 0;
    const perProject: Record<string, boolean> = {};
    for (const project of timesheet.projects) {
      perProject[project.id] = (day.projectHours[project.id] ?? 0) <= 0;
    }
    projectsByDate[day.date] = perProject;
  }

  return { coreByDate, projectsByDate };
};
