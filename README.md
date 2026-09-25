# 2D MMORPG HSO

> Inspired by **Knight Age** by Teamobi.  
> This repository contains the **Web API project**.

# Team Member

| Full Name | Student ID | Role |
|-----------|------------|------|
| Lai Minh Duy | 2311553066 | Project Leader, Unity Developer, Backend Developer, UI/UX Designer |

---

# Project Overview

HSO is a 2D MMORPG project developed to study and implement a client–server architecture for online games.

The backend is divided into two main components:

Server — Maintains real-time game sessions and communicates with Unity clients through WebSocket.
Web API — Provides RESTful APIs for persistent data access and communicates with Microsoft SQL Server.

The Server does not communicate directly with the database. All database-related operations are handled through the Web API.

---

# System Architecture

The system uses the following communication flow:

```text
Unity Client
     │
     │ WebSocket
     ▼
Server
     │
     │ REST API
     ▼
Web API
     │
     │ Entity Framework Core
     ▼
SQL Server
     │
     │ Entity Framework Core
     ▼
Web API
     │
     │ REST API
     ▼
Server
     │
     │ WebSocket
     ▼
Unity Client
```

This architecture separates real-time game processing from persistent data management.

- WebSocket is used between the Unity Client and Server for real-time gameplay communication.
- REST API is used between the Server and Web API for database-related operations.
- Entity Framework Core is used by the Web API to communicate with SQL Server.

---

# Project Objectives

This project was developed to:

- Study the client–server architecture used in online games.
- Practice real-time communication using WebSocket.
- Practice RESTful API development with ASP.NET Core.
- Practice multithreading and asynchronous programming techniques.
- Implement server-side game management for a 2D MMORPG.
- Implement persistent data management using SQL Server.
- Separate real-time game processing from database operations.
- Serve as the backend infrastructure for the HSO MMORPG.
- Serve as a graduation thesis project.

---

# Technologies Used

## Frameworks

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core

## Programming Languages

- C#
- SQL

## Networking

- WebSocket
- RESTful API
- HTTP
- Binary packet communication
- JSON

## Database

- Microsoft SQL Server 2022

## Development Tools

- Visual Studio 2022
- SQL Server Management Studio 2022 (SSMS)
- Git
- GitHub

---

# Backend Components

## Server

The Server is responsible for real-time game processing and maintaining connected game clients.The Server communicates with Unity Clients using WebSocket.

Main responsibilities include:

- Managing WebSocket connections.
- Managing connected players.
- User authentication session management.
- Character creation.
- Character information management.
- Player movement synchronization.
- Other player synchronization.
- Mob management.
- Mob AI and state management.
- Mob synchronization.
- Map management.
- NPC management.
- Inventory management.
- Equipment management.
- Item data management.
- Real-time game state processing.
- Communicating with the Web API for persistent data operations.

## Web API

The Web API is responsible for database-related operations and persistent game data. The Web API communicates with the Server using RESTful HTTP APIs and communicates with SQL Server through Entity Framework Core.

Main responsibilities include:

- User registration.
- User authentication.
- Account management.
- Character creation and management.
- Character information retrieval.
- Inventory management.
- Equipment management.
- Item data management.
- Map data management.
- Mob data management.
- NPC data management.
- Database access through Entity Framework Core.

## Database

The project uses Microsoft SQL Server 2022 as the persistent database. The Web API is the only backend component that directly accesses SQL Server. The database schema is provided in: HSO.sql

The database stores information such as:

- Account data.
- Character data.
- Inventory data.
- Equipment data.
- Item data.
- Item attributes.
- Map data.
- Mob data.
- NPC data.
- Registration data.

---

# Project Structure

## Web API Project

```text
HSO_WebAPI/
│
├── Controllers/
├── Models/
├── Properties/
├── .dockerignore
├── .gitignore
├── Dockerfile
├── HSO.sql
├── HSO_WebAPI.csproj
├── HSO_WebAPI.http
├── HSO_WebAPI.sln
├── Program.cs
├── README.md
└── appsettings.json
```

## Server Project

```text
HSO_Server/
│
├── Controllers/
├── Managers/
├── Models/
├── Properties/
├── .dockerignore
├── .gitignore
├── Dockerfile
├── HSO_Server.csproj
├── HSO_Server.sln
└── README.md
```

# System Requirements

- Windows 10/11
- Microsoft SQL Server 2022
- SQL Server Management Studio 2022
- Visual Studio 2022
- .NET 9 SDK

# Installation Guide

## 1. Clone the repository

Clone both the Server and Web API repositories:

```bash
git clone https://github.com/mduy23042005/HSO_WebAPI.git
git clone https://github.com/mduy23042005/HSO_Server.git
```

The Unity Client is maintained separately from these backend repositories.

## 2. Configure SQL Server

- Open SQL Server Management Studio.
- Connect to your SQL Server instance.
- Create a new database for the project.
- Execute the following SQL script: HSO.sql
- Verify that the database and required tables have been created successfully.

## 3. Configure the Web API

- Open the HSO_WebAPI project in Visual Studio 2022.
- Configure the SQL Server connection string in: appsettings.json
- Replace the server and database information with your local SQL Server configuration.
- Build the project and configure the Web API to run on port: 55555

## 4. Run the Web API

- Start the HSO_WebAPI project.
- The Web API should be available on: http://localhost:55555
- Verify that the Web API starts successfully before running the Server.

## 5. Configure the Server

- Open the HSO_Server project in Visual Studio 2022.
- Configure the Server to use port: 55556
- The Server should also be configured to communicate with the Web API running on port 55555.
- Build the project after completing the configuration.

## 6. Run the Server

- Start the HSO_Server project.
- The Server should be available on: ws://localhost:55556

---

# Backend Communication

## Unity Client <-> Server

The Unity Client communicates with the Server using WebSocket.

This connection is used for real-time gameplay data such as:

- Player movement.
- Player state.
- Other player synchronization.
- Mob synchronization.
- Game state updates.
- Real-time gameplay events.

## Server <-> Web API

The Server communicates with the Web API using RESTful HTTP requests.

The Server uses the Web API when it needs to perform persistent data operations such as:

- Login.
- Registration.
- Loading character information.
- Loading inventory.
- Loading equipment.
- Loading item data.
- Loading map data.
- Loading mob data.
- Loading NPC data.
- Saving persistent player information.

## Web API <-> SQL Server

The Web API communicates with SQL Server using Entity Framework Core.

This layer is responsible for querying, inserting, updating, and deleting persistent game data.

## Example Data Flow

For example, when a player logs into the game:

```text
Unity Client
     │
     │ Login request
     │ WebSocket
     ▼
Server
     │
     │ REST API
     ▼
Web API
     │
     │ Entity Framework Core
     ▼
SQL Server
     │
     │ Account data
     ▼
Web API
     │
     │ REST API response
     ▼
Server
     │
     │ WebSocket
     ▼
Unity Client
```

The same architecture is used for other persistent game data operations.

---

# Current Features

## Account

- User registration.
- User authentication.
- Account management.

## Character

- Character creation.
- Character information management.
- Character appearance synchronization.
- Character state synchronization.

## Inventory & Equipment

- Inventory management.
- Equipment management.
- Item data management.
- Item attribute management.

## World

- Map data loading.
- Mob data loading.
- Mob management.
- Mob AI.
- Mob synchronization.
- NPC data loading.

## Multiplayer

- WebSocket communication.
- Multiple connected clients.
- Player synchronization.
- Real-time game state synchronization.

---

# Future Development

- Quest management API.
- Skill management API.
- Mail system API.
- Guild system API.
- Friend system API.
- More comprehensive character progression systems.
- Server performance optimization.
- Network optimization.
- Database optimization.
- Security improvements.
- Authentication and authorization improvements.
- Logging and monitoring improvements.

---

# License

This project was developed for educational and research purposes only, it is inspired by Knight Age by Teamobi and is not intended for commercial use.
