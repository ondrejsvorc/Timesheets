# Coding Style & Explicit Programming

## 1. Explicit over Implicit
- **Code is Documentation**: Code must be self-documenting. Use clear, transparent names for functions, methods, and classes.
- **No Redundant Comments**: Do not write comments that restate what the code is doing (e.g., no `// creates a user` above `createUser()`). Only use comments to explain "why" a non-obvious business decision was made.
- **Be Explicit**: Avoid "magic" behavior. Prefer explicit logic that can be traced easily over clever, implicit "under-the-hood" solutions.

## 2. Type System & Variables
- **Strict Typing**: Use explicit types for all function signatures, class members, and complex variables.
- **Avoid `var` / Implicit types**: 
    - In C#: Never use `var` unless the type is explicitly clear from the right-hand side (e.g., `new User()`). If in doubt, use the explicit type name.
    - In TypeScript: Use `const` and `let`. Avoid `any`.
- **Selective Inference**: You may omit the type only if it's 100% obvious to a human without an IDE (e.g., `string name = "John"`). If the type is not immediately clear, write it out.

## 3. Naming Conventions (The "Senior Balance")
- **No Abbreviations**: Use `request` instead of `req`, `response` instead of `res`, `error` instead of `err`.
- **Contextual Conciseness**: Do not repeat the parent context in property names.
    - **WRONG**: `User.UserName`, `User.UserSurname`.
    - **RIGHT**: `User.Name`, `User.Surname`.
- **Word Count Limits**:
    - **1-2 words**: Ideal.
    - **3 words**: Acceptable.
    - **4+ words**: Prohibited (refactor unless absolutely unavoidable).
- **Domain Value**: Names should reflect business value (e.g., `TimesheetValidator`, `UserImporter`).

## 4. Modern Syntax vs. Readability
- **Readability First**: Use modern language features only if they improve clarity. Do not use "syntactic sugar" for the sake of being modern if it makes the code harder to read for others.
- **Explicit Loops**: Prefer readable language structures (like `foreach`) over complex functional chains (like `.Reduce().Map().Filter()`) if the loop is easier to reason about.
- **Clarity over "Magic"**: A slightly longer, explicit code block is always better than a short, "magical" one-liner that is difficult to debug or understand at a glance.

## 5. Functional Implementation
- **Prefer Records/Readonly**: In C#, use `record` instead of `class` for data carriers. In TypeScript, use `readonly` properties.
- **Avoid Global State**: Do not use static variables for storing state. Pass all dependencies explicitly via parameters or constructors.
- **Expression-bodied Members**: Use short-form syntax for simple functions, but only if it stays readable.

## 6. Brackets & Structural Consistency
- **Always Use Brackets**: For control structures (`if`, `else`, `for`, `while`, `switch`), always use curly braces `{ }`, even for single-line statements. 
    - **WRONG**: `if (user.IsActive) return true;`
    - **RIGHT**: 
      ```csharp
      if (user.IsActive) 
      {
          return true; 
      }
      ```
- **Arrow Functions Exception**: For short, concise arrow functions (especially in functional chains or simple callbacks), omitting brackets is encouraged if it improves clarity and the logic is a single expression.
- **Language Idioms**: Follow the most prevalent and preferred formatting style of the specific language (e.g., K&R style for C# with braces on new lines, or Egyptian braces for TypeScript), but never at the expense of the "Always Use Brackets" rule for logic.