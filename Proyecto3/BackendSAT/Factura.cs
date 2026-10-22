using System;

namespace BackendSAT
{
    public class Factura
    {
        public string Tiempo { get; set; }
        public string Referencia { get; set; }
        public string NitEmisor { get; set; }
        public string NitReceptor { get; set; }
        public decimal Valor { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public string CodigoAutorizacion { get; set; }

        public Factura(string tiempo, string referencia, string nitEmisor, string nitReceptor, decimal valor, decimal iva, decimal total)
        {
            Tiempo = tiempo;
            Referencia = referencia;
            NitEmisor = nitEmisor;
            NitReceptor = nitReceptor;
            Valor = valor;
            Iva = iva;
            Total = total;
            CodigoAutorizacion = "";
        }
        public static string GenerarCodigoAutorizacion(DateTime fecha, int correlativo)
        {
            string fechaFormato = fecha.ToString("yyyyMMdd");
            string correlativoFormato = correlativo.ToString("D8"); 
            return $"{fechaFormato}{correlativoFormato}";
        }
    }
}