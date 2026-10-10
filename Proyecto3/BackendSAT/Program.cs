using BackendSAT;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

List<Contribuyente> rtuLista = new List<Contribuyente>();
List<Factura> facturasAutorizadas = new List<Factura>();

Dictionary<string, int> correlativosPorFecha = new Dictionary<string, int>();
app.MapGet("/", () => "API de la SAT funcionando correctamente");

app.MapPost("/CargarRTU", (List<Contribuyente> listaContribuyentes) =>
{
    int nuevos = 0;
    int actualizados = 0;
    int invalidos = 0;

    foreach (var c in listaContribuyentes)
    {
        if (!Contribuyente.EsNitValido(c.Nit))
        {
            invalidos++;
            continue;
        }

        var existente = rtuLista.FirstOrDefault(x => x.Nit == c.Nit);
        if (existente != null)
        {
            existente.Nombre = c.Nombre;
            actualizados++;
        }
        else
        {
            rtuLista.Add(new Contribuyente(c.Nit, c.Nombre));
            nuevos++;
        }
    }

    return Results.Ok(new
    {
        CONTRIBUYENTES_NUEVOS = nuevos,
        CONTRIBUYENTES_ACTUALIZADOS = actualizados,
        NITS_INVALIDOS = invalidos
    });
});

app.MapPost("/ProcesarFacturas", (List<SolicitudFacturaDto> solicitudes) =>
{
    int aprobadas = 0;
    int emisorInexistente = 0;
    int receptorInexistente = 0;

    var detalleAprobaciones = new List<object>();

    foreach (var s in solicitudes)
    {
        bool existeEmisor = rtuLista.Any(x => x.Nit == s.NitEmisor);
        bool existeReceptor = rtuLista.Any(x => x.Nit == s.NitReceptor);

        if (!existeEmisor) emisorInexistente++;
        if (!existeReceptor) receptorInexistente++;
        bool referenciaDuplicada = facturasAutorizadas.Any(x => x.Referencia == s.Referencia);

        decimal ivaEsperado = Math.Round(s.Valor * 0.12m, 2);
        bool ivaCorrecto = Math.Abs(s.Iva - ivaEsperado) <= 0.01m;
        bool totalCorrecto = Math.Abs(s.Total - (s.Valor + s.Iva)) <= 0.01m;

        if (existeEmisor && existeReceptor && !referenciaDuplicada && ivaCorrecto && totalCorrecto)
        {
            DateTime fechaFactura = DateTime.Now;
            string claveFecha = fechaFactura.ToString("yyyyMMdd");
            var partes = s.Tiempo.Split(',');
            if (partes.Length > 1)
            {
                string fechaTexto = partes[1].Replace("hrs.", "").Trim();
                if (DateTime.TryParseExact(fechaTexto, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    fechaFactura = parsedDate;
                    claveFecha = fechaFactura.ToString("yyyyMMdd");
                }
            }

            if (!correlativosPorFecha.ContainsKey(claveFecha))
            {
                correlativosPorFecha[claveFecha] = 0;
            }
            correlativosPorFecha[claveFecha]++;
            int correlativoHoy = correlativosPorFecha[claveFecha];

            string codigoAprobacion = Factura.GenerarCodigoAutorizacion(fechaFactura, correlativoHoy);

            var nuevaFactura = new Factura(s.Tiempo, s.Referencia, s.NitEmisor, s.NitReceptor, s.Valor, s.Iva, s.Total)
            {
                CodigoAutorizacion = codigoAprobacion
            };

            facturasAutorizadas.Add(nuevaFactura);
            aprobadas++;

            detalleAprobaciones.Add(new
            {
                referencia = s.Referencia,
                codigoAprobacion = codigoAprobacion
            });
        }
    }

    app.MapPost("/Resetear", () =>
    {
        rtuLista.Clear();
        facturasAutorizadas.Clear();
        correlativosPorFecha.Clear();
        
        return Results.Ok(new { mensaje = "Sistema reiniciado correctamente" });
    });

    return Results.Ok(new
    {
        resultadoAutorizaciones = new
        {
            cantidadAutorizacionesAprobadas = aprobadas,
            cantidadAutorizacionesRechazadasPorNitEmisorInexistente = emisorInexistente,
            cantidadAutorizacionesRechazadasPorNitReceptorInexistente = receptorInexistente,
            detalleAprobaciones = detalleAprobaciones
        }
    });
});

app.Run();