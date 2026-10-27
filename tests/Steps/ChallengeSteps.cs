using System.Globalization;
using System.Text.Json.Nodes;
using ApiTests.Support;
using NUnit.Framework;
using Reqnroll;

namespace ApiTests.Steps;

[Binding]
public sealed class ChallengeSteps
{
    private readonly ApiClient api = new();
    private JsonNode response = null!;
    private JsonNode submitted = null!;
    private string? accessToken;
    private readonly Dictionary<int, JsonNode> catalog = new();
    private readonly Dictionary<int, int> quantities = new() { [1] = 1, [2] = 2, [3] = 1, [4] = 3, [5] = 2 };
    private static int I(JsonNode n, string key) => n[key]!.GetValue<int>();
    private static decimal D(JsonNode n, string key) => n[key]!.GetValue<decimal>();
    private static string S(JsonNode n, string key) => n[key]!.GetValue<string>();
    private static decimal Number(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [AfterScenario]
    public void Cleanup() { accessToken = null; api.Dispose(); }

    [Given("I authenticate with the documented demo credentials")]
    [When("I authenticate with the documented demo credentials")]
    public async Task Login()
    {
        response = await api.Send(HttpMethod.Post, "auth/login", 200,
            new { username = "emilys", password = "emilyspass", expiresInMins = 30 });
        accessToken = S(response, "accessToken");
        Assert.That(accessToken, Is.Not.Null.And.Not.Empty);
    }

    [Then("an access token is stored for this scenario")]
    public void TokenStored() => Assert.That(accessToken, Is.Not.Null.And.Not.Empty);

    [When("I request the current authenticated user")]
    public async Task CurrentUser()
    {
        Assert.That(accessToken, Is.Not.Null.And.Not.Empty);
        response = await api.Send(HttpMethod.Get, "auth/me", 200, token: accessToken);
    }

    [Then("the user is {int} named {string} {string}")]
    public void UserIdentity(int id, string first, string last)
    {
        Assert.That(I(response, "id"), Is.EqualTo(id));
        Assert.That(S(response, "firstName"), Is.EqualTo(first));
        Assert.That(S(response, "lastName"), Is.EqualTo(last));
    }

    [When("I filter users with red hair")]
    public async Task FilterUsers() => response = await api.Send(HttpMethod.Get,
        "users/filter?key=hair.color&value=Red&limit=0", 200);

    [Then("the count and identities match the complete user dataset")]
    public async Task CountUsers()
    {
        var all = await api.Send(HttpMethod.Get, "users?limit=0", 200);
        var allUsers = all["users"]!.AsArray();
        Assert.That(allUsers.Count, Is.EqualTo(I(all, "total")), "Reference dataset must be complete");
        var expected = allUsers.Where(x => S(x!["hair"]!, "color") == "Red").Select(x => I(x!, "id")).ToArray();
        var actual = response["users"]!.AsArray();
        Assert.That(actual.All(x => S(x!["hair"]!, "color") == "Red"), Is.True);
        Assert.That(actual.Select(x => I(x!, "id")), Is.EquivalentTo(expected));
        Assert.That(actual.Count, Is.EqualTo(I(response, "total")));
        Assert.That(I(response, "total"), Is.EqualTo(expected.Length));
        TestContext.Progress.WriteLine($"Verified red-haired users: {expected.Length}");
    }

    [When("I add a user with all required nested details")]
    public async Task AddUser()
    {
        submitted = JsonNode.Parse("""
        {"firstName":"Alex","lastName":"Tester","age":30,"gender":"male",
         "hair":{"color":"Red","type":"Curly"},
         "address":{"address":"10 Test Street","city":"Manchester","state":"Greater Manchester",
         "stateCode":"MAN","postalCode":"M1 1AA","country":"United Kingdom",
         "coordinates":{"lat":53.4808,"lng":-2.2426}}}
        """)!;
        response = await api.Send(HttpMethod.Post, "users/add", 201, submitted);
    }

    [Then("the created response contains all submitted user details")]
    public void UserCreated()
    {
        Assert.That(I(response, "id"), Is.GreaterThan(0));
        foreach (var field in submitted.AsObject())
            Assert.That(JsonNode.DeepEquals(response[field.Key], field.Value), Is.True,
                $"Submitted field differs: {field.Key}");
    }

    [When("I search for {string}")]
    public async Task Search(string title) => response = await api.Send(HttpMethod.Get,
        $"products/search?q={Uri.EscapeDataString(title)}&limit=0", 200);

    [Then(@"the exact matching product costs (\d+\.\d+)")]
    public void PhonePrice(string price)
    {
        var matches = response["products"]!.AsArray().Where(x => S(x!, "title") == "iPhone 13 Pro").ToArray();
        Assert.That(matches, Has.Length.EqualTo(1), "Require one exact title match");
        Assert.That(D(matches[0]!, "price"), Is.EqualTo(Number(price)));
    }

    [When("I request one product after skipping fourteen products")]
    public async Task Page() => response = await api.Send(HttpMethod.Get,
        "products?limit=1&skip=14&select=id,title,price", 200);

    [Then(@"only product (\d+) titled ""([^""]*)"" priced (\d+\.\d+) is returned")]
    public void Fifteenth(int id, string title, string price)
    {
        var products = response["products"]!.AsArray();
        Assert.That(products, Has.Count.EqualTo(1));
        Assert.That(I(response, "skip"), Is.EqualTo(14));
        Assert.That(I(response, "limit"), Is.EqualTo(1));
        var product = products[0]!;
        Assert.That(I(product, "id"), Is.EqualTo(id));
        Assert.That(S(product, "title"), Is.EqualTo(title));
        Assert.That(D(product, "price"), Is.EqualTo(Number(price)));
        Assert.That(product.AsObject().Select(p => p.Key), Is.EquivalentTo(new[] { "id", "title", "price" }));
    }

    [When("I update product {int} title to {string}")]
    public async Task Update(int id, string title) => response = await api.Send(HttpMethod.Put,
        $"products/{id}", 200, new { title });

    [Then("product {int} has title {string} in the update response")]
    public void Updated(int id, string title)
    {
        Assert.That(I(response, "id"), Is.EqualTo(id));
        Assert.That(S(response, "title"), Is.EqualTo(title));
    }

    [Given("five distinct catalog products with mixed quantities")]
    public async Task Catalog()
    {
        foreach (var id in quantities.Keys)
            catalog[id] = await api.Send(HttpMethod.Get, $"products/{id}", 200);
    }

    [When("I add those products to a cart")]
    public async Task AddCart() => response = await api.Send(HttpMethod.Post, "carts/add", 201,
        new { userId = 1, products = quantities.Select(p => new { id = p.Key, quantity = p.Value }).ToArray() });

    [Then("the product discount percentages and their sum match the catalog")]
    public void Percentages()
    {
        var lines = response["products"]!.AsArray();
        Assert.That(I(response, "id"), Is.GreaterThan(0));
        Assert.That(I(response, "userId"), Is.EqualTo(1));
        Assert.That(lines, Has.Count.EqualTo(5));
        Assert.That(I(response, "totalProducts"), Is.EqualTo(5));
        Assert.That(I(response, "totalQuantity"), Is.EqualTo(quantities.Values.Sum()));
        Assert.That(lines.Select(x => I(x!, "id")), Is.EquivalentTo(quantities.Keys));
        foreach (var line in lines)
        {
            var id = I(line!, "id");
            Assert.That(I(line!, "quantity"), Is.EqualTo(quantities[id]));
            Assert.That(D(line!, "price"), Is.EqualTo(D(catalog[id], "price")));
            Assert.That(D(line!, "discountPercentage"), Is.InRange(0m, 100m));
            Assert.That(D(line!, "discountPercentage"), Is.EqualTo(D(catalog[id], "discountPercentage")));
        }
        var expected = catalog.Values.Sum(p => D(p, "discountPercentage"));
        var actual = lines.Sum(p => D(p!, "discountPercentage"));
        Assert.That(actual, Is.EqualTo(expected));
        TestContext.Progress.WriteLine($"Sum of five line discount percentages: {actual}% (not a cart-wide rate)");
    }

    [Then("the effective cart discount percentage agrees with the monetary totals")]
    public void EffectiveDiscount()
    {
        var lines = response["products"]!.AsArray();
        decimal gross = 0, expectedNet = 0;
        foreach (var line in lines)
        {
            var id = I(line!, "id");
            var lineGross = D(catalog[id], "price") * quantities[id];
            var lineNet = decimal.Round(lineGross * (1 - D(catalog[id], "discountPercentage") / 100m), 0,
                MidpointRounding.AwayFromZero);
            Assert.That(D(line!, "total"), Is.EqualTo(lineGross).Within(0.01m));
            // POST /carts/add rounds each discounted line to whole currency units.
            // This is observed mock behaviour, not a recommended financial rounding policy.
            Assert.That(D(line!, "discountedPrice"), Is.EqualTo(lineNet).Within(0.01m));
            gross += lineGross;
            expectedNet += lineNet;
        }
        Assert.That(D(response, "total"), Is.EqualTo(gross).Within(0.01m));
        Assert.That(D(response, "discountedTotal"), Is.EqualTo(expectedNet).Within(0.05m));
        var expectedPercent = (gross - expectedNet) / gross * 100m;
        var actualPercent = (D(response, "total") - D(response, "discountedTotal")) / D(response, "total") * 100m;
        Assert.That(actualPercent, Is.EqualTo(expectedPercent).Within(0.05m / gross * 100m));
        TestContext.Progress.WriteLine($"Effective cart discount: {actualPercent:F4}%");
    }

    [When("I delete existing cart {int}")]
    public async Task Delete(int id) => response = await api.Send(HttpMethod.Delete, $"carts/{id}", 200);

    [Then("cart {int} is marked deleted with a valid timestamp")]
    public void Deleted(int id)
    {
        Assert.That(I(response, "id"), Is.EqualTo(id));
        Assert.That(response["isDeleted"]!.GetValue<bool>(), Is.True);
        Assert.That(DateTimeOffset.TryParse(S(response, "deletedOn"), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out _), Is.True);
    }
}
