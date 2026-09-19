using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Ritagissa.Models;
using System.Diagnostics;

public class TestController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TestController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetWord()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"http://localhost:5231/api/ordlista/random");

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "API-anropet misslyckades");

        var word = await response.Content.ReadFromJsonAsync<OrdDTO>();
        ViewBag.Word = word?.Word ?? "Inget ord hittades";
        return View();
    }


}