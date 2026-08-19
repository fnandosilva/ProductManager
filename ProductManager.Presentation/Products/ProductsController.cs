using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManager.Application.Products.Commands.AddToStock;
using ProductManager.Application.Products.Commands.CreateProduct;
using ProductManager.Application.Products.Commands.DecrementStock;
using ProductManager.Application.Products.Commands.DeleteProduct;
using ProductManager.Application.Products.Commands.UpdateProduct;
using ProductManager.Application.Products.Queries.GetProductById;
using ProductManager.Application.Products.Queries.GetProducts;
using ProductManager.Application.Products.Queries.GetProductsByStockLevel;
using ProductManager.Application.Products.Queries.SearchProducts;

namespace ProductManager.Presentation.Products;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var products = await _sender.Send(new GetProductsQuery(), cancellationToken);
        return Ok(products);
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string name, CancellationToken cancellationToken)
    {
        var products = await _sender.Send(new SearchProductsQuery(name), cancellationToken);
        return Ok(products);
    }

    [HttpGet("stock-level")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStockLevel(
       [FromQuery] int min,
       [FromQuery] int max,
       CancellationToken cancellationToken)
    {
        var products = await _sender.Send(new GetProductsByStockLevelQuery(min, max), cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _sender.Send(
            new CreateProductCommand(request.Name, request.Description, request.Price, request.Stock),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _sender.Send(
            new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Stock),
            cancellationToken);

        return Ok(product);
    }

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DecrementStock(
        int id,
        int quantity,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DecrementStockCommand(id, quantity), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToStock(
        int id,
        int quantity,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AddToStockCommand(id, quantity), cancellationToken);
        return Ok();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}
