using System.Text;
using Google.Apis.Gmail.v1.Data;
using SwiftJob.Infrastructure.Gmail;

namespace SwiftJob.Tests.Gmail;

public class GmailMessageParserTests
{
    private static string B64(string text) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Fact]
    public void Parses_headers_date_and_multipart_bodies_and_ignores_attachments()
    {
        var message = new Message
        {
            Id = "18c2", ThreadId = "t1", InternalDate = 1_790_000_000_000,
            Payload = new MessagePart
            {
                MimeType = "multipart/mixed",
                Headers = [new() { Name = "Subject", Value = "3 new jobs" }, new() { Name = "From", Value = "LinkedIn <a@linkedin.com>" }],
                Parts =
                [
                    new MessagePart
                    {
                        MimeType = "multipart/alternative",
                        Parts =
                        [
                            new MessagePart { MimeType = "text/plain", Body = new MessagePartBody { Data = B64("Plain £75,000 ✓") } },
                            new MessagePart { MimeType = "text/html", Body = new MessagePartBody { Data = B64("<p>Html</p>") } },
                        ],
                    },
                    new MessagePart { MimeType = "text/plain", Filename = "cv.txt", Body = new MessagePartBody { Data = B64("attachment") } },
                ],
            },
        };

        var parsed = GmailMessageParser.Parse(message);

        Assert.Equal("18c2", parsed.Id);
        Assert.Equal("3 new jobs", parsed.Subject);
        Assert.Equal("LinkedIn <a@linkedin.com>", parsed.From);
        Assert.Equal("Plain £75,000 ✓", parsed.BodyText);
        Assert.Equal("<p>Html</p>", parsed.BodyHtml);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1_790_000_000_000).UtcDateTime, parsed.ReceivedAt);
    }
}
