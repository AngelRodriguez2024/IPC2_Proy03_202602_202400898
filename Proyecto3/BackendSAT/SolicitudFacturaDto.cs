using System.Text.Json.Serialization;

namespace BackendSAT
{
    public class SolicitudFacturaDto
    {
        [JsonPropertyName("TIEMPO")]
        public string Tiempo { get; set; }

        [JsonPropertyName("REFERENCIA")]
        public string Referencia { get; set; }

        [JsonPropertyName("NIT_EMISOR")]
        public string NitEmisor { get; set; }

        [JsonPropertyName("NIT_RECEPTOR")]
        public string NitReceptor { get; set; }

        [JsonPropertyName("VALOR")]
        public decimal Valor { get; set; }

        [JsonPropertyName("IVA")]
        public decimal Iva { get; set; }

        [JsonPropertyName("TOTAL")]
        public decimal Total { get; set; }
    }
}