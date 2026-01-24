# Preflight Check Skill

This skill provides a sanity check for Claude Code sessions before commits and PRs through three verification phases.

**When to Use:** After completing features, significant changes, refactoring, or before creating pull requests.

**The Three Phases:**

1. **Build Verification** - Compiles the solution and catches type/syntax errors (stops if it fails)
2. **Format Check** - Verifies code formatting meets project standards
3. **Test Suite** - Runs all unit tests with the xUnit test framework

**Allowed Tools:**
- `dotnet build *` (Build and type checking)
- `dotnet format *` (Code formatting verification)
- `dotnet test *` (Unit testing)

**Execution Steps:**

1. Run `dotnet build WebAPI.sln` - If this fails, stop and report errors
2. Run `dotnet format WebAPI.sln --verify-no-changes` - Report any formatting issues
3. Run `dotnet test WebAPI.sln` - Capture test results and counts

**Output Requirements:**

Generate a structured preflight report in this format:

```
## Preflight Report

| Check  | Status      |
|--------|-------------|
| Build  | PASS / FAIL |
| Format | PASS / FAIL |
| Tests  | PASS / FAIL |

### Metrics
- **Build Errors:** [count]
- **Format Issues:** [count]
- **Tests Passed:** [passed]/[total]
- **Tests Failed:** [count]

### Issues (if any)
- [List specific errors or failures that need attention]

### Status: READY / NOT READY for PR
```

**Failure Handling:**

- **Build fails:** Stop immediately, report errors, status is NOT READY
- **Format issues:** Continue to tests, list files needing formatting
- **Test failures:** Report failed test names and failure messages

This quality gate ensures code compiles, follows formatting standards, and passes all tests before integration.
