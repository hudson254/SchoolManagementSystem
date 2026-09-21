namespace SMS.API.Controllers.v1.Oms;

/// <summary>
/// Authorization policy names for OMS endpoints.
/// These policies are wired in Program.cs and must match the role-based
/// access rules defined in SMS.Application.Common.OmsAuthorization.
/// </summary>
public static class OmsPolicy
{
    /// <summary>
    /// View orders: Coordinator, Lecturer (view-only).
    /// Matches OmsAuthorization.ViewOrdersRoles.
    /// </summary>
    public const string CanViewOrders = "Oms.CanViewOrders";

    /// <summary>
    /// Create order: Coordinator.
    /// Matches OmsAuthorization.CreateOrderRoles.
    /// </summary>
    public const string CanCreateOrder = "Oms.CanCreateOrder";

    /// <summary>
    /// Edit order (add/remove items on Draft orders): Coordinator.
    /// Matches OmsAuthorization.CreateOrderRoles.
    /// </summary>
    public const string CanEditOrder = "Oms.CanEditOrder";

    /// <summary>
    /// Submit order: Coordinator.
    /// Matches OmsAuthorization.CreateOrderRoles.
    /// </summary>
    public const string CanSubmitOrder = "Oms.CanSubmitOrder";

    /// <summary>
    /// Cancel any order: Administrator only.
    /// Matches OmsAuthorization.CancelAnyOrderRoles.
    /// Broader cancellation permission - delegates creator-only checks to the handler.
    /// </summary>
    public const string CanCancelAnyOrder = "Oms.CanCancelAnyOrder";

    /// <summary>
    /// Cancel own order: Administrator and Coordinator.
    /// Matches OmsAuthorization.CancelOwnOrderRoles.
    /// Creator-only cancellation - the handler enforces ownership.
    /// </summary>
    public const string CanCancelOwnOrder = "Oms.CanCancelOwnOrder";
}
