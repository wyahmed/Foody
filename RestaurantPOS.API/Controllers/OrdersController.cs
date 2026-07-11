using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantPOS.Application.Features.Orders.Commands;
using RestaurantPOS.Application.Features.Orders.Queries;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Interfaces;

namespace RestaurantPOS.API.Controllers;

/// <summary>Orders REST API – create, query, pay, and cancel orders.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Get paged orders for a branch.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(Shared.Models.PagedResult<OrderListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] OrderType? orderType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var bid = branchId ?? _currentUser.BranchId ?? Guid.Empty;
        var result = await _mediator.Send(
            new GetOrdersQuery(bid, page, pageSize, status, orderType, from, to, search),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Get order details by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a new order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetOrder), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>Process payment for an order and generate invoice.</summary>
    [HttpPost("{id:guid}/payment")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment(
        Guid id,
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ProcessPaymentCommand(id, request.Payments.Select(p =>
            new PaymentRequest(p.Method, p.Amount, p.Reference, p.CardLast4)).ToList());
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        return Ok(new { invoiceId = result.Value });
    }

    /// <summary>Cancel an order.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelRequest req, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, req.Reason), cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Order cancelled." });
    }
}

public record ProcessPaymentRequest(List<PaymentItemRequest> Payments);
public record PaymentItemRequest(PaymentMethod Method, decimal Amount, string? Reference = null, string? CardLast4 = null);
public record CancelRequest(string Reason);
