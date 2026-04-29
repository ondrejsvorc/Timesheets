// TimesheetImporter.cs

using CzechHolidays;

namespace Timesheets.Api.Timesheets;

public interface ITimesheetImporter<T> where T : ITimesheet
{
    Task<T> ImportAsync(Stream stream);
}

public sealed class AttendanceTimesheetImporter(ITimesheetReader<AttendanceTimesheet> reader, ICzechHolidaysFactory factory) : ITimesheetImporter<AttendanceTimesheet>
{
    public async Task<AttendanceTimesheet> ImportAsync(Stream stream)
    {
        AttendanceTimesheet timesheet = reader.Read(stream);
        IEnumerable<CzechHolidayDate> holidays = factory.Create(timesheet.Year);

        return timesheet with
        {
            Days = timesheet.Days
                .Select(day => day with { IsHoliday = holidays.Any(holiday => holiday.Date == DateOnly.FromDateTime(day.Date)) })
                .ToList()
        };
    }
}

// TimesheetValidator.cs

using System.Globalization;

namespace Timesheets.Api.Timesheets;

public enum IssueType { Warning = 0, Error = 1 }

public sealed record TimesheetIssue(string Code, IssueType Type, string Description);
public sealed record DayIssue(string Code, IssueType Type, string Description, int Day, string Field);

public sealed class TimesheetReview
{
    public bool HasErrors => Issues.Any(i => i.Type is IssueType.Error) || DayIssues.Any(i => i.Type is IssueType.Error);
    public bool HasWarnings => Issues.Any(i => i.Type is IssueType.Warning) || DayIssues.Any(i => i.Type is IssueType.Warning);

    public bool CanBeSaved => !HasErrors;
    public bool CanBeApproved => !HasErrors && !HasWarnings;

    public IEnumerable<TimesheetIssue> Issues { get; init; } = [];
    public IEnumerable<DayIssue> DayIssues { get; init; } = [];
}

file static class TimesheetLimits
{
    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 88 odst. 1
    /// Přestávka nejpozději po 6 hodinách nepřetržité práce.
    /// </summary>
    public const decimal MaxContinuousWorkBeforeBreakHours = 6;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 83 odst. 4
    /// Délka směny nesmí přesáhnout 12 hodin.
    /// </summary>
    public const decimal MaxWorkShiftHours = 12;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 90 odst. 1
    /// Minimální odpočinek mezi koncem a začátkem směny činí alespoň 11 hodin.
    /// </summary>
    public const decimal MinRestBetweenShiftsHours = 11;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 92 odst. 1
    /// Nepřetržitý odpočinek v týdnu musí činit alespoň 35 hodin.
    /// 24 h týdenního + 11 h denního.
    /// </summary>
    public const decimal MinWeeklyRestHours = 35;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce - § 83 odst. 1
    /// Stanovená týdenní pracovní doba je 40 hodin (při plném úvazku).
    /// </summary>
    public const decimal StandardWeeklyWorkHours = 40;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 79 odst. 1
    /// Standardní denní pracovní doba činí 8 hodin (při úvazku 1,0).
    /// </summary>
    public const decimal StandardWorkdayHours = 8;

    /// <summary>
    /// Zákon č. 262/2006 Sb., zákoník práce — § 88 odst. 1
    /// Minimální délka přestávky na jídlo a oddech činí 30 minut.
    /// </summary>
    public const decimal MinBreakDurationHours = 0.5m;

}

public interface ITimesheetReviewer<T> where T : ITimesheet
{
    TimesheetReview Review(T timesheet);
}

public sealed class CombinedTimesheetReviewer : ITimesheetReviewer<CombinedTimesheet>
{
    public TimesheetReview Review(CombinedTimesheet timesheet)
    {
        return new TimesheetReview
        {
            Issues = ReviewTimesheet(timesheet),
            DayIssues = timesheet.Days.SelectMany(ReviewDay)
        };
    }

    private static IEnumerable<TimesheetIssue> ReviewTimesheet(CombinedTimesheet timesheet) =>
    [
        .. ReviewOvertime(timesheet),
        .. ReviewUndertime(timesheet),
        .. ReviewWeeklyWorkHours(timesheet)
    ];

    private static IEnumerable<DayIssue> ReviewDay(CombinedDay day) =>
    [
        .. ReviewOvertime(day),
        .. ReviewUndertime(day),
        .. ReviewTooLongWorkday(day),
        .. ReviewWeekendAndHolidayWork(day)
    ];

    private static IEnumerable<TimesheetIssue> ReviewOvertime(CombinedTimesheet timesheet)
    {
        if (timesheet.TotalHours > timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue
            (
                Code: "ERR-COM-02",
                Type: IssueType.Error,
                Description: "Celková pracovní doba za měsíc přesahuje součet denních povinností."
            );
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewUndertime(CombinedTimesheet timesheet)
    {
        if (timesheet.TotalHours < timesheet.TotalHoursObligation)
        {
            yield return new TimesheetIssue
            (
                Code: "ERR-COM-03",
                Type: IssueType.Error,
                Description: "Celková pracovní doba za měsíc je nižší než součet denních povinností."
            );
        }
    }

    private static IEnumerable<TimesheetIssue> ReviewWeeklyWorkHours(CombinedTimesheet timesheet)
    {
        List<CombinedDay> orderedWorkDays = timesheet.Days
            .Where(d => d.IsWorkday)
            .OrderBy(d => d.Date)
            .ToList();

        decimal weeklyLimit = TimesheetLimits.StandardWeeklyWorkHours * timesheet.TotalWorkload;

        var weeks = orderedWorkDays.GroupBy(d => ISOWeek.GetWeekOfYear(d.Date));
        foreach (var week in weeks)
        {
            decimal weekTotalHours = week.Sum(day => day.TotalHours);
            if (weekTotalHours > weeklyLimit)
            {
                yield return new TimesheetIssue(
                    Code: "ERR-COM-04",
                    Type: IssueType.Error,
                    Description:
                        $"V týdnu {week.Key} bylo odpracováno {weekTotalHours:F1} h, " +
                        $"což překračuje zákonný limit {weeklyLimit:F1} h při celkovém úvazku {timesheet.TotalWorkload:P0}."
                );
            }
        }
    }

    private static IEnumerable<DayIssue> ReviewOvertime(CombinedDay day)
    {
        if (day.IsWorkday && day.TotalHours > day.TotalHoursObligation)
        {
            yield return new DayIssue(
                Code: "WAR-ATT-02A",
                Type: IssueType.Warning,
                Description: "Odpracovaný čas za den je vyšší než denní pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewUndertime(CombinedDay day)
    {
        if (day.IsWorkday && day.TotalHours < day.TotalHoursObligation)
        {
            yield return new DayIssue(
                Code: "WAR-ATT-02B",
                Type: IssueType.Warning,
                Description: "Odpracovaný čas za den je nižší než denní pracovní povinnost.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewTooLongWorkday(CombinedDay day)
    {
        if (day.IsWorkday && day.TotalHours is > TimesheetLimits.MaxWorkShiftHours)
        {
            yield return new DayIssue
            (
                Code: "ERR-ATT-05",
                Type: IssueType.Error,
                Description: $"Odpracovaný čas za den překračuje {TimesheetLimits.MaxWorkShiftHours} hodin.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }

    private static IEnumerable<DayIssue> ReviewWeekendAndHolidayWork(CombinedDay day)
    {
        if (day.IsWeekend && day.TotalHours > 0)
        {
            yield return new DayIssue(
                Code: "WAR-COM-01",
                Type: IssueType.Warning,
                Description: "Práce evidovaná o víkendu. Očekává se, že bude kompenzována v jiném pracovním dni.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
        else if (day.IsHoliday && day.TotalHours > 0)
        {
            yield return new DayIssue(
                Code: "WAR-COM-02",
                Type: IssueType.Warning,
                Description: "Práce evidovaná ve státní svátek. Očekává se, že bude kompenzována v jiném pracovním dni.",
                Day: day.Date.Day,
                Field: nameof(day.TotalHours)
            );
        }
    }
}

// CreateProject.cs

using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class CreateProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle)
           .WithSummary("Create Project")
           .DisableAntiforgery()
           .WithRequestValidation<Request>();

    public sealed record Request(string Name, string RegistrationNumber, DateTime StartDate, DateTime? EndDate);
    public sealed record Response(ProjectItem Project);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(ProjectSchema.Name.MaxLength);

            RuleFor(x => x.RegistrationNumber)
                .MaximumLength(ProjectSchema.RegistrationNumber.MaxLength);

            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate)
                .When(x => x.EndDate.HasValue);
        }
    }

    private static async Task<Results<Created<Response>, BadRequest<string>>> Handle([FromBody] Request request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Project project = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            RegistrationNumber = request.RegistrationNumber,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        ProjectItem projectItem = new(
            project.Id,
            project.Name,
            project.RegistrationNumber,
            project.StartDate,
            project.EndDate,
            ContractCount: 0
        );

        return TypedResults.Created($"/projects/{project.Id}", new Response(projectItem));
    }
}

// GetProject.cs

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data;

namespace Timesheets.Api.Projects.Endpoints;

public sealed class GetProject : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", Handle)
           .WithSummary("Get Project");

    public sealed record ProjectItem(Guid Id, string Name, string RegistrationNumber);
    public sealed record Response(ProjectItem Project);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        ProjectItem? project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectItem(
                p.Id,
                p.Name,
                p.RegistrationNumber
            ))
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new Response(project));
    }
}

// AbstractFactoryExample.cs

interface IGuiFactory
{
  public Button CreateButton();
  public Checkbox CreateCheckbox();
}
class WinFactory : IGuiFactory
{
    public Button CreateButton()
    {
      Console.WriteLine("Creating Windows button...");
      return new WinButton();
    }

    public Checkbox CreateCheckbox()
    {
      Console.WriteLine("Creating Windows checkbox...");
      return new WinCheckbox();
    }
}
class MacFactory : IGuiFactory
{
    public Button CreateButton()
    {
      Console.WriteLine("Creating MAC button...");
      return new MacButton();
    }

    public Checkbox CreateCheckbox()
    {
      Console.WriteLine("Creating MAC checkbox...");
      return new MacCheckbox();
    }
}

abstract class Button { }
class WinButton : Button { }
class MacButton : Button { }

abstract class Checkbox { }
class WinCheckbox : Checkbox { }
class MacCheckbox : Checkbox { }

// StrategyExample.cs

interface ISorter
{
    public IEnumerable<int> Sort (IEnumerable<int> collection);
}

class SortContext : ISorter
{
    private readonly Dictionary<Func<int, bool>, ISorter> _sorters = new()
    {
        { collectionCount => collectionCount <= 20, new BubbleSorter() },
        { collectionCount => collectionCount > 20, new QuickSorter() }
    };

    public IEnumerable<int> Sort(IEnumerable<int> collection)
    {
        int count = collection.Count();

        ISorter sorter = _sorters
            .SingleOrDefault(x => x.Key(count))
            .Value;

        return sorter.Sort(collection);
    }
}

class BubbleSorter : ISorter
{
    public IEnumerable<int> Sort(IEnumerable<int> collection)
    {
        Console.WriteLine("Using bubble sort...");
        return BubbleSort(collection);
    }

    private IEnumerable<int> BubbleSort(IEnumerable<int> collection)
    {
        List<int> list = collection.ToList();
        bool swapped;

        do
        {
            swapped = false;
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (Comparer<int>.Default.Compare(list[i], list[i + 1]) > 0)
                {
                    (list[i + 1], list[i]) = (list[i], list[i + 1]);
                    swapped = true;
                }
            }
        } while (swapped);

        return list;
    }
}

class QuickSorter : ISorter
{
    public IEnumerable<int> Sort(IEnumerable<int> collection)
    {
        Console.WriteLine("Using quick sort...");
        return QuickSort(collection.ToList());
    }

    private IEnumerable<int> QuickSort(List<int> collection)
    {
        if (collection.Count <= 1)
        {
            return collection;
        }

        int pivot = collection[0];
        List<int> less = [];
        List<int> greater = [];

        foreach (var item in collection.Skip(1))
        {
            if (Comparer<int>.Default.Compare(item, pivot) < 0)
            {
                less.Add(item);
            }
            else
            {
                greater.Add(item);
            }
        }

        IEnumerable<int> sortedLess = QuickSort(less);
        IEnumerable<int> sortedGreater = QuickSort(greater);

        return sortedLess.Concat([pivot]).Concat(sortedGreater);
    }
}

// IteratorExample.cs

using System.Collections.Concurrent;

class Program
{
    static async Task Main()
    {
        TreeNode<string> root = new("Root");
        TreeNode<string> child1 = new("Child1");
        TreeNode<string> child2 = new("Child2");
        TreeNode<string> grandChild1 = new("GrandChild1");
        TreeNode<string> grandChild2 = new("GrandChild2");

        root.AddChild(child1);
        root.AddChild(child2);
        child1.AddChild(grandChild1);
        child2.AddChild(grandChild2);

        TreeCollection<string> tree = new(root);

        Task depthFirstTask = Task.Run(async () =>
        {
            IIterator<string> depthFirstIterator = tree.CreateDepthFirstIterator();
            while (depthFirstIterator.HasMore())
            {
                await Task.Delay(250);
                string current = depthFirstIterator.GetNext();
                Console.WriteLine($"[Thread: {Environment.CurrentManagedThreadId,-3}] Depth-First  : {current}");
            }
        });

        Task breadthFirstTask = Task.Run(async () =>
        {
            IIterator<string> breadthFirstIterator = tree.CreateBreadthFirstIterator();
            while (breadthFirstIterator.HasMore())
            {
                await Task.Delay(250);
                string current = breadthFirstIterator.GetNext();
                Console.WriteLine($"[Thread: {Environment.CurrentManagedThreadId,-3}] Breadth-First: {current}");
            }
        });

        await Task.WhenAll(depthFirstTask, breadthFirstTask);
        Console.WriteLine("Traversal complete.");
    }
}

public class TreeNode<T>(T value) where T : class
{
    public T Value { get; } = value;
    public List<TreeNode<T>> Children { get; } = [];

    public void AddChild(TreeNode<T> child)
    {
        Children.Add(child);
    }
}

public interface IIterator<T> where T : class
{
    T GetNext();
    bool HasMore();
}

public interface IIterableCollection<T> where T : class
{
    IIterator<T> CreateDepthFirstIterator();
    IIterator<T> CreateBreadthFirstIterator();
}

public class TreeCollection<T>(TreeNode<T> root) : IIterableCollection<T> where T : class
{
    private readonly TreeNode<T> _root = root;

    public IIterator<T> CreateDepthFirstIterator()
    {
        return new DepthFirstIterator<T>(_root);
    }

    public IIterator<T> CreateBreadthFirstIterator()
    {
        return new BreadthFirstIterator<T>(_root);
    }
}

public class DepthFirstIterator<T> : IIterator<T> where T : class
{
    private readonly ConcurrentStack<TreeNode<T>> _stack = new(); 

    public DepthFirstIterator(TreeNode<T> root)
    {
        _stack.Push(root);
    }

    public T GetNext()
    {
        if (!HasMore())
        {
            throw new InvalidOperationException("No more elements.");
        }

        if (!_stack.TryPop(out TreeNode<T>? currentNode))
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        foreach (TreeNode<T> child in currentNode.Children)
        {
            _stack.Push(child);
        }

        return currentNode.Value;
    }

    public bool HasMore()
    {
        return !_stack.IsEmpty;
    }
}

public class BreadthFirstIterator<T> : IIterator<T> where T : class
{
    private readonly ConcurrentQueue<TreeNode<T>> _queue = new();

    public BreadthFirstIterator(TreeNode<T> root)
    {
        _queue.Enqueue(root);
    }

    public T GetNext()
    {
        if (!HasMore())
        {
            throw new InvalidOperationException("No more elements.");
        }

        if (!_queue.TryDequeue(out TreeNode<T>? currentNode))
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        foreach (TreeNode<T> child in currentNode.Children)
        {
            _queue.Enqueue(child);
        }

        return currentNode.Value;
    }

    public bool HasMore()
    {
        return !_queue.IsEmpty;
    }
}

// CommandExample.cs

class VsCodeEditor
{
    public string Text { get; private set; } = string.Empty;
    private static string Clipboard { get; set; } = string.Empty;

    public void SetText(string newText)
    {
        Text = newText;
        Console.WriteLine($"Text: \"{Text}\"");
    }

    public void Copy()
    {
        Clipboard = Text;
        Console.WriteLine($"Copy: \"{Clipboard}\"");
    }

    public void Paste()
    {
        if (!string.IsNullOrEmpty(Clipboard))
        {
            Text += Clipboard;
            Console.WriteLine($"Paste: \"{Text}\"");
        }
        else
        {
            Console.WriteLine("Clipboard is empty, nothing to paste.");
        }
    }
}

class Invoker(Command command)
{
    private readonly Command _command = command;

    public void ExecuteCommand()
    {
        _command.Execute();
    }
}

abstract class Command(VsCodeEditor receiver, string parameters = "")
{
    protected readonly VsCodeEditor Receiver = receiver;
    protected readonly string Parameters = parameters;

    public abstract void Execute();
}

class CopyCommand(VsCodeEditor receiver, string parameters = "") : Command(receiver, parameters)
{
    public override void Execute()
    {
        Receiver.Copy();
    }
}

class PasteCommand(VsCodeEditor receiver, string parameters = "") : Command(receiver, parameters)
{
    public override void Execute()
    {
        Receiver.Paste();
    }
}