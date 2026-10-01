using Backend.Errors;

namespace Backend.Tests.Unit;

public class ErrorCodesTests
{
    public static TheoryData<ErrorCode> AllCodes => [.. Enum.GetValues<ErrorCode>()];

    [Theory]
    [MemberData(nameof(AllCodes))]
    public void Every_error_code_has_a_status_and_message(ErrorCode code)
    {
        Assert.InRange(code.Status(), 400, 599);
        Assert.False(string.IsNullOrWhiteSpace(code.Message()));
    }

    [Theory]
    [InlineData(400, ErrorCode.ValidationFailed)]
    [InlineData(404, ErrorCode.NotFound)]
    [InlineData(405, ErrorCode.RequestFailed)]
    [InlineData(500, ErrorCode.Unexpected)]
    [InlineData(503, ErrorCode.Unexpected)]
    public void Framework_status_codes_map_to_an_error_code(int status, ErrorCode expected)
    {
        Assert.Equal(expected, ErrorCodes.FromStatus(status));
    }
}
