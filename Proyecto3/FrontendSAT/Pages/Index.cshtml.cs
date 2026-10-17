using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace FrontendSAT.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public string RespuestaJson { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile archivoJson, string tipoMensaje)
        {
            if (archivoJson == null || archivoJson.Length == 0)
            {
                RespuestaJson = "Debe seleccionar un archivo JSON válido.";
                return Page();
            }

            using var stream = archivoJson.OpenReadStream();
            using var reader = new StreamReader(stream);
            string contenidoJson = await reader.ReadToEndAsync();

            var client = _httpClientFactory.CreateClient("BackendSAT");
            HttpContent httpContent = new StringContent(contenidoJson, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response;

            if (tipoMensaje == "RTU")
            {
                response = await client.PostAsync("CargarRTU", httpContent);
            }
            else
            {
                response = await client.PostAsync("ProcesarFacturas", httpContent);
            }

            if (response.IsSuccessStatusCode)
            {
                RespuestaJson = await response.Content.ReadAsStringAsync();
            }
            else
            {
                RespuestaJson = $"Error en la petición: {response.StatusCode}";
            }

            return Page();
        }
    }
}