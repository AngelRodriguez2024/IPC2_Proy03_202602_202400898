using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FrontendSAT.Pages
{
    public class ConsultasModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ConsultasModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public DynamicResultadoConsulta? ResultadoConsulta { get; set; }
        public string JsonResumenIva { get; set; } = string.Empty;
        public string JsonResumenRango { get; set; } = string.Empty;

        public async Task OnGetConsultarContribuyenteAsync(string nit, string fechaInicio, string fechaFin)
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.GetAsync($"ConsultaDatos?fechaInicio={fechaInicio}&fechaFin={fechaFin}&nit={nit}");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                ResultadoConsulta = JsonSerializer.Deserialize<DynamicResultadoConsulta>(json);
            }
        }

        public async Task OnGetResumenIvaAsync(string fecha)
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.GetAsync($"ResumenIva?fecha={fecha}");

            if (response.IsSuccessStatusCode)
            {
                JsonResumenIva = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task OnGetResumenRangoAsync(string fechaInicio, string fechaFin, bool conIva)
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.GetAsync($"ResumenRango?fechaInicio={fechaInicio}&fechaFin={fechaFin}&conIva={conIva}");

            if (response.IsSuccessStatusCode)
            {
                JsonResumenRango = await response.Content.ReadAsStringAsync();
            }
        }
    }

    public class DynamicResultadoConsulta
    {
        public int totalFacturasAutorizadas { get; set; }
        public decimal montoTotalFacturado { get; set; }
    }
}