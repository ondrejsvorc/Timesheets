export const UserRole = {
  Employee: 0,
  ContractManager: 1,
  ProjectManager: 2,
  GlobalManager: 3,
  Admin: 4,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const isAtLeast = (role: UserRole, minRole: UserRole): boolean => role >= minRole;
