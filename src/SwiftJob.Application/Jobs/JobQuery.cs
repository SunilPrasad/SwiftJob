using System.ComponentModel.DataAnnotations;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Jobs;

public enum JobSortField
{
    ReceivedAt = 0,
    Title = 1,
    Company = 2,
}

public enum SortDirection
{
    Desc = 0,
    Asc = 1,
}

/// <summary>Filter, sort and paging options for the job list. Bound from the query string.</summary>
public sealed class JobQuery
{
    public const int MaxPageSize = 100;

    /// <summary>Null means all statuses.</summary>
    public JobStatus? Status { get; set; }

    /// <summary>Case-insensitive match against job title or company.</summary>
    [StringLength(200)]
    public string? Search { get; set; }

    [StringLength(100)]
    public string? Source { get; set; }

    /// <summary>Inclusive lower bound (UTC).</summary>
    public DateTime? ReceivedFrom { get; set; }

    /// <summary>Exclusive upper bound (UTC).</summary>
    public DateTime? ReceivedBefore { get; set; }

    public JobSortField SortBy { get; set; } = JobSortField.ReceivedAt;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 25;
}
