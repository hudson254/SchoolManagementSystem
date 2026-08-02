global using MediatR;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using SMS.Application.Common.Interfaces;
global using SMS.Application.DTOs;
global using SMS.Application.Exceptions;
global using SMS.Domain.Entities;
global using SMS.Domain.Interfaces;
// Remove the ambiguous ITenantContext using
// We'll use fully qualified names where needed