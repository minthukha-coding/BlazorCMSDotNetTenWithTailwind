using System.Security.Claims;
using BlazorCMSDotNetTenWithTailwind.Components;
using BlazorCMSDotNetTenWithTailwind.Components.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/"; // Login မဝင်ထားရင် သွားရမည့်လမ်းကြောင်း
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddAuthorizationCore();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles(); // (သို့) app.MapStaticAssets();

// ၁။ Routing ကို ဦးစွာ ခံရပါမည်
app.UseRouting();

// ၂။ Authentication နှင့် Authorization ကို MapStaticAssets / MapRazorComponents မတိုင်မီ တပ်ဆင်ရပါမည်
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/api/login", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"];
    var password = form["password"];

    // (Optional) ဒီနေရာမှာ သင့်ရဲ့ Database ထဲက Email / Password စစ်ဆေးမှုများ ထည့်နိုင်သည်
    if (!string.IsNullOrEmpty(email))
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Admin") // လိုအပ်ပါက Role ထည့်နိုင်သည်
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        };

        // ၁။ Cookie ကို Server တွင် Sign In လုပ်ခြင်း
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties);
        
        // ၂။ Dashboard သို့ Force Load ဖြင့် ဝင်ရောက်ခြင်း (Cookie အသစ်ပါလာစေရန်)
        return Results.Redirect("/cms/dashboard", true);
    }
    
    return Results.Redirect("/", true);
});

app.MapGet("/api/logout", async (HttpContext context) =>
{
    // ၁။ Cookie ကို SignOut လုပ်ခြင်း
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // ၂။ Browser ထဲက Cookies များကို ဖျက်ခြင်း
    if (context.Request.Cookies.Count > 0)
    {
        foreach (var cookie in context.Request.Cookies.Keys)
        {
            context.Response.Cookies.Delete(cookie);
        }
    }

    // ၃။ Login Page သို့ Force Redirect လုပ်ခြင်း
    return Results.Redirect("/", true);
});

app.Run();