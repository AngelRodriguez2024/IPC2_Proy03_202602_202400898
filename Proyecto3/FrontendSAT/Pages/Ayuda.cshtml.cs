using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontendSAT.Pages
{
    public class AyudaModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AyudaModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string MensajeReset { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.PostAsync("Resetear", null);

            if (response.IsSuccessStatusCode)
            {
                MensajeReset = "El sistema ha sido reiniciado con éxito.";
            }
            else
            {
                MensajeReset = "Ocurrió un error al intentar reiniciar el sistema.";
            }

            return Page();
        }
    }
}