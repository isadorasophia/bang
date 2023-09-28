using Bang.Generator.Metadata;
using System.Collections.Immutable;

namespace Bang.Generator.Templating;

public sealed class FileTemplate
{
    private readonly string _templateText;
    private readonly ImmutableArray<TemplateSubstitution> _substitutions;

    public string FileName { get; }

    public FileTemplate(
        string fileName,
        string templateText,
        ImmutableArray<TemplateSubstitution> substitutions
    )
    {
        FileName = fileName;
        this._substitutions = substitutions;
        this._templateText = templateText;
    }

    public void Process(TypeMetadata metadata)
    {
        foreach (var substitution in _substitutions)
        {
            substitution.Process(metadata);
        }
    }

    public string GetDocumentWithReplacements() => _substitutions.Aggregate(
        _templateText,
        (text, substitution) => text.Replace(
            substitution.StringToReplaceInTemplate,
            substitution.GetProcessedText()
        )
    );
}

internal sealed class ProjectPrefixSubstitution : TemplateSubstitution
{
    public ProjectPrefixSubstitution() : base("<project_prefix>") { }

    protected override string ProcessProject(TypeMetadata.Project project)
        => project.ProjectName;
}