// RISK-15: The deprecated Microsoft.AspNetCore.Mvc.Versioning package has been
// replaced by the Asp.Versioning family (Asp.Versioning.Mvc.ApiExplorer), which
// uses the Asp.Versioning namespace. This global using makes the [ApiVersion]
// attribute and ApiVersion type available to all controllers without editing
// each controller file individually.
global using Asp.Versioning;
