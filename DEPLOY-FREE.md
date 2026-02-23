# Free deploy: show your boss the site

Use **Render.com** (free PostgreSQL + free Web Services) so everything is free. Free tier may spin down after ~15 min idle—**open the site 1–2 minutes before the demo** so it’s warm.

---

## 1. Push your code to GitHub

Make sure your repo is on GitHub (you already use it).

---

## 2. Create a Render account and PostgreSQL

1. Go to [render.com](https://render.com) and sign up (GitHub login is fine).
2. **Dashboard** → **New +** → **PostgreSQL**.
3. **Name:** e.g. `denolite-db`. **Region:** pick one near you. **Create Database**.
4. When it’s ready, open the DB and copy:
   - **Internal Database URL** → use as `ConnectionStrings__DefaultConnection` in the **API** Web Service env vars (step 3). The API runs on Render and connects with this.
   - **External Database URL** → use when running migrations from your PC (step 4). Your machine is outside Render, so it needs this URL to reach the DB.

---

## 3. Deploy the API (Web Service)

1. **New +** → **Web Service**.
2. **Connect repository:** choose your GitHub repo. **Connect**.
3. **Name:** e.g. `denolite-api`.
4. **Region:** same as the database.
5. **Runtime:** **Docker**.
6. **Dockerfile path:** `DenoLite.Api/Dockerfile`.
7. **Instance type:** **Free**.
8. **Advanced** → **Environment Variables.** Add these (values from your `DenoLite.Api/.env.production` where noted; **never commit that file**):

   | Key | Where to get the value |
   |-----|------------------------|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__DefaultConnection` | **Render Internal Database URL** from step 2 (do *not* use the value from .env.production — that’s your local DB) |
   | `Jwt__Key` | Copy from `.env.production` |
   | `Jwt__Issuer` | After first deploy: your API URL (e.g. `https://denolite-api.onrender.com`) |
   | `Jwt__Audience` | Same as `Jwt__Issuer` |
   | **Email (choose one)** | |
   | `Resend__ApiKey` | **Recommended on Render.** Sign up at [resend.com](https://resend.com), create an API key, add your domain (or use `onboarding@resend.dev` as `Email__FromEmail` for testing). If set, the app uses Resend’s HTTP API instead of SMTP (avoids “operation has timed out” when Render blocks SMTP). |
   | `Email__FromEmail` | Sender address (e.g. `onboarding@resend.dev` for Resend test, or your verified domain). |
   | `Email__FromName` | Sender name (e.g. `DenoLite`). |
   | `Email__Host` | Only if **not** using Resend (SMTP host). |
   | `Email__Port` | Only if using SMTP (e.g. `587`). |
   | `Email__UseSsl` | Only if using SMTP (e.g. `true`). |
   | `Email__Username` | Only if using SMTP. |
   | `Email__Password` | Only if using SMTP. |
   | `Otp__Secret` | Copy from `.env.production` |
   | `Google__ClientId` | Copy from `.env.production` |
   | `Google__ClientSecret` | Copy from `.env.production` |
   | `WebApp__BaseUrl` | **Your deployed Web app URL** (e.g. `https://denolite-web.onrender.com`). Required so after Google login the API redirects to the Web app, not localhost. |

   For Google login: in Google Cloud Console add **Authorised redirect URI** `https://YOUR-API-URL/api/auth/google-callback` (e.g. `https://denolite.onrender.com/api/auth/google-callback`). Set `WebApp__BaseUrl` above to your Web URL so the API redirects back to the site after login.

   **Connection string:** Key must be exactly `ConnectionStrings__DefaultConnection` (two underscores). Value: paste the **Internal Database URL** from Render with **no quotes** around it. If you get "Format of the initialization string does not conform to specification", use the **key=value** form instead of the URI: `Host=HOST;Port=5432;Database=DATABASE;Username=USER;Password=PASSWORD;SSL Mode=Require` (get HOST, DATABASE, USER, PASSWORD from your Render DB info).

   **Using Resend for email (recommended on Render):** See [§4b. Configure Resend on the API](#4b-configure-resend-on-the-api-so-verification-emails-work-on-render) below.

9. **Create Web Service.** Wait for the first deploy. Then copy the service URL (e.g. `https://denolite-api.onrender.com`).
10. **Environment** → set `Jwt__Issuer` and `Jwt__Audience` to that URL if you used a placeholder. **Save changes** (triggers a redeploy).

---

## 4. Run database migrations (from your PC)

Use the **External** Database URL from step 2 (so your machine can reach the DB). **Important:** pass it with `--connection` so the EF tools use Render's DB and not your local one from appsettings.Development.json. Use **single quotes** around the URI so PowerShell doesn't drop `=require` (e.g. `'...?sslmode=require'`). Or use the key=value form below.

```powershell
cd c:\Denis_dotnet\StudyProjects\DenoLite

dotnet ef database update --project DenoLite.Infrastructure --startup-project DenoLite.Api --connection 'postgresql://USER:PASSWORD@HOST.frankfurt-postgres.render.com/DATABASE?sslmode=require'
```

Or with key=value (External host = full hostname with `.frankfurt-postgres.render.com`):

```powershell
dotnet ef database update --project DenoLite.Infrastructure --startup-project DenoLite.Api --connection "Host=HOST.frankfurt-postgres.render.com;Port=5432;Database=DATABASE;Username=USER;Password=PASSWORD;SSL Mode=Require"
```

If it says "already up to date" but the API still reports missing tables, reset and re-apply (use single quotes for URI):

```powershell
dotnet ef database update 0 --project DenoLite.Infrastructure --startup-project DenoLite.Api --connection 'postgresql://YOUR_EXTERNAL_URL?sslmode=require'
dotnet ef database update --project DenoLite.Infrastructure --startup-project DenoLite.Api --connection 'postgresql://YOUR_EXTERNAL_URL?sslmode=require'
```

---

## 4b. Configure Resend on the API (so verification emails work on Render)

Render often blocks outbound SMTP, so the app can use **Resend** instead: you add an API key and sender address on the API service; the app then sends email over HTTPS.

### Step 1: Get a Resend API key

1. Go to [resend.com](https://resend.com) and sign up (free tier is enough).
2. In the dashboard, open **API Keys** and click **Create API Key**.
3. Name it (e.g. `DenoLite Render`) and copy the key. It looks like `re_xxxxxxxxxxxx`. You won’t see it again, so save it somewhere safe.

### Step 2: Open the API service env vars on Render

1. In [Render Dashboard](https://dashboard.render.com), open your **API** Web Service (e.g. `denolite-api`).
2. In the left sidebar click **Environment**.
3. You’ll see the list of environment variables. You will **add** three and **remove** four (if they exist).

### Step 3: Add these three variables

Click **Add Environment Variable** and add each of these. Use the **key** exactly as written (including two underscores).

| Key | Value | Notes |
|-----|--------|--------|
| `Resend__ApiKey` | Your Resend API key (e.g. `re_xxxxxxxxxxxx`) | Paste the key you copied in Step 1. |
| `Email__FromEmail` | `onboarding@resend.dev` (for testing) | Resend’s test sender; no domain setup needed. For production, verify your own domain in Resend and use e.g. `noreply@yourdomain.com`. |
| `Email__FromName` | `DenoLite` | Shown as the sender name in the inbox. |

So you should have:

- **Resend__ApiKey** = `re_xxxx...`
- **Email__FromEmail** = `onboarding@resend.dev`
- **Email__FromName** = `DenoLite`

### Step 4: Remove these SMTP variables (if present)

When the app uses Resend, it **ignores** SMTP settings. You can leave them and they won’t be used, but to avoid confusion and keep secrets off the dashboard, **remove** these from the API service if you added them earlier:

| Remove this key | Why |
|-----------------|-----|
| `Email__Host` | Only used for SMTP. |
| `Email__Port` | Only used for SMTP. |
| `Email__UseSsl` | Only used for SMTP. |
| `Email__Username` | Only used for SMTP. |
| `Email__Password` | Only used for SMTP. |

To remove: in the Environment list, use the trash/delete icon next to each of these keys. Do **not** remove `Email__FromEmail` or `Email__FromName` — those are still used by Resend.

### Step 5: Save and redeploy

Click **Save Changes**. Render will redeploy the API. After the deploy finishes, register or use “Resend code” on the Verify Email page; the email should be sent via Resend and arrive in the inbox (or spam).

**Production:** To send from your own domain (e.g. `noreply@yourdomain.com`), in Resend add your domain, add the DNS records they show, then set `Email__FromEmail` to that address and save again.

---

## 5. Deploy the Web app (second Web Service)

1. **New +** → **Web Service**.
2. Same repo. **Name:** e.g. `denolite-web`.
3. **Runtime:** **Docker**. **Dockerfile path:** `DenoLite.Web/Dockerfile`.
4. **Instance type:** **Free**.
5. **Environment Variables:**

   | Key | Value |
   |-----|--------|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `Api__BaseUrl` | *(API URL from step 3, e.g. `https://denolite-api.onrender.com`)* |

6. **Create Web Service.** Copy the Web URL (e.g. `https://denolite-web.onrender.com`).

---

## 6. CORS on the API

1. Open the **API** Web Service on Render → **Environment**.
2. Add:
   - **Key:** `Cors__AllowedOrigins`
   - **Value:** your Web URL, e.g. `https://denolite-web.onrender.com` (multiple URLs: separate with `;`).

The API reads this from config so the Web app can call it.

---

## 7. Demo checklist

- [ ] Migrations run (step 4).
- [ ] API env vars set, including JWT and DB URL (step 3).
- [ ] Web env var `Api__BaseUrl` = API URL (step 5).
- [ ] CORS allows the Web URL (step 6).
- [ ] **1–2 minutes before the demo:** open the **Web** URL in the browser so the free instance wakes up (no “loading” delay in front of your boss).

---

## If deploy fails with "Exited with status 139"

Exit 139 often means the process was killed (e.g. out of memory) or a runtime crash.

1. **Env var names** – In Render use **two underscores** for nested config: `Jwt__Issuer`, `Jwt__Audience`, `ConnectionStrings__DefaultConnection` (not `Jwt_Issuer`). Single underscore does not map to `Jwt:Issuer`.
2. **Check full logs** – In Render open **Logs** for the service and scroll up. Look for "Out of memory", "Killed", or an exception message before the exit.
3. **GC limit** – The API Dockerfile sets `DOTNET_GCHeapHardLimit` to reduce OOM risk on the free tier. Commit and redeploy.
4. **Try .NET 9** – If it still fails, switch the API (and Web) to .NET 9 for deploy: in both Dockerfiles use `sdk:9.0` and `aspnet:9.0`, and set `TargetFramework` to `net9.0` in `DenoLite.Api.csproj` and `DenoLite.Web.csproj` (and downgrade package versions to 9.x). Then redeploy.

---

## If Render Docker build fails (.NET 10)

If the build fails with an error about the .NET 10 image, change both Dockerfiles to use **9.0** and temporarily set **TargetFramework** to `net9.0` in:

- `DenoLite.Api/DenoLite.Api.csproj`
- `DenoLite.Web/DenoLite.Web.csproj`

Then redeploy.

---

## Alternative: Azure free + free Postgres

- Create **2 × App Service** (Free F1): one for API, one for Web.
- Use a **free PostgreSQL** at [Neon](https://neon.tech) or [Supabase](https://supabase.com).
- Set `ConnectionStrings__DefaultConnection` (Neon/Supabase URL) and JWT/Email in the API App Service; set `Api__BaseUrl` in the Web App Service.
- Publish from Visual Studio (Publish → Azure) or `dotnet publish` + zip deploy.
- Run migrations from your PC using the Neon/Supabase connection string.

No Docker needed; good if you prefer Azure and only need a quick demo.
