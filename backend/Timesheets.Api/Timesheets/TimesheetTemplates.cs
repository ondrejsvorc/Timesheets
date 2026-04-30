using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Timesheets.Api.Timesheets;

public sealed record TimesheetTemplate(Metadata Metadata, IReadOnlyList<Cell> Cells, IReadOnlyList<Range> Ranges);
public sealed record Metadata(TemplateType Type, DateTime CreatedAt, DateTime? DeprecatedAt);
public enum TemplateType { Attendance, Project }
public sealed record Cell(
    string Key,
    string Field,
    string InputDatatype,
    string OutputDatatype,
    string InputExample,
    string OutputExample,
    string Pattern,
    bool IsRequired,
    object? DefaultValue = null
);
public sealed record Range(
    string Start,
    string End,
    string Field,
    string InputDatatype,
    string OutputDatatype,
    string InputExample,
    string OutputExample,
    string Pattern,
    bool IsRequired
);

public interface ITimesheetTemplateLoader
{
    public Task<TimesheetTemplate> LoadAsync(string file);
    public Task<IEnumerable<TimesheetTemplate>> LoadAsync();
}

public class TimesheetTemplateLoader : ITimesheetTemplateLoader
{
    private readonly string _directory;
    private readonly JsonSerializerOptions _options = new() { Converters = { new JsonStringEnumConverter() } };

    public TimesheetTemplateLoader(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Templates directory not found: {_directory}");
        }
        _directory = directory;
    }

    public async Task<TimesheetTemplate> LoadAsync(string file)
    {
        await using FileStream stream = File.OpenRead(Path.Combine(_directory, file));
        TimesheetTemplate? template = await JsonSerializer.DeserializeAsync<TimesheetTemplate>(stream, _options);
        if (template is null)
        {
            throw new InvalidDataException($"Invalid template file: {file}");
        }

        TimesheetTemplateValidator validator = new();
        ValidationResult result = validator.Validate(template);
        if (!result.IsValid)
        {
            string message = string.Join(Environment.NewLine, result.Errors.Select(e => e.ErrorMessage));
            throw new InvalidDataException($"Template validation failed: {file}{Environment.NewLine}{message}");
        }

        return template;
    }

    public async Task<IEnumerable<TimesheetTemplate>> LoadAsync()
    {
        string[] files = Directory.GetFiles(_directory, searchPattern: "*.json", SearchOption.TopDirectoryOnly);
        return await Task.WhenAll(files.Select(LoadAsync));
    }
}

file class TimesheetTemplateValidator : AbstractValidator<TimesheetTemplate>
{
    public TimesheetTemplateValidator()
    {
        RuleFor(x => x.Metadata).NotNull();
        RuleFor(x => x.Metadata.DeprecatedAt).GreaterThan(x => x.Metadata.CreatedAt).When(x => x.Metadata.DeprecatedAt is not null);

        RuleForEach(x => x.Cells).ChildRules(cell =>
        {
            cell.RuleFor(c => c.Key).NotEmpty();
            cell.RuleFor(c => c.Field).NotEmpty();
            cell.RuleFor(c => c.InputDatatype).NotEmpty();
            cell.RuleFor(c => c.OutputDatatype).NotEmpty();

            cell.RuleFor(c => c.Pattern)
                .NotEmpty()
                .Must(BeValidRegex).WithMessage(x => $"Invalid regex: {x.Pattern}");

            cell.RuleFor(c => c.InputExample)
                .NotEmpty()
                .Must((c, input) => MatchesPattern(input, c.Pattern))
                .WithMessage(x => $"Pattern '{x.Pattern}' does not match InputExample '{x.InputExample}'");

            cell.RuleFor(c => c.OutputExample)
                .NotEmpty()
                .Must((c, output) => OutputMatchesInput(c.Pattern, c.InputExample, output))
                .WithMessage(x => $"OutputExample '{x.OutputExample}' does not correspond to InputExample '{x.InputExample}' via pattern '{x.Pattern}'");
        });

        RuleForEach(x => x.Ranges).ChildRules(range =>
        {
            range.RuleFor(r => r.Start).NotEmpty();
            range.RuleFor(r => r.End).NotEmpty();
            range.RuleFor(r => r.Field).NotEmpty();

            range.RuleFor(r => r.Pattern)
                .NotEmpty()
                .Must(BeValidRegex).WithMessage(x => $"Invalid regex: {x.Pattern}");

            range.RuleFor(r => r.InputExample)
                .NotEmpty()
                .Must((r, input) => MatchesPattern(input, r.Pattern))
                .WithMessage(x => $"Pattern '{x.Pattern}' does not match InputExample '{x.InputExample}'");

            range.RuleFor(r => r.OutputExample)
                .NotEmpty()
                .Must((r, output) => OutputMatchesInput(r.Pattern, r.InputExample, output))
                .WithMessage(x => $"OutputExample '{x.OutputExample}' does not correspond to InputExample '{x.InputExample}' via pattern '{x.Pattern}'");
        });
    }

    private static bool BeValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesPattern(string input, string pattern)
    {
        return new Regex(pattern).IsMatch(input);
    }

    private static bool OutputMatchesInput(string pattern, string input, string output)
    {
        Match match = new Regex(pattern).Match(input);
        if (!match.Success)
        {
            return false;
        }

        string extracted = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
        return extracted == output;
    }
}
