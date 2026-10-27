# STA coding challenge — DummyJSON API

C# API automation with Reqnroll (Gherkin), NUnit, HttpClient and System.Text.Json. Exactly **nine scenarios** map to the nine exercises in the supplied brief.

## Run

Install the .NET 8 SDK. Open this folder in VS Code with the C# extension, or open `tests/ApiTests.csproj` in a compatible Visual Studio installation.

```sh
dotnet restore tests/ApiTests.csproj
dotnet test tests/ApiTests.csproj --list-tests
dotnet test tests/ApiTests.csproj --configuration Release --logger "trx;LogFileName=api-tests.trx" --results-directory TestResults
```

The list command should discover nine tests. Internet access to NuGet and DummyJSON is required. GitHub Actions runs the same suite on pushes, pull requests and manual dispatch, and retains TRX results even on failure.

Optional: set `DUMMYJSON_BASE_URL` to an equivalent API deployment, with a trailing slash. The fixtures and paths target the DummyJSON contract. The published `emilys` / `emilyspass` credentials are public demo data, not private account credentials. Do not substitute real credentials into source control.

## Coverage

| Exercise | Request | Main checks |
|---|---|---|
| Auth a | POST /auth/login | 200, JSON, nonempty access token stored in scenario instance |
| Auth b | POST /auth/login then GET /auth/me | 200, bearer token, id 1, Emily Johnson |
| Users a | GET /users/filter?key=hair.color&value=Red&limit=0 | all returned hair colours, total, exact IDs against full users list |
| Users b | POST /users/add | 201, generated id, all supplied fields including nested hair/address/coordinates |
| Products a | GET /products/search?q=iPhone%2013%20Pro&limit=0 | exactly one exact title match, price 1099.99 |
| Products b | GET /products?limit=1&skip=14&select=id,title,price | single product, pagination metadata, id 15, Wooden Bathroom Sink With Mirror, 799.99 |
| Products c | PUT /products/121 | 200, id and updated title |
| Cart a | GET /products/{id}, POST /carts/add | 201, five distinct lines, quantities, percentages and independent totals |
| Cart b | DELETE /carts/1 | 200, id, isDeleted, parseable deletion timestamp |

Every request also checks JSON content type. Product price/title baselines were directly checked on 26 October 2026; public fixture data may change. Investigate drift before editing an expected value. Do not make expected prices equal the response under test just to obtain a pass.

## Structure and decisions

- `tests/Features/Challenge.feature`: readable acceptance scenarios, tagged by resource.
- `tests/Steps/ChallengeSteps.cs`: scenario-scoped state and domain assertions. A single binding class is deliberately modest for nine scenarios; split by resource and inject a shared scenario context if the suite grows.
- `tests/Support/ApiClient.cs`: HTTP transport, JSON requests, exact status checks, timeout, safe endpoint/status diagnostics and disposal.
- `.github/workflows/api-tests.yml`: CI execution and test-result artifact.

HttpClient is built into .NET and sufficient for this API-only scope. Reqnroll turns Gherkin into executable NUnit tests. NUnit supplies assertions and test execution support. System.Text.Json avoids an extra serializer dependency. JSON nodes keep the small, nested payloads easy to inspect; explicit typed DTOs would improve discoverability in a larger framework.

No static token or test ordering is used. Each scenario owns its client and state; the current-user scenario logs in independently. Cookies are disabled so it verifies bearer authentication. A client per scenario is sufficient for nine low-volume tests; a larger suite should reuse a managed handler/client pool while keeping authorization request-specific. All I/O is awaited. Requests/responses are disposed and tokens are never printed. There are no automatic retries that could disguise assertion failures. Execution is not explicitly parallelised against the public service.

## Assumptions and API limitations

1. **Simulated writes:** validate each mutation response. Never try to fetch a newly created user/cart, assert a title persists, or expect deletion to produce a later 404. The delete scenario uses existing cart 1 independently.
2. **“Delete a Card”:** interpreted as “delete a cart” from the resource heading and available endpoint.
3. **Fifteenth product:** default API ordering, skip 14 and limit 1. The products array contains exactly one item. The API still returns its normal pagination envelope; select limits the product fields, not the envelope.
4. **Red hair:** `limit=0` avoids the default page size. The returned total and exact identities are checked against a complete unfiltered dataset. This is a cross-endpoint consistency oracle, not proof that the underlying dataset itself is correct. A controlled product would use an approved fixture baseline as well.
5. **Discount percentage:** the wording does not specify whether it means the sum of line percentages or the effective cart-wide percentage. Both are checked and labelled. The sum is compared with independently fetched catalog percentages, so it is not a response compared with itself. A sum of percentages is not a meaningful overall cart saving and can exceed 100%.

### Discount calculation

Five distinct product IDs 1–5 are sent with quantities 1, 2, 1, 3, 2 (nine units). For each line:

- Gross = catalog price × requested quantity.
- Percentage = catalog discountPercentage, checked against the cart line.
- POST /carts/add currently rounds the discounted line amount to a whole currency unit. Expected net = Round(gross × (1 − percentage / 100), 0, AwayFromZero).
- Cart gross/net are independently summed from those expectations.
- Effective cart discount percentage = (gross − net) / gross × 100.

The added-cart field is `discountedPrice`; the cart-wide `discountedTotal` is money, not a percentage. Existing-cart examples can use different line field names. Whole-unit rounding is observed mock behaviour, not a suggested production financial policy. A one-cent line tolerance and a five-cent aggregate tolerance bound numerical discrepancies; percentage tolerance is derived from the same monetary bound. A change in the API rounding contract should fail for investigation.

## Validation status

Source structure and nine-scenario mapping have been reviewed. See `API-PROBE-RESULTS.md` if present for separately executed live API assertions. These do **not** constitute a C# test-suite pass.

A .NET 8 SDK was downloaded in the preparation environment, but `dotnet test` and direct MSBuild fail during process metadata lookup before compilation. Therefore package restore, C# compilation, generated Reqnroll bindings, discovery and NUnit execution remain unverified here. Run the commands above on a normal workstation or GitHub Actions before submission; resolve any failures and retain the resulting TRX. No passing C# run is claimed.

## Deliberate scope and next improvements

The brief asks for exactly nine scenarios, so no additional executable scenarios are included. Next priorities would be invalid credentials/missing token, invalid IDs, boundary pagination, malformed payloads, schema validation, timeout handling and focused unit tests of reusable calculation logic. Separate public-service outages from product failures before deciding to retry. Use controlled fixtures for reproducible regression tests in a real system.

## AI disclosure

OpenAI ChatGPT assisted in generating the framework, test code, documentation and review suggestions. This submission includes AI-generated code. The validation status above states what was and was not executed. The submitting candidate should review, run and understand the code before submission and should not claim independent authorship or unperformed verification.

## References

- https://dummyjson.com/docs/auth
- https://dummyjson.com/docs/users
- https://dummyjson.com/docs/products
- https://dummyjson.com/docs/carts
- https://docs.reqnroll.net/latest/integrations/nunit.html
- https://www.nuget.org/packages/Reqnroll.NUnit/3.3.4
