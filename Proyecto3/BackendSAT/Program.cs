using BackendSAT;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string rtuFilePath = "rtu.json";
string facturasFilePath = "facturas.json";
string correlativosFilePath = "correlativos.json";
string erroresFilePath = "errores.json"; 

List<Contribuyente> rtuLista = CargarDesdeJson<List<Contribuyente>>(rtuFilePath) ?? new List<Contribuyente>();
List<Factura> facturasAutorizadas = CargarDesdeJson<List<Factura>>(facturasFilePath) ?? new List<Factura>();
Dictionary<string, int> correlativosPorFecha = CargarDesdeJson<Dictionary<string, int>>(correlativosFilePath) ?? new Dictionary<string, int>();
List<FacturaRechazada> facturasRechazadas = CargarDesdeJson<List<FacturaRechazada>>(erroresFilePath) ?? new List<FacturaRechazada>();

void GuardarDatos()
{
    File.WriteAllText(rtuFilePath, JsonSerializer.Serialize(rtuLista, new JsonSerializerOptions { WriteIndented = true }));
    File.WriteAllText(facturasFilePath, JsonSerializer.Serialize(facturasAutorizadas, new JsonSerializerOptions { WriteIndented = true }));
    File.WriteAllText(correlativosFilePath, JsonSerializer.Serialize(correlativosPorFecha, new JsonSerializerOptions { WriteIndented = true }));
    File.WriteAllText(erroresFilePath, JsonSerializer.Serialize(facturasRechazadas, new JsonSerializerOptions { WriteIndented = true }));
}

T? CargarDesdeJson<T>(string path)
{
    if (!File.Exists(path)) return default;
    string json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<T>(json);
}

string ExtraerFechaRegex(string texto)
{
    var match = Regex.Match(texto, @"\d{2}/\d{2}/\d{4}");
    return match.Success ? match.Value : texto;
}

app.MapGet("/", () => "API de la SAT funcionando correctamente");

app.MapPost("/CargarRTU", (List<Contribuyente> listaContribuyentes) =>
{
    int nuevos = 0, actualizados = 0, invalidos = 0;
    foreach (var c in listaContribuyentes)
    {
        if (!Contribuyente.EsNitValido(c.Nit)) { invalidos++; continue; }
        var existente = rtuLista.FirstOrDefault(x => x.Nit == c.Nit);
        if (existente != null) { existente.Nombre = c.Nombre; actualizados++; }
        else { rtuLista.Add(new Contribuyente(c.Nit, c.Nombre)); nuevos++; }
    }
    GuardarDatos();
    return Results.Ok(new { CONTRIBUYENTES_NUEVOS = nuevos, CONTRIBUYENTES_ACTUALIZADOS = actualizados, NITS_INVALIDOS = invalidos });
});

app.MapPost("/ProcesarFacturas", (List<SolicitudFacturaDto> solicitudes) =>
{
    int aprobadas = 0, emisorInexistente = 0, receptorInexistente = 0;
    var detalleAprobaciones = new List<object>();

    foreach (var s in solicitudes)
    {
        var error = new FacturaRechazada { Tiempo = s.Tiempo, Referencia = s.Referencia };
        bool tieneError = false;

        if (!Contribuyente.EsNitValido(s.NitEmisor)) { error.EmisorInvalido = true; tieneError = true; }
        if (!Contribuyente.EsNitValido(s.NitReceptor)) { error.ReceptorInvalido = true; tieneError = true; }

        bool existeEmisor = rtuLista.Any(x => x.Nit == s.NitEmisor);
        bool existeReceptor = rtuLista.Any(x => x.Nit == s.NitReceptor);
        if (!existeEmisor && !error.EmisorInvalido) { error.EmisorInexistente = true; tieneError = true; emisorInexistente++; }
        if (!existeReceptor && !error.ReceptorInvalido) { error.ReceptorInexistente = true; tieneError = true; receptorInexistente++; }

        if (facturasAutorizadas.Any(x => x.Referencia == s.Referencia)) { error.ReferenciaDuplicada = true; tieneError = true; }
        decimal ivaEsperado = Math.Round(s.Valor * 0.12m, 2);
        if (Math.Abs(s.Iva - ivaEsperado) > 0.01m) { error.IvaMalCalculado = true; tieneError = true; }
        if (Math.Abs(s.Total - (s.Valor + s.Iva)) > 0.01m) { error.TotalMalCalculado = true; tieneError = true; }

        if (tieneError)
        {
            facturasRechazadas.Add(error);
        }
        else
        {
            DateTime fechaFactura = DateTime.Now;
            string claveFecha = fechaFactura.ToString("yyyyMMdd");
            string fechaExtraida = ExtraerFechaRegex(s.Tiempo);

            if (DateTime.TryParseExact(fechaExtraida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                fechaFactura = parsedDate;
                claveFecha = fechaFactura.ToString("yyyyMMdd");
            }

            if (!correlativosPorFecha.ContainsKey(claveFecha)) correlativosPorFecha[claveFecha] = 0;
            correlativosPorFecha[claveFecha]++;

            string codigoAprobacion = Factura.GenerarCodigoAutorizacion(fechaFactura, correlativosPorFecha[claveFecha]);
            var nuevaFactura = new Factura(s.Tiempo, s.Referencia, s.NitEmisor, s.NitReceptor, s.Valor, s.Iva, s.Total) { CodigoAutorizacion = codigoAprobacion };
            facturasAutorizadas.Add(nuevaFactura);
            aprobadas++;
            detalleAprobaciones.Add(new { referencia = s.Referencia, codigoAprobacion = codigoAprobacion });
        }
    }
    GuardarDatos();
    return Results.Ok(new { resultadoAutorizaciones = new { cantidadAutorizacionesAprobadas = aprobadas, cantidadAutorizacionesRechazadasPorNitEmisorInexistente = emisorInexistente, cantidadAutorizacionesRechazadasPorNitReceptorInexistente = receptorInexistente, detalleAprobaciones = detalleAprobaciones } });
});

app.MapPost("/Resetear", () =>
{
    rtuLista.Clear(); facturasAutorizadas.Clear(); correlativosPorFecha.Clear(); facturasRechazadas.Clear();
    if (File.Exists(rtuFilePath)) File.Delete(rtuFilePath);
    if (File.Exists(facturasFilePath)) File.Delete(facturasFilePath);
    if (File.Exists(correlativosFilePath)) File.Delete(correlativosFilePath);
    if (File.Exists(erroresFilePath)) File.Delete(erroresFilePath);
    return Results.Ok(new { mensaje = "Sistema reiniciado correctamente" });
});

app.MapGet("/ConsultaDatos", (string fechaInicio, string fechaFin, string? nit) =>
{
    DateTime.TryParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtInicio);
    DateTime.TryParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtFin);

    var facturasEnRango = facturasAutorizadas.Where(f =>
    {
        string fechaExtraida = ExtraerFechaRegex(f.Tiempo);
        if (DateTime.TryParseExact(fechaExtraida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtFactura))
            return dtFactura.Date >= dtInicio.Date && dtFactura.Date <= dtFin.Date;
        return false;
    }).ToList();

    if (!string.IsNullOrEmpty(nit))
        facturasEnRango = facturasEnRango.Where(f => f.NitEmisor == nit || f.NitReceptor == nit).ToList();

    return Results.Ok(new { totalFacturasAutorizadas = facturasEnRango.Count, montoTotalFacturado = facturasEnRango.Sum(f => f.Total), facturas = facturasEnRango });
});

app.MapGet("/ResumenIva", (string fecha) =>
{
    var facturasDelDia = facturasAutorizadas.Where(f => ExtraerFechaRegex(f.Tiempo) == fecha).ToList();
    var nits = facturasDelDia.Select(f => f.NitEmisor).Union(facturasDelDia.Select(f => f.NitReceptor)).Distinct();
    return Results.Ok(nits.Select(nit => new { nit = nit, ivaEmitido = facturasDelDia.Where(f => f.NitEmisor == nit).Sum(f => f.Iva), ivaRecibido = facturasDelDia.Where(f => f.NitReceptor == nit).Sum(f => f.Iva) }));
});

app.MapGet("/ResumenRango", (string fechaInicio, string fechaFin, bool conIva) =>
{
    DateTime.TryParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtInicio);
    DateTime.TryParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtFin);

    var facturasEnRango = facturasAutorizadas.Where(f =>
    {
        string fechaExtraida = ExtraerFechaRegex(f.Tiempo);
        if (DateTime.TryParseExact(fechaExtraida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtFactura))
            return dtFactura.Date >= dtInicio.Date && dtFactura.Date <= dtFin.Date;
        return false;
    }).ToList();

    var resumenPorFecha = facturasEnRango.GroupBy(f => ExtraerFechaRegex(f.Tiempo))
        .Select(g => new { fecha = g.Key, montoTotal = conIva ? g.Sum(f => f.Total) : g.Sum(f => f.Valor) });

    return Results.Ok(resumenPorFecha);
});

app.MapGet("/ReporteErrores", (string fechaInicio, string fechaFin) =>
{
    DateTime.TryParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtInicio);
    DateTime.TryParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtFin);

    var erroresEnRango = facturasRechazadas.Where(f =>
    {
        string fechaExtraida = ExtraerFechaRegex(f.Tiempo);
        if (DateTime.TryParseExact(fechaExtraida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtFactura))
            return dtFactura.Date >= dtInicio.Date && dtFactura.Date <= dtFin.Date;
        return false;
    }).ToList();

    var facturasCorrectasEnRango = facturasAutorizadas.Where(f =>
    {
        string fechaExtraida = ExtraerFechaRegex(f.Tiempo);
        if (DateTime.TryParseExact(fechaExtraida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtFactura))
            return dtFactura.Date >= dtInicio.Date && dtFactura.Date <= dtFin.Date;
        return false;
    }).ToList();

    return Results.Ok(new
    {
        totalRecibidas = erroresEnRango.Count + facturasCorrectasEnRango.Count,
        emisorInvalido = erroresEnRango.Count(e => e.EmisorInvalido),
        receptorInvalido = erroresEnRango.Count(e => e.ReceptorInvalido),
        emisorInexistente = erroresEnRango.Count(e => e.EmisorInexistente),
        receptorInexistente = erroresEnRango.Count(e => e.ReceptorInexistente),
        ivaMalCalculado = erroresEnRango.Count(e => e.IvaMalCalculado),
        totalMalCalculado = erroresEnRango.Count(e => e.TotalMalCalculado),
        referenciaDuplicada = erroresEnRango.Count(e => e.ReferenciaDuplicada),
        facturasSinErrores = facturasCorrectasEnRango.Count,
        emisoresUnicos = facturasCorrectasEnRango.Select(f => f.NitEmisor).Distinct().Count(),
        receptoresUnicos = facturasCorrectasEnRango.Select(f => f.NitReceptor).Distinct().Count()
    });
});

app.Run();