﻿using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ECommerce.Models.Orders;
using Microsoft.AspNetCore.Identity;
using ECommerce.Models.Users;
namespace ECommerce.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly storeContext _db;
        private  List<ShoppingCartItem> _items;
        private readonly UserManager<User> _userManager;
        public ShoppingCartController(storeContext context, UserManager<User> userManager)
        {
            _db = context;
            _items = new List<ShoppingCartItem>();
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult AddtoCart(int id)
        {
            var _product=_db.Products.Find(id);  
            var _cartItems=HttpContext.Session.Get<List<ShoppingCartItem>>("Cart")??new List<ShoppingCartItem>();
            var extingproduct = _cartItems.FirstOrDefault(x => x.product.Id == id);
            if (_product.QuantityInStock <= 0 || _product.IsActive==false)
            {
                return RedirectToAction("Details","Products", new
                {
                    ID = id
                });
            }
            if (extingproduct != null)
            {
                if (_product.QuantityInStock > extingproduct.Quantitiy)
                { extingproduct.Quantitiy++; }
                else
                {
                    return RedirectToAction("Details","Products", new
                    {
                        ID = id
                    });
                }
            }
            else
            {
                _cartItems.Add(new ShoppingCartItem
                {
                    Quantitiy = 1,
                    product = _product
                });
            }
            HttpContext.Session.Set("Cart", _cartItems);
            return RedirectToAction("Index", "Products");

        }
        public IActionResult ViewCart()
        {
            var _cartItems = HttpContext.Session.Get<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();

            var catviewmodel = new ShoppingCartViewItem
            {
                Items = _cartItems,
                TotalPrice = _cartItems.Sum(item => item.product.Price * item.Quantitiy)
            };
            return View(catviewmodel);
        }
        public IActionResult RemoveFromCart(int id)
        {
            var _cartItems = HttpContext.Session.Get<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            var extingproduct = _cartItems.FirstOrDefault(x => x.product.Id == id);
            if (extingproduct != null)
            {
                _cartItems.Remove(extingproduct);
            }
            HttpContext.Session.Set("Cart", _cartItems);

            return RedirectToAction("ViewCart");
        }
        
        public async Task<IActionResult> Purchase()
        {
            var _cartItems = HttpContext.Session.Get<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            if(_cartItems.Count==0)
            {
                return RedirectToAction("Index", "Products");
            }
            var orderitemincart = new List<OrderItem>();
            decimal sumtotal = 0;
            
            foreach (var item in _cartItems)
            {
                var product = _db.Products.Find(item.product.Id);
                product.QuantityInStock -= item.Quantitiy;
                orderitemincart.Add(new OrderItem
                {
                    Id = item.Id,
                    ProductId=item.product.Id,
                    Quantity=item.Quantitiy,
                    //OrderId=1,
                    UnitPrice=Convert.ToDecimal(item.product.Price*item.Quantitiy)
                });
                sumtotal =sumtotal + Convert.ToDecimal(item.product.Price * item.Quantitiy);
                _db.OrderItems.AddRange(orderitemincart);
            }
            var totals = new Order();
            var user = await _userManager.GetUserAsync(User);
            totals.UserId = user.Id;
            totals.OrderDate = DateTime.Now;
            totals.OrderItems= orderitemincart;
            totals.TotalAmount=sumtotal;
            _db.Orders.Add(totals);
            _db.SaveChanges();
            HttpContext.Session.Set("Cart", new List<ShoppingCartItem>());
            return RedirectToAction("Index","Home");
        }
    }
}
