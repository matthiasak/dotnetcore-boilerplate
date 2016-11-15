using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;

public interface IAuthService {
    Task<string> Login(string email, string pass);
    Task Logout();
    Task<IEnumerable<string>> Register(string email, string pass);
    Task<bool> ResetPassword(string email, Func<Object, string> getCallbackUrl);
    Task<IdentityUser> GetUser(HttpContext context);
}

public class AuthService : IAuthService {
    private UserManager<IdentityUser> u;
    private SignInManager<IdentityUser> s;
    private IEmail emailer;

    public AuthService(UserManager<IdentityUser> u, SignInManager<IdentityUser> s, IEmail emailer){
        this.u = u;
        this.s = s;
        this.emailer = emailer;
    }

    public async Task<string> Login(string email, string pass){
        var result = await s.PasswordSignInAsync(email, pass, true, lockoutOnFailure: false);
        if(!result.Succeeded) 
            return "Invalid login attempt.";
        if(result.RequiresTwoFactor)
            return "TwoFactor authentication is required.";
        if(result.IsLockedOut)
            return "This account is locked.";
        return null;
    }

    public async Task<IEnumerable<string>> Register(string email, string pass){
        var user = new IdentityUser { UserName = email, Email = email };
        var result = await u.CreateAsync(user, pass);
        if(result.Succeeded){
            await s.SignInAsync(user, isPersistent: true);
            return null;
        } else {
            List<string> errors = new List<string>();
            foreach (var error in result.Errors)
                errors.Add(error.Description);
            return errors;
        }
    }

    public async Task Logout() => await s.SignOutAsync();

    public async Task<bool> ResetPassword(string email, Func<Object, string> getCallbackUrl){
        var user = await u.FindByNameAsync(email);
        
        if (user != null || (await u.IsEmailConfirmedAsync(user)))
            return false;
        
        var code = await u.GeneratePasswordResetTokenAsync(user);
        var url = getCallbackUrl(new { userId = user.Id, code = code });

        await emailer.SendEmailAsync(
            email,
            "Reset Password",
            $"Please reset your password by clicking here: <a href='{url}'>link</a>");
        
        return true;
    }

    public async Task<IdentityUser> GetUser(HttpContext context) => await u.GetUserAsync(context.User);
}
