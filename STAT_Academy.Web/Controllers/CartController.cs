using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models;
using STAT_Academy.Web.ViewModels;

namespace STAT_Academy.Web.Controllers;

[Authorize]
public class CartController : Controller
{
    private const string CartKey = "STAT_CART";
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public IActionResult Index() => View(GetCart());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product == null) return NotFound();

        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.ProductId == productId);
        if (item == null) cart.Add(new CartItemViewModel { ProductId = product.Id, Name = product.Name, Price = product.Price, Quantity = Math.Max(1, quantity) });
        else item.Quantity += Math.Max(1, quantity);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        var cart = GetCart();
        cart.RemoveAll(x => x.ProductId == productId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm()
    {
        var cart = GetCart();
        if (!cart.Any()) return RedirectToAction(nameof(Index));
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var order = new Order
        {
            UserId = user.Id,
            Total = cart.Sum(x => x.Subtotal),
            Items = cart.Select(x => new OrderItem { ProductId = x.ProductId, Quantity = x.Quantity, UnitPrice = x.Price }).ToList()
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _db.Invoices.Add(new Invoice { OrderId = order.Id, Number = $"FAC-{DateTime.UtcNow:yyyyMMdd}-{order.Id:0000}", Total = order.Total });
        await _db.SaveChangesAsync();
        SaveCart([]);
        TempData["Success"] = "Pedido confirmado correctamente.";
        return RedirectToAction("Orders", "Student");
    }

    private List<CartItemViewModel> GetCart()
    {
        var json = HttpContext.Session.GetString(CartKey);
        return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? [];
    }

    private void SaveCart(List<CartItemViewModel> cart) => HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
}
