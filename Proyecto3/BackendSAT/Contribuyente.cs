namespace BackendSAT
{
    public class Contribuyente
    {
        public string Nit { get; set; }
        public string Nombre { get; set; }

        public Contribuyente(string nit, string nombre)
        {
            Nit = nit;
            Nombre = nombre;
        }
        public static bool EsNitValido(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit)) return false;

            nit = nit.Trim().ToUpper().Replace("-", "");

            if (nit.Length < 2) return false;

            char digitoVerificador = nit[nit.Length - 1];
            string cuerpoNit = nit.Substring(0, nit.Length - 1);

            int suma = 0;
            int posicion = 2;

            for (int i = cuerpoNit.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(cuerpoNit[i])) return false;

                int valorDigito = int.Parse(cuerpoNit[i].ToString());
                suma += valorDigito * posicion;
                posicion++;
            }

            int modulo = suma % 11;
            int resta = 11 - modulo;
            int resultadoFinal = resta % 11;

            char digitoEsperado;
            if (resultadoFinal == 10)
            {
                digitoEsperado = 'K';
            }
            else
            {
                digitoEsperado = resultadoFinal.ToString()[0];
            }

            return digitoVerificador == digitoEsperado;
        }
    }
}