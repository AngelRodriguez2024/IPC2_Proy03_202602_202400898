using BackendSAT;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Almacenamiento en memoria
List<Contribuyente> rtuLista = new List<Contribuyente>();
List<Factura> facturasAutorizadas = new List<Factura>();

// Diccionario para controlar el correlativo por fecha (YYYYMMDD -> ultimo correlativo)
Dictionary<string, int> correlativosPorFecha = new Dictionary<string, int>();

// Endpoint de prueba
app.MapGet("/", () => "API de la SAT funcionando correctamente");

// 1. Endpoint Cargar RTU
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

// 2. Endpoint Procesar Solicitudes de Facturas
app.MapPost("/ProcesarFacturas", (List<SolicitudFacturaDto> solicitudes) =>
{
    int aprobadas = 0;
    int emisorInexistente = 0;
    int receptorInexistente = 0;

    var detalleAprobaciones = new List<object>();

    foreach (var s in solicitudes)
    {
        // Verificar existencia en RTU
        bool existeEmisor = rtuLista.Any(x => x.Nit == s.NitEmisor);
        bool existeReceptor = rtuLista.Any(x => x.Nit == s.NitReceptor);

        if (!existeEmisor) emisorInexistente++;
        if (!existeReceptor) receptorInexistente++;

        // Verificar referencia duplicada
        bool referenciaDuplicada = facturasAutorizadas.Any(x => x.Referencia == s.Referencia);

        // Validaciones de montos
        decimal ivaEsperado = Math.Round(s.Valor * 0.12m, 2);
        bool ivaCorrecto = Math.Abs(s.Iva - ivaEsperado) <= 0.01m;
        bool totalCorrecto = Math.Abs(s.Total - (s.Valor + s.Iva)) <= 0.01m;

        // Si cumple con todas las validaciones
        if (existeEmisor && existeReceptor && !referenciaDuplicada && ivaCorrecto && totalCorrecto)
        {
            // Parsear fecha del string (ej. "Guatemala, 15/01/2025 15:25 hrs.")
            DateTime fechaFactura = DateTime.Now;
            string claveFecha = fechaFactura.ToString("yyyyMMdd");

            // Extraer fecha si viene en formato estándar
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

            // Incrementar correlativo diario
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

    // 3. Endpoint para resetear datos a estado inicial
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

// 4. Endpoint Resumen de IVA por fecha
app.MapGet("/ResumenIva", (string fecha) =>
{
    // Filtrar facturas que coincidan con la fecha dada (ej. "15/01/2025")
    var facturasDelDia = facturasAutorizadas.Where(f => f.Tiempo.Contains(fecha)).ToList();

    // Obtener todos los NITs involucrados en ese día (emisores y receptores)
    var nits = facturasDelDia.Select(f => f.NitEmisor)
                             .Union(facturasDelDia.Select(f => f.NitReceptor))
                             .Distinct();

    var resumen = nits.Select(nit => new
    {
        nit = nit,
        ivaEmitido = facturasDelDia.Where(f => f.NitEmisor == nit).Sum(f => f.Iva),
        ivaRecibido = facturasDelDia.Where(f => f.NitReceptor == nit).Sum(f => f.Iva)
    });

    return Results.Ok(resumen);
});

// 5. Endpoint Consulta de Datos por Rango de Fechas y NIT
app.MapGet("/ConsultaDatos", (string fechaInicio, string fechaFin, string? nit) =>
{
    // Filtrar facturas que estén dentro del rango de texto/fechas
    var facturasEnRango = facturasAutorizadas.Where(f => 
        string.Compare(f.Tiempo, fechaInicio) >= 0 && 
        string.Compare(f.Tiempo, fechaFin) <= 0
    ).ToList();

    // Si se envía un NIT, filtramos las emitidas o recibidas por ese NIT
    if (!string.IsNullOrEmpty(nit))
    {
        facturasEnRango = facturasEnRango.Where(f => f.NitEmisor == nit || f.NitReceptor == nit).ToList();
    }

    int totalFacturas = facturasEnRango.Count;
    decimal montoTotal = facturasEnRango.Sum(f => f.Total);

    return Results.Ok(new
    {
        totalFacturasAutorizadas = totalFacturas,
        montoTotalFacturado = montoTotal,
        facturas = facturasEnRango
    });
});

// 6. Endpoint Resumen por Rango de Fechas (Total o Sin IVA)
app.MapGet("/ResumenRango", (string fechaInicio, string fechaFin, bool conIva) =>
{
    var facturasEnRango = facturasAutorizadas.Where(f => 
        string.Compare(f.Tiempo, fechaInicio) >= 0 && 
        string.Compare(f.Tiempo, fechaFin) <= 0
    ).ToList();

    var resumenPorFecha = facturasEnRango
        .GroupBy(f => f.Tiempo.Split(',')[1].Trim().Split(' ')[0]) // Agrupa por la fecha "dd/MM/yyyy"
        .Select(g => new
        {
            fecha = g.Key,
            montoTotal = conIva ? g.Sum(f => f.Total) : g.Sum(f => f.Valor)
        });

    return Results.Ok(resumenPorFecha);
});

app.Run();