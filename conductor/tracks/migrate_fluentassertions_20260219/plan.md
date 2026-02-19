# Implementation Plan - Replace FluentAssertions with xUnit Assert

This plan outlines the steps to migrate the `backend/Timesheets.Api.Tests/` project from `FluentAssertions` to native xUnit `Assert` methods, including the cleanup of dependencies.

## Phase 1: Project Baseline & Initial Setup
**Goal:** Confirm the current state of the test project and prepare for the migration.

- [x] Task: Verify that all current tests in `backend/Timesheets.Api.Tests/` pass before making any changes. e180304
    - [ ] Run `dotnet test backend/Timesheets.Api.Tests/Timesheets.Api.Tests.csproj` and ensure all tests pass.
- [ ] Task: Conductor - User Manual Verification 'Phase 1: Project Baseline & Initial Setup' (Protocol in workflow.md)

## Phase 2: Migrate AttendanceTimesheetReaderTests
**Goal:** Refactor `AttendanceTimesheetReaderTests.cs` to use xUnit Assert.

- [x] Task: Refactor assertions in `AttendanceTimesheetReaderTests.cs` (Red Phase - Migration). d8bc0f6
    - [x] Replace `using FluentAssertions;` with `using Xunit;` (if not already present).
    - [x] Migrate `FluentAssertions` syntax (e.g., `.Should().Be(...)`) to xUnit `Assert` methods (e.g., `Assert.Equal(...)`).
    - [x] Utilize `Assert.Multiple` to group related assertions where appropriate.
- [x] Task: Verify migration in `AttendanceTimesheetReaderTests.cs` (Green Phase - Passing). d8bc0f6
    - [x] Run tests for `AttendanceTimesheetReaderTests.cs` and confirm they all pass.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Migrate AttendanceTimesheetReaderTests' (Protocol in workflow.md)

## Phase 3: Migrate CellParserTests
**Goal:** Refactor `CellParserTests.cs` to use xUnit Assert.

- [ ] Task: Refactor assertions in `CellParserTests.cs` (Red Phase - Migration).
    - [ ] Replace `using FluentAssertions;` with `using Xunit;` (if not already present).
    - [ ] Migrate `FluentAssertions` syntax to xUnit `Assert` methods.
    - [ ] Utilize `Assert.Multiple` to group related assertions where appropriate.
- [ ] Task: Verify migration in `CellParserTests.cs` (Green Phase - Passing).
    - [ ] Run tests for `CellParserTests.cs` and confirm they all pass.
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Migrate CellParserTests' (Protocol in workflow.md)

## Phase 4: Dependency Cleanup & Final Verification
**Goal:** Remove the FluentAssertions library and perform a final project-wide check.

- [ ] Task: Uninstall the `FluentAssertions` NuGet package.
    - [ ] Remove the `FluentAssertions` reference from `backend/Timesheets.Api.Tests/Timesheets.Api.Tests.csproj`.
- [ ] Task: Perform a clean build and final test run.
    - [ ] Run `dotnet clean backend/Timesheets.Api.Tests/Timesheets.Api.Tests.csproj`.
    - [ ] Run `dotnet build backend/Timesheets.Api.Tests/Timesheets.Api.Tests.csproj`.
    - [ ] Run `dotnet test backend/Timesheets.Api.Tests/Timesheets.Api.Tests.csproj` and confirm all tests pass.
- [ ] Task: Conductor - User Manual Verification 'Phase 4: Dependency Cleanup & Final Verification' (Protocol in workflow.md)
