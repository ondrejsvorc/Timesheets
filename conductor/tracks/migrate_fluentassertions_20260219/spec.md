# Specification - Replace FluentAssertions with xUnit Assert

## Overview
This track involves refactoring the test suite in the `backend/Timesheets.Api.Tests/` project to remove the dependency on `FluentAssertions` and instead use the native xUnit `Assert` library. This ensures a more lightweight test project and adheres to standard xUnit patterns.

## Goals
- Remove `FluentAssertions` dependency from the `Timesheets.Api.Tests` project.
- Migrate all existing assertions from `FluentAssertions` syntax to xUnit `Assert` syntax.
- Maintain test coverage and reliability during the migration.

## Functional Requirements
- **Migration:** All test files in `backend/Timesheets.Api.Tests/` must be updated.
- **Assertion Style:** 
    - Use standard xUnit `Assert` methods (e.g., `Assert.Equal`, `Assert.True`, `Assert.NotNull`).
    - Use `Assert.Multiple` to group related assertions where it improves readability or replicates grouped FluentAssertions behavior.
- **Cleanup:** 
    - Remove all `using FluentAssertions;` directives.
    - Uninstall the `FluentAssertions` NuGet package from the `Timesheets.Api.Tests` project.

## Non-Functional Requirements
- **Consistency:** Ensure consistent use of xUnit assertion patterns across all migrated files.
- **Performance:** Removing a library dependency should slightly reduce test project initialization time.

## Acceptance Criteria
- [ ] All tests in `backend/Timesheets.Api.Tests/` pass after the migration.
- [ ] No references to `FluentAssertions` remain in the code.
- [ ] The `Timesheets.Api.Tests.csproj` file no longer includes the `FluentAssertions` package.
- [ ] `Assert.Multiple` is used appropriately for grouped assertions.

## Out of Scope
- Rewriting the logic of the tests themselves (only the assertions are being changed).
- Migrating other test projects (if any) not specified in the `backend/Timesheets.Api.Tests/` directory.
