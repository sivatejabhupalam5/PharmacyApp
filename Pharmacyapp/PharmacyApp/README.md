# ABC Pharmacy Medicine Tracker

A single-page pharmacy inventory and sales application built with ASP.NET Core Web API and vanilla JavaScript. It helps pharmacy staff manage medicine stock, record sales, and review operational events.

## Features

- View medicines in an inventory grid
- Add medicines with name, notes, expiry date, quantity, price, and brand
- Search medicines by name
- Highlight medicines expiring within 30 days
- Highlight medicines with fewer than 10 units in stock
- Record medicine sales with stock validation and automatic quantity updates
- Store medicines, sales, and event logs as JSON files on the server
- Review information and warning events in the Event Log view

## Technology

- .NET 10 ASP.NET Core Web API
- C# controllers and services
- HTML, CSS, and JavaScript single-page frontend
- JSON file persistence in `App_Data`

## Run Locally

From the parent directory:

```powershell
dotnet run --project PharmacyApp --urls http://localhost:5080
```

Open <http://localhost:5080> in a browser.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/medicines?search=` | List medicines, optionally filtered by name |
| GET | `/api/medicines/{id}` | Get one medicine |
| POST | `/api/medicines` | Add a medicine |
| GET | `/api/sales` | List sales, newest first |
| POST | `/api/sales` | Record a sale and decrement stock |
| GET | `/api/events?take=100` | List recent application events |

## Data Files

- `App_Data/medicines.json` stores inventory records.
- `App_Data/sales.json` stores completed sales.
- `App_Data/events.json` stores information and warning events, retaining the newest 500 entries.

This application uses JSON persistence for a lightweight local deployment. For production use, replace the JSON store with a transactional database and add authentication and authorization.
