using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

[Authorize]
[Route("/account")]
public class AccountController : Controller
{
    private readonly IAuthService auth;
    public AccountController(IAuthService auth){
        this.auth = auth;
    }

    [HttpGet]
    public IActionResult Root()
    {
        // HttpContext.User
        return Ok();
    }

    [HttpGet("register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        ViewData["Action"] = "Register";
        return View("RegisterOrLogin");
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromForm] UserView user)
    {
        ViewData["Action"] = "Register";
        if(!ModelState.IsValid) return View("RegisterOrLogin", user);

        var errors = await auth.Register(user.Email, user.Password);
        if((errors ?? new List<string>()).Count() == 0)
            return Redirect("/account");
        
        foreach(var e in errors) ModelState.AddModelError("", e);
        return View("RegisterOrLogin", user);
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        ViewData["Action"] = "Login";
        return View("RegisterOrLogin");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] UserView user)
    {
        ViewData["Action"] = "Login";

        if (!ModelState.IsValid) return View("RegisterOrLogin", user);

        string result = await auth.Login(user.Email, user.Password);
        if(result == null){
            return Redirect("/account");
        }

        ModelState.AddModelError("", result);
        return View("RegisterOrLogin", user);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await auth.Logout();
        return Redirect("/");
    }
}

public class UserView {
    [Required]
    [EmailAddress]
    public string Email {get;set;}
    [Required]
    [DataType(DataType.Password)]
    public string Password {get;set;}
}