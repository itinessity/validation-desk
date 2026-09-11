using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Concurrent;

var app = Setup.Create(args);
var validator = new Validator(Setup.Http(app), Setup.Fixture(app));
var store = new Store<Audit>("audit");
app.MapPost("/api/validate", async (Check input) =>
{
    var result = await validator.Check(input);
    var id = store.Save(result);
    return Results.Ok(new { id, result });
});
app.MapPost("/api/batch", async (Check[] inputs) =>
{
    if (inputs.Length is < 1 or > 10)
        throw new ArgumentException("Submit between 1 and 10 records.");
    var results = new List<object>();
    foreach (var input in inputs)
    {
        var result = await validator.Check(input);
        results.Add(new { id = store.Save(result), result });
    }

    return Results.Ok(results);
});
app.MapGet("/api/audit", () => store.List());
app.MapGet("/api/audit/{id}", (string id) => store.Get(id)is { } v ? Results.Ok(v) : Results.NotFound(new { error = "Audit record not found." }));
app.Run();
