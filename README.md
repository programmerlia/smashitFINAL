

# Smash-IT Court Queueing and Reservation System

![Frontend](https://img.shields.io/badge/Frontend-ASP.NET_WebForms-blue)
![Backend](https://img.shields.io/badge/Backend-C%23%20%2F%20ASP.NET-purple)
![Database](https://img.shields.io/badge/Database-MS%20SQL%20Server-red)
![Payments](https://img.shields.io/badge/Payments-PayMongo-green)



## Description

A web-based **Court Queueing and Reservation System** built using **ASP.NET WebForms** and **MS SQL Server**.

The system allows administrators to manage walk-in players, approve reservations, and monitor active court sessions.  
Players can reserve courts online or join the queue.

LINK HERE -> [smashitdemo3324.somee.com](http://smashitdemo3324.somee.com)
## Creators

- **Isaiah Andrei Noda** – Project Leader  
- **Hannali Olayvar**  
- **Dean Lorenz Par**  
- **Mark Audrey Unira**  
- **Harvey Lafuente**


## Features

- **Admin Management** — Manage walk-ins, approve/reject reservations, assign courts  
- **Queueing System** — Paid priority queue and free-for-all mode  
- **Player Management** — Registration, profiles, payment tracking  
- **Reservation System** — User booking with admin-side control  
- **PayMongo Integration** — Checkout, verification, webhook confirmation
- <img width="1846" height="948" alt="image" src="https://github.com/user-attachments/assets/070d608a-c8cb-42d1-a92f-42e3c4f6864b" />  

# Project Documentation
📄 **Database Script, Diagrams, User & Data Flow**  
[Figma Board](https://www.figma.com/board/4AYIX1QipVPQXOklgNM3rI/dbase-stuff?node-id=0-1&t=yMUlPw4AoIaSnwTn-1)  


# Setup Guide

## 1. Clone Repository

```bash
git clone https://github.com/programmerlia/smashitFINAL.git
cd smashit_system
```

## 2. Open in Visual Studio

- Use **ASP.NET WebForms**
- Enable **IIS Express** or **Local IIS**
- Match the required **.NET Framework version**

## 3. Database Setup (MS SQL Server)

1. Create database: `dbsmashit`  
2. Run the **latest SQL script** from the project  
3. Verify tables and relationships

## 4. Configure Connection String

Update `web.config`:

```xml
<connectionStrings>
  <add name="SmashItConnection"
       connectionString="Data Source=YOUR_SERVER;Initial Catalog=dbsmashit;Integrated Security=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```


# PayMongo Configuration

## 1. Get API Keys

From the **PayMongo dashboard**, obtain:

- Secret Key  
- Public Key  
- Webhook Secret  

## 2. Add to `web.config`

```xml
<appSettings>
  <add key="PayMongoSecretKey" value="YOUR_SECRET_KEY" />
  <add key="PayMongoPublicKey" value="YOUR_PUBLIC_KEY" />
  <add key="PayMongoWebhookSecret" value="YOUR_WEBHOOK_SECRET" />

  <add key="PayMongoSuccessUrl" value="http://localhost:PORT/success.html" />
  <add key="PayMongoCancelUrl" value="http://localhost:PORT/cancel.html" />
</appSettings>
```

---

## Webhook Setup (ngrok – Local Testing Only)

PayMongo webhooks require a public HTTPS URL.  
For local testing, use **ngrok**.


1. Install ngrok (PowerShell – Admin):

```powershell
winget install ngrok.ngrok
```

2. Authenticate ngrok (one time only):

```powershell
ngrok config add-authtoken YOUR_AUTH_TOKEN
```

3. Run your project in Visual Studio and note the port  
(example: `http://localhost:5000`)

4. Start the tunnel:

```powershell
ngrok http YOUR_PORT
```

5. Copy the generated HTTPS URL and set it as your PayMongo webhook:

```powershell
https://YOUR-NGROK-URL/homepage/paymongo_webhook.ashx
```

# Running the System

Before running, ensure:

- SQL Server is running  
- Connection string is correct  
- PayMongo keys are configured  
- ngrok is running (for local webhook testing only)

Run the project via **Visual Studio (IIS Express)**.


# Dependencies

- ASP.NET WebForms (C#)  
- MS SQL Server  
- PayMongo API  
- ngrok (local webhook testing only)


# License

Licensed under the **MIT License** — see the `LICENSE` file for details.
