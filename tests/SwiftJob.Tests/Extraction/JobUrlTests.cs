using SwiftJob.Application.Extraction;

namespace SwiftJob.Tests.Extraction;

public class JobUrlTests
{
    [Theory]
    [InlineData("https://www.linkedin.com/comm/jobs/view/4012345671/?trackingId=abc&refId=x&trk=eml",
        "https://www.linkedin.com/jobs/view/4012345671", "4012345671")]
    [InlineData("https://uk.linkedin.com/jobs/view/senior-net-developer-at-fintech-4012345671?trk=public",
        "https://www.linkedin.com/jobs/view/4012345671", "4012345671")]
    [InlineData("https://uk.indeed.com/rc/clk?jk=a1b2c3d4e5f6&from=ja&tk=xyz",
        "https://uk.indeed.com/viewjob?jk=a1b2c3d4e5f6", "a1b2c3d4e5f6")]
    [InlineData("https://www.reed.co.uk/jobs/net-developer/51234567?source=email&utm_medium=email",
        "https://www.reed.co.uk/jobs/51234567", "51234567")]
    [InlineData("https://www.totaljobs.com/job/software-architect/fabrikam-job104455661?utm_source=alert",
        "https://www.totaljobs.com/job/software-architect/fabrikam-job104455661", null)]
    public void Known_boards_are_normalised(string input, string expectedUrl, string? expectedId)
    {
        var result = JobUrl.Canonicalise(input);

        Assert.NotNull(result);
        Assert.Equal(expectedUrl, result.Url);
        Assert.Equal(expectedId, result.SourceJobId);
    }

    [Fact]
    public void Generic_urls_lose_tracking_parameters_and_fragment_but_keep_meaningful_ones()
    {
        var result = JobUrl.Canonicalise("https://Careers.Example.com/jobs/123/?utm_source=x&gclid=1&id=9&a=2#apply");

        Assert.Equal("https://careers.example.com/jobs/123?a=2&id=9", result!.Url);
        Assert.Null(result.SourceJobId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:jobs@example.com")]
    [InlineData("/relative/path")]
    public void Invalid_urls_are_rejected(string? input) => Assert.Null(JobUrl.Canonicalise(input));
}
