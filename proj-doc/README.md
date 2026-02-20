# Smash-IT Court Queueing and Reservation System

![Primary Language](https://img.shields.io/badge/Primary_Language-HTML-yellow)
![Backend Language](https://img.shields.io/badge/Backend-PHP-blue)
![Database](https://img.shields.io/badge/Database-MySQL-green)
![Hosting](https://img.shields.io/badge/Hosting-InfinityFree-orange)

## Description

A web-based Court Queueing and Reservation System that allows administrators to manage walk-in players, approve online reservations, and track active court sessions. Players can reserve courts online or join queues, with paid priority options available.

## Creators

- Isaiah Andrei Noda – _Project Leader_
- Hannali Olayvar
- Dean Lorenz Par
- Mark Audrey Unira
- Harvey Lafuente

## Features

- **Admin Management** — Add and manage walk-in players, approve or reject reservations, assign courts, and monitor active sessions.
- **Queueing System** — Paid queueing for priority access, free-for-all play when courts are open.
- **Player Management** — Register players, track profiles and payments.
- **Reservation Management** — Online reservations appear in the system but require admin approval.
- **Payment Tracking** — Upload and verify proof of payment, track payment status and amounts.
- **Sample Home Page**
  <img width="1880" height="908" alt="image" src="https://github.com/user-attachments/assets/adc2241e-be80-4d4e-b08a-1819c6c7e4db" />

## Project Documentation

You can view the full project documentation and diagrams here:  
📄 [View Relationship Diagram](proj-doc/relationship_diagram.png)

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/programmerlia/smashit_system.git
   ```

2. Navigate to the project directory:

   ```bash
   cd smashit_system
   ```

3. Setup the MySQL database in your local pc:
   1. Open **XAMPP** and start **MySQL**.
   2. Open **phpMyAdmin** in your browser: [http://localhost/phpmyadmin](http://localhost/phpmyadmin)
   3. In phpMyAdmin, click the **Import** tab.
   4. Choose the `setUpDB.sql` file from the project folder.
   5. Click **Go** to run the SQL file.

   > This will automatically create the database `dbsmashit` and all required tables.

## Usage

1. Open the project folder in **Visual Studio Code**.
2. Move project folder to **xampp/htdocs**
3. Open Xampp and run **apache and Mysql**
4. Go to the web and search **"http://localhost/smashit_system/index.php"**.

## Dependencies

- **HTML, CSS, Tailwind CSS, Bootstrap**
- **PHP 7+**
- **MySQL**
- **Hosting:** InfinityFree

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
