# ABC Pharmacy Medicine Tracker

This project was built as a small pharmacy operations dashboard for tracking stocked medicines, recording sales, and reviewing daily activity. The goal was to keep the workflow simple: staff can add medicines, monitor expiry and low-stock warnings, log sales, and review recent events without relying on a heavy database setup.

## What the app does

- View the current medicine inventory in one place
- Add new stock with brand, expiry date, quantity, price, and notes
- Search for medicines by name
- Highlight items that are near expiry or running low on stock
- Record sales while automatically reducing inventory levels
- Review the most recent operational events and warnings

## Project setup

- ASP.NET Core Web API backend
- Plain HTML, CSS, and JavaScript frontend
- JSON-based persistence in `App_Data` for a lightweight local deployment
- Simple single-page workflow designed for day-to-day pharmacy use

## Run locally

From the project folder:

```powershell
dotnet run --urls http://localhost:5080
```

Then open <http://localhost:5080> in a browser.

## API endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/medicines?search=` | List medicines, optionally filtered by name |
| GET | `/api/medicines/{id}` | Get one medicine |
| POST | `/api/medicines` | Add a medicine |
| GET | `/api/sales` | List sales, newest first |
| POST | `/api/sales` | Record a sale and decrement stock |
| GET | `/api/events?take=100` | List recent application events |

## Data files

- `App_Data/medicines.json` stores inventory records
- `App_Data/sales.json` stores completed sales
- `App_Data/events.json` stores information and warning events, retaining the newest 500 entries

This app was designed to be easy to run locally and easy to extend. For production use, the JSON store could be replaced with a transactional database and the app could be expanded with authentication and authorization.
