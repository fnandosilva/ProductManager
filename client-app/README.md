# ProductManager.Client

Angular frontend for the ProductManager API, built with Angular Material.

## Features

- **Login** — JWT-based authentication against the `ProductManager.WebAPI` backend, with a friendly Material UI, reactive form validation, and inline error handling. User registration is handled outside this app (e.g. directly against `POST /api/auth/register`).
- **Route guards** — Unauthenticated users are redirected to `/login`; authenticated users are redirected away from `/login` straight to `/products`.
- **Auth interceptor** — Automatically attaches the JWT bearer token to every API request and logs the user out if a request comes back `401 Unauthorized`.
- **Products dashboard**
  - Paginated table listing all products (ID, name, description, price, stock).
  - Search products by name.
  - Filter products by stock range (min/max).
  - Create / edit product dialog with validation matching the backend rules.
  - Add-to-stock / decrement-stock dialogs.
  - Delete confirmation dialog.
  - Low-stock visual indicator.

## Prerequisites

- Node.js 20+ (Node 24 recommended) and npm.
- The `ProductManager.WebAPI` backend running locally (defaults to `https://localhost:7228`, see `proxy.conf.json`).

## Getting started

```bash
cd product-manager-client
npm install
npm start
```

`npm start` runs `ng serve`, which serves the app at `http://localhost:4200` and proxies any request to `/api/*` to the backend (configured in `proxy.conf.json`), so there is no CORS configuration to worry about in development.

> If your backend runs on a different port, update the `target` in `proxy.conf.json` accordingly.

Open `http://localhost:4200` in your browser. You'll land on the **Login** page — sign in with an existing account to access the products dashboard. (New accounts are created outside this app, e.g. via `POST /api/auth/register`.)

## Project structure

```
src/app/
├── core/
│   ├── guards/          # authGuard, guestGuard
│   ├── interceptors/    # authInterceptor (attaches JWT, handles 401s)
│   ├── models/          # Product, Auth request/response DTOs
│   ├── services/        # AuthService, ProductService
│   └── utils/           # API error message helper
├── features/
│   ├── auth/
│   │   └── login/
│   └── products/
│       ├── product-list/         # main dashboard (table, search, filters)
│       ├── product-form-dialog/  # create / edit dialog
│       └── stock-dialog/         # add / decrement stock dialog
├── layout/
│   └── shell/           # authenticated app shell (toolbar, user menu, logout)
└── shared/
    └── confirm-dialog/  # reusable confirmation dialog (used for delete)
```

## Available scripts

| Command         | Description                                      |
| --------------- | ------------------------------------------------- |
| `npm start`     | Serve the app locally with the API dev proxy.     |
| `npm run build` | Production build (output in `dist/`).             |
| `npm test`      | Run unit tests.                                   |

## Configuration

The API base URL is set in `src/environments/environment.ts` / `environment.development.ts` as `apiUrl: '/api'`, which relies on the dev proxy (or, in production, on the app being served from the same origin/reverse proxy as the API). If you deploy the frontend separately from the API, update `apiUrl` to the full API URL and make sure the corresponding origin is added to the backend's `Cors:AllowedOrigins` configuration.
