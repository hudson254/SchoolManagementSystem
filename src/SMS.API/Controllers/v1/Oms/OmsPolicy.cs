namespace SMS.API.Controllers.v1.Oms;

/// <summary>
/// Authorization policy names for OMS endpoints.
/// These policies are wired in Program.cs and must match the role-based
/// access rules defined in SMS.Application.Common.OmsAuthorization.
/// </summary>
public static class OmsPolicy
{
    // --- Legacy Order policies (Phase 2A) ---
    public const string CanViewOrders = "Oms.CanViewOrders";
    public const string CanCreateOrder = "Oms.CanCreateOrder";
    public const string CanEditOrder = "Oms.CanEditOrder";
    public const string CanSubmitOrder = "Oms.CanSubmitOrder";
    public const string CanCancelAnyOrder = "Oms.CanCancelAnyOrder";
    public const string CanCancelOwnOrder = "Oms.CanCancelOwnOrder";
    public const string CanApproveOrder = "Oms.CanApproveOrder";
    public const string CanRejectOrder = "Oms.CanRejectOrder";

    // --- OMS Request policies (Phase 2C) ---
    public const string CanViewRequests = "Oms.CanViewRequests";
    public const string CanCreateRequest = "Oms.CanCreateRequest";
    public const string CanUpdateRequest = "Oms.CanUpdateRequest";
    public const string CanSubmitRequest = "Oms.CanSubmitRequest";
    public const string CanAssignRequest = "Oms.CanAssignRequest";
    public const string CanReassignRequest = "Oms.CanReassignRequest";
    public const string CanReviewRequest = "Oms.CanReviewRequest";
    public const string CanApproveRequest = "Oms.CanApproveRequest";
    public const string CanRejectRequest = "Oms.CanRejectRequest";
    public const string CanReturnRequest = "Oms.CanReturnRequest";
    public const string CanCancelOwnRequest = "Oms.CanCancelOwnRequest";
    public const string CanCancelAnyRequest = "Oms.CanCancelAnyRequest";
    public const string CanCompleteRequest = "Oms.CanCompleteRequest";
    public const string CanEscalateRequest = "Oms.CanEscalateRequest";
    public const string CanCommentOnRequest = "Oms.CanCommentOnRequest";
    public const string CanManageRequestTypes = "Oms.CanManageRequestTypes";
}