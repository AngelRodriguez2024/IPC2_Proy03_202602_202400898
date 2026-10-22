namespace BackendSAT
{
    public class FacturaRechazada
    {
        public string Tiempo { get; set; }
        public string Referencia { get; set; }
        public bool EmisorInvalido { get; set; }
        public bool ReceptorInvalido { get; set; }
        public bool EmisorInexistente { get; set; }
        public bool ReceptorInexistente { get; set; }
        public bool IvaMalCalculado { get; set; }
        public bool TotalMalCalculado { get; set; }
        public bool ReferenciaDuplicada { get; set; }

        public FacturaRechazada() { }
    }
}