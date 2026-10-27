# Independent API probes

Checked 26 October 2026 using curl and Python assertions. These are API contract probes, not execution of the C# / Reqnroll suite. No tokens or authentication bodies are retained.

- 1 Login: token present; HTTP 200
- 2 Current user: expected identity; HTTP 200
- 3 Red hair: 25 users, complete dataset cross-check
- 4 Add user: all submitted nested fields; HTTP 201
- 5 iPhone 13 Pro: 1099.99
- 6 Pagination: one product, id 15, title and 799.99 confirmed
- 7 Update product 121: title echoed; HTTP 200
- 8 Cart: sum of percentages 62.11%; gross 121.91; net 106; effective discount 13.0506%
- 9 Delete cart: id, deletion flag and timestamp; HTTP 200
