using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Base class for solution-wide code refactoring tests that supports multi-document
///     and multi-project scenarios.
/// </summary>
/// <typeparam name="TRefactoring">
///     The code refactoring provider type to test.
///     Must have a parameterless constructor.
/// </typeparam>
/// <remarks>
///     <para>
///         Use this class when testing refactorings that operate across multiple documents
///         or projects, such as "Fix all in file/project/solution" scenarios.
///     </para>
///     <para>
///         For simple single-document refactoring tests, use <see cref="RefactoringTest{TRefactoring}" /> instead.
///     </para>
/// </remarks>
/// <example>
///     <para>Testing a solution-wide refactoring:</para>
///     <code>
/// public class MyRefactoringTests : SolutionRefactoringTest&lt;MyRefactoring&gt;
/// {
///     [Fact]
///     public async Task FixesAllInSolution()
///     {
///         await VerifyMultiProjectAsync(
///             projects: [
///                 ("Project1", [("File1.cs", source1)]),
///                 ("Project2", [("File2.cs", source2)])
///             ],
///             expected: [
///                 ("Project1", [("File1.cs", fixed1)]),
///                 ("Project2", [("File2.cs", fixed2)])
///             ],
///             triggerFile: "File1.cs",
///             triggerText: "x => x * 2",
///             refactoringTitle: "Make all lambdas static in solution");
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="RefactoringTest{TRefactoring}" />
public abstract class SolutionRefactoringTest<TRefactoring> : IDisposable
    where TRefactoring : CodeRefactoringProvider, new()
{
    private static readonly ImmutableArray<MetadataReference> References =
        Net100.References.All.CastArray<MetadataReference>();

    private readonly AdhocWorkspace _workspace = new();

    /// <summary>
    ///     Disposes the underlying workspace.
    /// </summary>
    public void Dispose()
    {
        _workspace.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Verifies a refactoring across multiple documents in a single project.
    /// </summary>
    /// <param name="documents">
    ///     The source documents as tuples of (fileName, content).
    /// </param>
    /// <param name="expected">
    ///     The expected documents after refactoring as tuples of (fileName, content).
    /// </param>
    /// <param name="triggerFile">
    ///     The file name where the refactoring is triggered.
    /// </param>
    /// <param name="triggerText">
    ///     The text to find in the trigger file to determine the refactoring span.
    /// </param>
    /// <param name="refactoringTitle">
    ///     The title of the refactoring action to apply.
    /// </param>
    /// <param name="cancellationToken">
    ///     Optional cancellation token.
    /// </param>
    protected async Task VerifyMultiDocumentAsync(
        IReadOnlyList<(string fileName, string content)> documents,
        IReadOnlyList<(string fileName, string content)> expected,
        string triggerFile,
        string triggerText,
        string refactoringTitle,
        CancellationToken cancellationToken = default)
    {
        var solution = CreateSolution(documents);
        var triggerDoc = solution.Projects.First().Documents.First(d => d.Name == triggerFile);
        var triggerDocText = await triggerDoc.GetTextAsync(cancellationToken);
        var span = GetSpan(triggerDocText, triggerText);

        var actions = await GetRefactoringsAsync(triggerDoc, span, cancellationToken);

        var matchingAction = actions.FirstOrDefault(a => a.Title == refactoringTitle)
            ?? throw new InvalidOperationException(
                $"Expected refactoring '{refactoringTitle}' not found. Available: {string.Join(", ", actions.Select(a => $"'{a.Title}'"))}");

        var changedSolution = await ApplySolutionCodeActionAsync(matchingAction, cancellationToken);

        foreach (var (fileName, expectedContent) in expected)
        {
            var changedDoc = changedSolution.Projects.First().Documents.First(d => d.Name == fileName);
            var changedText = await changedDoc.GetTextAsync(cancellationToken);
            var actual = changedText.ToString();
            var normalizedExpected = expectedContent.ReplaceLineEndings();

            if (actual != normalizedExpected)
            {
                throw new InvalidOperationException(
                    $"Document '{fileName}' did not match expected content.\n\nExpected:\n{normalizedExpected}\n\nActual:\n{actual}");
            }
        }
    }

    /// <summary>
    ///     Verifies a refactoring across multiple projects.
    /// </summary>
    /// <param name="projects">
    ///     The source projects as tuples of (projectName, documents) where documents is a list of (fileName, content).
    /// </param>
    /// <param name="expected">
    ///     The expected projects after refactoring.
    /// </param>
    /// <param name="triggerProject">
    ///     The project name where the refactoring is triggered.
    /// </param>
    /// <param name="triggerFile">
    ///     The file name where the refactoring is triggered.
    /// </param>
    /// <param name="triggerText">
    ///     The text to find in the trigger file to determine the refactoring span.
    /// </param>
    /// <param name="refactoringTitle">
    ///     The title of the refactoring action to apply.
    /// </param>
    /// <param name="cancellationToken">
    ///     Optional cancellation token.
    /// </param>
    protected async Task VerifyMultiProjectAsync(
        IReadOnlyList<(string projectName, IReadOnlyList<(string fileName, string content)> documents)> projects,
        IReadOnlyList<(string projectName, IReadOnlyList<(string fileName, string content)> documents)> expected,
        string triggerProject,
        string triggerFile,
        string triggerText,
        string refactoringTitle,
        CancellationToken cancellationToken = default)
    {
        var solution = CreateMultiProjectSolution(projects);
        var triggerProj = solution.Projects.First(p => p.Name == triggerProject);
        var triggerDoc = triggerProj.Documents.First(d => d.Name == triggerFile);
        var triggerDocText = await triggerDoc.GetTextAsync(cancellationToken);
        var span = GetSpan(triggerDocText, triggerText);

        var actions = await GetRefactoringsAsync(triggerDoc, span, cancellationToken);

        var matchingAction = actions.FirstOrDefault(a => a.Title == refactoringTitle)
            ?? throw new InvalidOperationException(
                $"Expected refactoring '{refactoringTitle}' not found. Available: {string.Join(", ", actions.Select(a => $"'{a.Title}'"))}");

        var changedSolution = await ApplySolutionCodeActionAsync(matchingAction, cancellationToken);

        foreach (var (projectName, expectedDocs) in expected)
        {
            var changedProj = changedSolution.Projects.First(p => p.Name == projectName);
            foreach (var (fileName, expectedContent) in expectedDocs)
            {
                var changedDoc = changedProj.Documents.First(d => d.Name == fileName);
                var changedText = await changedDoc.GetTextAsync(cancellationToken);
                var actual = changedText.ToString();
                var normalizedExpected = expectedContent.ReplaceLineEndings();

                if (actual != normalizedExpected)
                {
                    throw new InvalidOperationException(
                        $"Document '{projectName}/{fileName}' did not match expected content.\n\nExpected:\n{normalizedExpected}\n\nActual:\n{actual}");
                }
            }
        }
    }

    /// <summary>
    ///     Gets the available refactorings for a multi-document scenario.
    /// </summary>
    protected async Task<ImmutableArray<CodeAction>> GetRefactoringsForDocumentAsync(
        IReadOnlyList<(string fileName, string content)> documents,
        string triggerFile,
        string triggerText,
        CancellationToken cancellationToken = default)
    {
        var solution = CreateSolution(documents);
        var triggerDoc = solution.Projects.First().Documents.First(d => d.Name == triggerFile);
        var triggerDocText = await triggerDoc.GetTextAsync(cancellationToken);
        var span = GetSpan(triggerDocText, triggerText);

        return await GetRefactoringsAsync(triggerDoc, span, cancellationToken);
    }

    /// <summary>
    ///     Verifies that no refactoring is offered for the given scenario.
    /// </summary>
    protected async Task VerifyNoRefactoringAsync(
        IReadOnlyList<(string fileName, string content)> documents,
        string triggerFile,
        string triggerText,
        CancellationToken cancellationToken = default)
    {
        var actions = await GetRefactoringsForDocumentAsync(documents, triggerFile, triggerText, cancellationToken);

        if (actions.Length > 0)
        {
            throw new InvalidOperationException(
                $"Expected no refactorings but found: {string.Join(", ", actions.Select(a => $"'{a.Title}'"))}");
        }
    }

    private Solution CreateSolution(IReadOnlyList<(string name, string source)> documents)
    {
        var project = _workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(new CSharpParseOptions(TestConfiguration.LanguageVersion))
            .WithMetadataReferences(References);

        var solution = project.Solution;
        foreach (var (name, source) in documents)
        {
            var docId = DocumentId.CreateNewId(project.Id, name);
            solution = solution.AddDocument(docId, name, SourceText.From(source.ReplaceLineEndings()));
        }

        return solution;
    }

    private Solution CreateMultiProjectSolution(
        IReadOnlyList<(string projectName, IReadOnlyList<(string fileName, string content)> documents)> projects)
    {
        var solution = _workspace.CurrentSolution;

        foreach (var (projectName, documents) in projects)
        {
            var projectId = ProjectId.CreateNewId(projectName);
            solution = solution.AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithProjectParseOptions(projectId, new CSharpParseOptions(TestConfiguration.LanguageVersion))
                .WithProjectMetadataReferences(projectId, References);

            foreach (var (fileName, content) in documents)
            {
                var docId = DocumentId.CreateNewId(projectId, fileName);
                solution = solution.AddDocument(docId, fileName, SourceText.From(content.ReplaceLineEndings()));
            }
        }

        return solution;
    }

    private static TextSpan GetSpan(SourceText text, string textToFind)
    {
        var start = text.ToString().IndexOf(textToFind, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new ArgumentException($"Text '{textToFind}' not found in document");
        }

        return new TextSpan(start, textToFind.Length);
    }

    private static async Task<ImmutableArray<CodeAction>> GetRefactoringsAsync(
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var provider = new TRefactoring();
        var actions = new List<CodeAction>();

        var context = new CodeRefactoringContext(
            document,
            span,
            actions.Add,
            cancellationToken);

        await provider.ComputeRefactoringsAsync(context);

        return [.. actions];
    }

    private static async Task<Solution> ApplySolutionCodeActionAsync(
        CodeAction action,
        CancellationToken cancellationToken)
    {
        var operations = await action.GetOperationsAsync(cancellationToken);
        var applyChanges = operations.OfType<ApplyChangesOperation>().First();
        return applyChanges.ChangedSolution;
    }
}
