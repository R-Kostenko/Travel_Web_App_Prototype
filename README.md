This is a logical continuation of the previous pet-project in which I get trained in new technologies and gain experience in developing ASP.NET projects.

At this point, the system is capable of performing all the functions assigned to it. Of course it needs in-depth testing with debugging, but this may take too much time, which I can't afford at the moment. The development is planned to continue for mastering new technologies (in the form of a pet-project).

DISCLAIMER: I do not have time to support the system at this time. The full functionality of the system is not available. For quick review of the development I recommend to look through the list of implemented functionality as well as screenshots of the system.

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

## Implemented functionality
- Registration, authorization and authentication
- Full-featured Tour Builder with the ability to select hotels, transportation, points of interest and other local activities (using Amadeus API, Google Maps API and currency conversion service).
- Storing tours in the database and manipulating them (e.g. automatic deletion of tours after their completion)
- Possibility to search for tours by visited countries or by using the search bar
- Possibility to view the selected tour together with further bookings (including hotel room reservations)
- Service for sending notifications to users' emails (e.g. notifying the user about the upcoming start of a tour or notifying the tour manager with links to booking activities and transportation within the system) (using FluentEmail)
- Automatic scheduling of tasks such as sending notifications, exchange rate updates and manipulation of stored tours (using HangFire)
- User support in the form of a chat with the administrator (using SignalR)

## It is planned to implement
- Multicultural interface

## System screenshots (Ukrainian interface language)
Right now, I can't provide screenshots of components such as tour ordering (along with ordering hotel rooms) and ordering transports for the tour (done by the agency manager after the tour orders are closed).

### Authorisation and registration:
![image](https://github.com/user-attachments/assets/8a7fb421-d0aa-49c8-bacd-33739ba716b0) ![image](https://github.com/user-attachments/assets/5750d83c-8028-4d98-9273-6dc3b6e27ffc)

![image](https://github.com/user-attachments/assets/4789c739-2006-4dec-8c40-e2e99b53aec9)

![image](https://github.com/user-attachments/assets/901643ff-1832-4e4e-98c1-f017df5106f2)

### Tour Builder:
![image](https://github.com/user-attachments/assets/bea49d62-cc2f-4998-b4b8-6fabce1b5d36)

![image](https://github.com/user-attachments/assets/e0776578-a05b-4de2-ab73-7a47ac373b11)

![image](https://github.com/user-attachments/assets/9b9f9369-5c1c-49ba-a079-f70e5dc6b0f8)

#### Agency registration:
![image](https://github.com/user-attachments/assets/c2026bc0-4538-49e7-a0e1-952f6e0d8a9e)

#### Search and select hotels:
![image](https://github.com/user-attachments/assets/df5165d4-e4b4-49f9-8217-baa44cf08437) ![image](https://github.com/user-attachments/assets/000b2776-3cba-499b-9903-d7d1ce8ab217)

![image](https://github.com/user-attachments/assets/465b4daa-8258-4769-b719-e9226e73c473) ![image](https://github.com/user-attachments/assets/04b1bf4f-4585-48b9-aa72-7b95f5598c94)

#### Search and selection of transport:
![image](https://github.com/user-attachments/assets/77b52eae-bc27-48bc-a218-ea5bc03372d1)

![image](https://github.com/user-attachments/assets/7cb741ca-d0c4-44f5-aa7c-1b4c5db79156)

![image](https://github.com/user-attachments/assets/39d345e7-3da6-4242-90f1-173800c04f6e)

#### Search for points of interest and local activities (demonstrated second):
![image](https://github.com/user-attachments/assets/5cc36bc2-f9db-43d9-8dc8-344a3980af5b)

![image](https://github.com/user-attachments/assets/3ed4c110-3884-448b-a4b3-df563bc3e3f4)

![image](https://github.com/user-attachments/assets/32dd67a8-d627-437b-8b98-c98515aa4a96)

### Home page:
![image](https://github.com/user-attachments/assets/3a8762ed-fc22-43a6-8043-8bceebf67ae6)

### Search by visited country:
![image](https://github.com/user-attachments/assets/180fe6d8-9ecb-4ba3-9a27-4d0034d23b5e)

### Tour overview:
![image](https://github.com/user-attachments/assets/d4e9ea39-d44f-4f60-bb8c-ddd9c8af00e1)

![image](https://github.com/user-attachments/assets/c8c3c047-4f82-4a07-9ebc-12ac8321338d)

![image](https://github.com/user-attachments/assets/cf9d5ea8-76b3-4855-b4c5-a7c22409df74)

### Chat support:
![image](https://github.com/user-attachments/assets/e31854cf-73ed-47e4-80d4-2e02decf84b0) ![image](https://github.com/user-attachments/assets/dc406b1c-bdf9-4d2e-bf30-6d414c74f112)

### Example of a successful registration email:
![image](https://github.com/user-attachments/assets/95b7201d-b12c-4acd-85fe-04246ac192b5)

## Thank you for your attention!
