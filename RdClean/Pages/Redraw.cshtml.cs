using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;
using RdClean.Extensions;

namespace RdClean.Pages;

[Authorize]
public class RedrawModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext dbContext;

    public RedrawModel(
        ILogger<IndexModel> logger,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        this.dbContext = dbContext;
    }

    public void OnGet()
    {
    }
}
