using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ClothingStore.Models;
using ClothingStore.Data;
using Microsoft.AspNetCore.Authorization;

namespace ClothingStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index(string searchName, decimal? minPrice, decimal? maxPrice, string searchType)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
                products = products.Where(p => p.Name.Contains(searchName));

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(searchType))
                products = products.Where(p => p.Category == searchType);

            var cartJson = HttpContext.Session.GetString("Cart");
            int cartCount = string.IsNullOrEmpty(cartJson) ? 0 : JsonSerializer.Deserialize<List<CartItem>>(cartJson)?.Count ?? 0;
            ViewBag.CartCount = cartCount;

            ViewData["searchName"] = searchName;
            ViewData["minPrice"] = minPrice;
            ViewData["maxPrice"] = maxPrice;
            ViewData["searchType"] = searchType;

            return View(await products.ToListAsync());
        }

        [HttpPost]
        [Authorize]

        public IActionResult AddToCart(int id, string size = null, int quantity = 1)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            var existingItem = cart.FirstOrDefault(c => c.ProductId == id && c.Size == size);
            if (existingItem != null)
            {
                existingItem.Quantity = (existingItem.Quantity ?? 0) + quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    Size = size
                });
            }

            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));

            TempData["Message"] = $"{product.Name} (Size: {size ?? "N/A"}, Quantity: {quantity}) đã được thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        public IActionResult ViewCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            decimal total = cart.Sum(item => (item?.Price ?? 0m) * (item?.Quantity ?? 0));
            ViewBag.Total = total;

            return View(cart);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int index)
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return RedirectToAction("ViewCart");
            }

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            if (index >= 0 && index < cart.Count)
            {
                cart.RemoveAt(index);
                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
                TempData["Message"] = "Sản phẩm đã được xóa khỏi giỏ hàng!";
            }

            return RedirectToAction("ViewCart");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


    }
}
