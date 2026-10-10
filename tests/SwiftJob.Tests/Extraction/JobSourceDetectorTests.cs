using SwiftJob.Application.Extraction;

namespace SwiftJob.Tests.Extraction;

public class JobSourceDetectorTests
{
    [Theory]
    [InlineData("LinkedIn Job Alerts <jobalerts-noreply@linkedin.com>", null, "LinkedIn")]
    [InlineData("alert@indeed.com", "Something else", "Indeed")]
    [InlineData("Reed <jobs@mail.reed.co.uk>", null, "Reed")]
    [InlineData("Sarah <sarah@smallagency.co.uk>", "Small Agency Ltd", "Small Agency Ltd")]
    [InlineData("Sarah <sarah@smallagency.co.uk>", null, "Email")]
    [InlineData("fakelinkedin.com <x@notlinkedin.com>", null, "Email")]
    public void Detects_source_from_sender_domain_first(string sender, string? guess, string expected) =>
        Assert.Equal(expected, JobSourceDetector.Detect(sender, guess));
}
