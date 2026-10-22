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
        public ReporteErroresDto? ReporteErrores { get; set; }
        public string JsonResumenIva { get; set; } = string.Empty;
        public string JsonResumenRango { get; set; } = string.Empty;
        public string RangoFechasReporte { get; set; } = string.Empty;

        public async Task OnGetConsultarContribuyenteAsync(string nit, string fechaInicio, string fechaFin)
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.GetAsync($"ConsultaDatos?fechaInicio={fechaInicio}&fechaFin={fechaFin}&nit={nit}");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                ResultadoConsulta = JsonSerializer.Deserialize<DynamicResultadoConsulta>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }

        public async Task OnGetReporteErroresAsync(string fechaInicio, string fechaFin)
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.GetAsync($"ReporteErrores?fechaInicio={fechaInicio}&fechaFin={fechaFin}");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                ReporteErrores = JsonSerializer.Deserialize<ReporteErroresDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                RangoFechasReporte = $"{fechaInicio} al {fechaFin}";
            }
        }

        public async Task OnGetResumenIvaAsync(string fecha)
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.GetAsync($"ResumenIva?fecha={fecha}");
            if (response.IsSuccessStatusCode) JsonResumenIva = await response.Content.ReadAsStringAsync();
        }

        public async Task OnGetResumenRangoAsync(string fechaInicio, string fechaFin, bool conIva)
        {
            var client = _httpClientFactory.CreateClient("BackendSAT");
            var response = await client.GetAsync($"ResumenRango?fechaInicio={fechaInicio}&fechaFin={fechaFin}&conIva={conIva}");
            if (response.IsSuccessStatusCode) JsonResumenRango = await response.Content.ReadAsStringAsync();
        }
    }

    public class DynamicResultadoConsulta
    {
        public int totalFacturasAutorizadas { get; set; }
        public decimal montoTotalFacturado { get; set; }
        public List<FacturaDto> facturas { get; set; } = new List<FacturaDto>();
    }

    public class FacturaDto
    {
        public string tiempo { get; set; }
        public string referencia { get; set; }
        public string nitEmisor { get; set; }
        public string nitReceptor { get; set; }
        public decimal valor { get; set; }
        public decimal iva { get; set; }
        public decimal total { get; set; }
        public string codigoAutorizacion { get; set; }
    }

    public class ReporteErroresDto
    {
        public int totalRecibidas { get; set; }
        public int emisorInvalido { get; set; }
        public int receptorInvalido { get; set; }
        public int emisorInexistente { get; set; }
        public int receptorInexistente { get; set; }
        public int ivaMalCalculado { get; set; }
        public int totalMalCalculado { get; set; }
        public int referenciaDuplicada { get; set; }
        public int facturasSinErrores { get; set; }
        public int emisoresUnicos { get; set; }
        public int receptoresUnicos { get; set; }
    }
}