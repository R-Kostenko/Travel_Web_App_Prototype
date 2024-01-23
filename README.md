# Travel_App_Web
This is a logical continuation of the previous pet-project in which I get trained in new technologies and gain experience in developing ASP.NET projects.

As you might have noticed in the repository name, this is a prototype of my project, which is far from complete. I didn't spend a lot of time on the design of the site, but I will do it closer to the logical completion of the project.

## Short description
I am creating a software system for a travel agency (it will be part of my undergraduate thesis project). This system is more oriented on comfort of work of the administrator of the tour company, namely convenience and responsiveness when creating a new tour in the automated tour designer (this convenience will be provided by third-party API services to facilitate the organization of the tour), as well as the ability of the system to independently book hotel rooms and buses for the entire tour. For the convenience of users: search for tours, communication with the administration (using the built-in chat).

## Technologies
- ASP.NET (Blazor)
- C#
- HTML, CSS
- Entity Framework
- MS SQL Server
- API controllers external API services
- SignalR
- Quartz.NET or Hangfire for scheduling automated tasks

## Implemented functionality:
- Registration, authorization and authentication
- Tour builder (no selection of existing hotels and buses yet)
- Inserting and storing tours in the database
- Tour preview

## It is planned to implement:
- implement the full functionality of the tour designer
  - add the ability to book a bus for a long tour (using a third-party API service)
  - add the ability to get data on hotels of different classes in the visited cities (using third-party API service)
- add full-fledged tour search (user location will be used)
- implement the ability to book a place in a tour (automatic booking of places in hotels using third-party API services)
- implement user support in the form of a chat with the administrator (SignalR)
- add a system for sending emails from the site mail to confirm user actions and regular order-related notifications
- implement a system for regular notifications to users (using the previous paragraph) as well as for regular updates of exchange rates and tour data (using Quartz.NET or Hangfire)
- implement a multicultural interface
