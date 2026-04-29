# Architectural Principles & Philosophy

## 1. Locality over Layering (Feature-First)
- **Grouping by Feature**: Prefer organizing code by functional modules/features, not technical layers (no generic `controllers/`, `services/` folders).
- **Proximity**: Keep related things together. If two things change together, they should live together.
- **Single File Policy**: If a feature or logic is small, keep everything (e.g., Request, Response, Logic) in a single file to maintain full context. Only split into multiple files when the file exceeds 200-300 lines.
- **Flat Hierarchy**: Keep folder nesting to a maximum of 2-3 levels. Avoid "folder soup".

## 2. Pragmatic Implementation (Anti-Overengineering)
- **Direct DB Access**: Avoid unnecessary abstractions like the Repository Pattern if an ORM (e.g., EF Core) is used. Calling DB logic alongside business logic in a single function is acceptable for simplicity.
- **No God Classes**: Strictly forbid classes like `UserService` or `GeneralManager`. Use small, focused functions or specialized handlers.
- **Composition over Inheritance**: Always prefer composition. Inheritance should be the absolute last resort.

## 3. Logic Flow & Readability
- **Early Return Principle**: Always return as early as possible to keep the main logic flow at the lowest indentation level. Avoid nested `if/else` blocks.
- **Single Way of Doing Things**: Eliminate redundancy. If multiple methods achieve the same goal, refactor to a single, clear path.
- **Strategy Pattern**: Use the Strategy Pattern or polymorphism to replace complex conditional branching.
- **Open-Closed Principle**: Design systems that are open for extension but closed for modification to support software evolution without breaking existing logic.

## 4. Responsibility
- **No Responsibility Bleeding**: Each function/module must have a clear responsibility. Do not mix unrelated domains in a single execution flow.
- **Human-Centric Code**: Readability for humans is the highest priority. If a pattern makes the code harder to follow, discard it.

## 5. Functional & Immutable Approach
- **Favor Immutability**: Data should be immutable by default. Instead of modifying an object, return a new instance with the changes.
- **Pure Functions**: Aim for functions without side effects. Input goes in, output comes out. Business logic should not depend on global state.
- **Interfaces as Contracts**: Use interfaces to define behaviors, but implement them using simple functions or "data-in, data-out" handlers.
- **State Separation**: Keep data (DTOs, Records, Entities) separate from behavior (Logic, Processors). Avoid mixing them into "Fat Models".