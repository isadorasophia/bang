using Bang.Generator.Metadata;
using System.Text;

namespace Bang.Generator.Templating;

/// <summary>
/// Aggregates data from all components and messages and turns it into a string that gets replaced into a <see cref="FileTemplate"/>
/// </summary>
public abstract class TemplateSubstitution
{
    private readonly StringBuilder _aggregatedText = new();

    protected string ProjectPrefix = "";
    protected string ParentProjectPrefix = "";

    /// <summary>
    /// String this substitution will replace in a <see cref="FileTemplate"/>
    /// </summary>
    public string StringToReplaceInTemplate { get; }

    protected TemplateSubstitution(string stringToReplaceInTemplate)
    {
        StringToReplaceInTemplate = stringToReplaceInTemplate;
    }

    public void Process(TypeMetadata metadata)
    {
        var result = metadata switch
        {
            TypeMetadata.Project project => SaveAndProcessProject(project),
            TypeMetadata.Message message => ProcessMessage(message),
            TypeMetadata.Component component => ProcessComponent(component),
            TypeMetadata.Interaction interaction => ProcessInteraction(interaction),
            TypeMetadata.StateMachine stateMachine => ProcessStateMachine(stateMachine),
            _ => throw new InvalidOperationException()
        };

        if (result is not null)
        {
            _aggregatedText.Append(result);
        }
    }

    private string? SaveAndProcessProject(TypeMetadata.Project project)
    {
        ProjectPrefix = project.ProjectName;
        ParentProjectPrefix = project.ParentProjectName ?? "";
        return ProcessProject(project);
    }

    protected virtual string? ProcessProject(TypeMetadata.Project project) => null;
    protected virtual string? ProcessMessage(TypeMetadata.Message metadata) => null;
    protected virtual string? ProcessComponent(TypeMetadata.Component metadata) => null;
    protected virtual string? ProcessInteraction(TypeMetadata.Interaction metadata) => null;
    protected virtual string? ProcessStateMachine(TypeMetadata.StateMachine metadata) => null;
    protected virtual string? FinalModification() => null;
    public string GetProcessedText()
    {
        var finalModification = FinalModification();
        if (finalModification is not null)
        {
            _aggregatedText.Append(finalModification);
        }

        return _aggregatedText.ToString();
    }
}
