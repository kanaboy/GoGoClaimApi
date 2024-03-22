namespace GoGoClaimApi.Domain.Entities;
public class Claimed : BaseAuditableEntity
{
    public string VisitId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Dir { get; set; } = string.Empty;
}
