# Testing reference

- Test framework: xUnit 2.5.3.
- Mocking library: NSubstitute 5.1.0.
- Use Arrange, Act, Assert and the existing test naming style.
- Reuse PatronFactory and LoanFactory before adding new helpers.
- Verify successful calls with Received(1) and rejected calls with DidNotReceive().
- For rejected operations, verify both the result status and that input collections/entities remain unchanged.
- Cover normal success, rejection, boundary values, persistence failures, and no-side-effect paths.

Run these commands from the repository root:

~~~bash
dotnet build
dotnet test
git diff --check
~~~

Do not treat a generated test summary or an agent statement as proof unless the command was actually executed in the current working tree.
