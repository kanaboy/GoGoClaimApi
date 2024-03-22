namespace GoGoClaimApi.Web.Responses;

public class ClaimResponse
{
    public IEnumerable<dynamic> claims { get; set; } = [];
    public long totalClaims { get; set; } = 0;
    public IEnumerable<long> totalWidgets1 { get; set; } = [];
    public IEnumerable<long> totalWidgets2 { get; set; } = [];
}
