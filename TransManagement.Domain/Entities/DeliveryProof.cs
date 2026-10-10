using TransManagement.Domain.Common;

namespace TransManagement.Domain.Entities;

public sealed class DeliveryProof : AuditableEntity
{
    private DeliveryProof() { }

    public DeliveryProof(Guid routeStopId, string receiverName, string? photoUrl,
        string? signatureData, string? note, DateTime capturedAtUtc)
    {
        if (routeStopId == Guid.Empty) throw new ArgumentException("Route stop ID is required.", nameof(routeStopId));
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverName);
        if (string.IsNullOrWhiteSpace(photoUrl) && string.IsNullOrWhiteSpace(signatureData))
            throw new ArgumentException("A delivery photo or signature is required.");

        RouteStopId = routeStopId;
        ReceiverName = receiverName.Trim();
        PhotoUrl = photoUrl?.Trim();
        SignatureData = signatureData?.Trim();
        Note = note?.Trim();
        CapturedAtUtc = capturedAtUtc;
    }

    public Guid RouteStopId { get; private set; }
    public RouteStop RouteStop { get; private set; } = null!;
    public string ReceiverName { get; private set; } = string.Empty;
    public string? PhotoUrl { get; private set; }
    public string? SignatureData { get; private set; }
    public string? Note { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }
}
