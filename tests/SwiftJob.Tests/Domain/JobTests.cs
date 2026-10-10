using SwiftJob.Domain.Jobs;

namespace SwiftJob.Tests.Domain;

public class JobTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Job NewJob() => new() { Title = "Developer", Source = "LinkedIn" };

    [Fact]
    public void New_job_starts_with_status_New()
    {
        var job = NewJob();

        Assert.Equal(JobStatus.New, job.Status);
        Assert.Null(job.StatusChangedAt);
    }

    [Fact]
    public void ChangeStatus_updates_status_and_timestamp()
    {
        var job = NewJob();

        var changed = job.ChangeStatus(JobStatus.Applied, Now);

        Assert.True(changed);
        Assert.Equal(JobStatus.Applied, job.Status);
        Assert.Equal(Now, job.StatusChangedAt);
    }

    [Fact]
    public void ChangeStatus_to_same_status_is_a_no_op()
    {
        var job = NewJob();
        job.ChangeStatus(JobStatus.NotRelevant, Now);

        var changed = job.ChangeStatus(JobStatus.NotRelevant, Now.AddHours(1));

        Assert.False(changed);
        Assert.Equal(Now, job.StatusChangedAt);
    }

    [Theory]
    [InlineData(JobStatus.Applied, JobStatus.New)]
    [InlineData(JobStatus.NotRelevant, JobStatus.Applied)]
    [InlineData(JobStatus.Applied, JobStatus.NotRelevant)]
    public void Status_can_be_changed_again(JobStatus first, JobStatus second)
    {
        var job = NewJob();
        job.ChangeStatus(first, Now);

        job.ChangeStatus(second, Now.AddMinutes(5));

        Assert.Equal(second, job.Status);
    }

    [Fact]
    public void ChangeStatus_rejects_undefined_values()
    {
        var job = NewJob();

        Assert.Throws<ArgumentOutOfRangeException>(() => job.ChangeStatus((JobStatus)42, Now));
    }
}
