# Web-based information and reference system for travel agencies
This is a logical continuation of the previous pet-project in which I get trained in new technologies and gain experience in developing ASP.NET projects.

At this point, the system is capable of performing all the functions assigned to it. Of course it needs in-depth testing with debugging, but this may take too much time, which I can't afford at the moment. The development is planned to continue for mastering new technologies (in the form of a pet-project).

The system is published on Azure service (with SQL Server database migration to AzureSQL) under the address https://wanderlust-explorers.azurewebsites.net. I will be glad if you show interest in it).

## Short description
I am creating a software system for a travel agency (it will be part of my undergraduate thesis project). This system is more oriented on comfort of work of the manager of the tour agency, namely convenience and responsiveness when creating a new tour in the automated tour designer with the ability to search for hotels, transportation, points of interest and local activities (this convenience will be provided by third-party API services to facilitate the organization of the tour), as well as easy booking. For the convenience of tourists: search for tours, a simple process of ordering a tour as well as the ability to get support admin (using the built-in chat).

## Technologies
- ASP.NET (Blazor Server)
- C#
- HTML, CSS, JavaScript
- MS SQL Server (migration to AzureSQL) using ORM Entity Framework
- Azure
- API controllers, use of external API services
- SignalR to create a real-time support chat
- Hangfire for scheduling automated tasks

## Implemented functionality:
- Registration, authorization and authentication
- Full-featured Tour Builder with the ability to select hotels, transportation, points of interest and other local activities (using Amadeus API, Google Maps API and currency conversion service).
- Storing tours in the database and manipulating them (e.g. automatic deletion of tours after their completion)
- Possibility to search for tours by visited countries or by using the search bar
- Possibility to view the selected tour together with further bookings (including hotel room reservations)
- Service for sending notifications to users' emails (e.g. notifying the user about the upcoming start of a tour or notifying the tour manager with links to booking activities and transportation within the system) (using FluentEmail)
- Automatic scheduling of tasks such as sending notifications, exchange rate updates and manipulation of stored tours (using HangFire)
- User support in the form of a chat with the administrator (using SignalR)

## It is planned to implement:
- Multicultural interface
